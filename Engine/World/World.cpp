#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <algorithm>
#include <vector>
#include <iostream>
#include <stdexcept>
#include "Server.h"
#include "Monster.h"
#include "Character.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "Player.h"
#include "ActorFactory.h"
#include "Common.h"
#include "Map.h"
#include "GameMode.h"
#include "GameModeFactory.h"

namespace
{
	// 세션 끊김 후 캐릭터를 월드에 유지하는 유예 시간(초). 이 시간 내 같은 userId 로
	// 재접속하면 기존 캐릭터를 그대로 넘겨받는다(핸드오버).
	constexpr float kReconnectGraceSec = 60.0f;
}

World::World()
{
	randomUtil_ = nullptr;
	timeStamp_ = nullptr;
}

World::~World()
{
	if (randomUtil_)
	{
		delete randomUtil_;
		randomUtil_ = nullptr;
	}
	if (timeStamp_)
	{
		delete timeStamp_;
		timeStamp_ = nullptr;
	}
}

void World::Init(const std::string& movementOverride)
{
	Monster::Initialize(GameDataPath::Resolve() + "mob.lua");
	Monster::registerLuaFunctionAll();

	// 게임 모드용 공유 lua 상태 생성 + 호스트 함수(GM_*) 등록.
	GameMode::InitializeLua();

	randomUtil_ = new RandomUtil();
	timeStamp_ = new TimeStamp();

	// 기본 게임 모드 부트스트랩(현재는 Field, id=1).
	// 추후 매치메이킹/세션이 모드 id 를 결정하도록 확장한다.
	gameMode_.reset(GameModeFactory::Create(1));

	// 게임 모드 데이터가 지정한 이동 전략으로 맵의 네비게이션을 초기화한다.
	// 미지정 시 NavMovementFactory 가 crowd 로 폴백한다.
	std::string movementType;
	if (gameMode_ && gameMode_->gamedata)
		movementType = gameMode_->gamedata->movement;

	// 명시적 오버라이드가 있으면 게임 모드 설정보다 우선한다(벤치마크/테스트).
	if (!movementOverride.empty())
		movementType = movementOverride;

	// 로드된 맵 데이터 중 field 타입(소속 게임 모드 type == "field") 맵을 모두 로드한다.
	auto& resource = ResourceLoader::Instance();
	std::vector<const gamedata::Map*> fieldMaps;
	for (const auto& pair : resource.GetMaps())
	{
		const gamedata::Map* mapData = pair.second;
		if (mapData == nullptr)
			continue;

		const gamedata::GameMode* gm = resource.GetGameMode(mapData->game_mode_id);
		if (gm == nullptr || gm->type != "field")
			continue;

		fieldMaps.push_back(mapData);
	}

	// GetMaps() 는 unordered_map 이라 순회 순서가 비결정적이므로, primary 맵(front)이
	// 항상 동일하도록 맵 id 오름차순으로 정렬한다.
	std::sort(fieldMaps.begin(), fieldMaps.end(),
		[](const gamedata::Map* a, const gamedata::Map* b) { return a->id < b->id; });

	for (const gamedata::Map* mapData : fieldMaps)
	{
		std::shared_ptr<Map> map = std::make_shared<Map>(this);
		if (!map->Init(movementType, mapData))
		{
			// navmesh 로드 실패 등 맵 초기화 실패는 치명적 설정 오류로 보고 서버 기동을 중단한다.
			// (Map::Init 이 원인을 로그로 남긴다.)
			LOG.error("World::Init map {} 초기화 실패로 서버 기동을 중단합니다.", mapData->id);
			throw std::runtime_error("맵 초기화 실패(navmesh 없음): map id " + std::to_string(mapData->id));
		}
		mapList_.push_back(map);
		mapById_[mapData->id] = map;
	}

	if (gameMode_ && !mapList_.empty())
	{
		gameMode_->SetMap(mapList_.front().get());
		gameMode_->LoadScript();
		gameMode_->Start();
	}
}

void World::SpawnMapMonsters()
{
	for (auto& map : mapList_)
		map->SpawnMonstersFromData();
}

void World::update(float deltaTime)
{
	timeStamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Map>>::iterator itr = mapList_.begin();itr!= mapList_.end();++itr)
		(*itr)->update(deltaTime);

	if (gameMode_)
		gameMode_->Update(deltaTime);

	TickReconnectGrace(deltaTime);
}

void World::join(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::join error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr != players_.end())
	{
		LOG.error("World::join error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->GetPlayerId(), player));

	// todo : map 선택 로직 추가
	mapList_.begin()->get()->join(player);
}

void World::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr == players_.end())
	{
		LOG.error("World::leave error player not found");
		return;
	}
	players_.erase(itr);

	// 플레이어가 실제로 속한 맵에서 정리한다. 게이트 이동 후에는 기본 맵이 아닌
	// 다른 맵에 있을 수 있으므로 캐릭터의 현재 맵을 사용해야 한다. 과거엔 항상 첫
	// 맵만 정리해서, 이동한 플레이어가 목적지 맵의 players_/actorList_ 에 남아 세션이
	// 사라진 뒤에도 매 틱 브로드캐스트되며 "Session expired! in send" 가 반복됐다.
	// OnRemoveAgent 는 캐릭터 액터(grid/movement/actorList)와 해당 맵의 players_ 를
	// 함께 제거한다.
	auto character = player->GetCharacter();
	if (character != nullptr && character->GetMap() != nullptr)
	{
		character->GetMap()->OnRemoveAgent(character->GetActorId());
	}
	else if (!mapList_.empty())
	{
		// 빙의된 캐릭터/맵이 없으면(로그인 직후 등) 기본 맵의 브로드캐스트 목록에서만 제거.
		mapList_.begin()->get()->leave(player);
	}
}

void World::BeginDisconnect(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::BeginDisconnect error player is nullptr");
		return;
	}

	// 이미 처리된(월드 목록에 없는) 플레이어면 재진입하지 않는다. 세션 종료는 read/write
	// 에러 등 여러 경로에서 중복 호출될 수 있으므로 멱등하게 만든다.
	auto itr = players_.find(player->GetPlayerId());
	if (itr == players_.end())
		return;
	players_.erase(itr);

	auto character = player->GetCharacter();

	// 캐릭터가 맵에 있으면(=로그인+스폰 완료), 캐릭터 액터는 맵에 유지한 채 브로드캐스트
	// 목록에서만 빼고 유예 대기열에 올린다. 유예 시간 내 같은 uuid(재접속 토큰)로 재접속하면
	// 그대로 넘겨받는다. 대기열 키는 플레이어 고유 uuid 라, 다른 클라가 같은 userId 로
	// 접속해도 남의 캐릭터를 가로챌 수 없다.
	if (character != nullptr && character->GetMap() != nullptr)
	{
		const std::string uuid = player->GetUuid();
		character->GetMap()->leave(player); // 맵 players_(브로드캐스트)에서만 제거, 액터는 유지
		pendingReconnects_[uuid] = PendingReconnect{ player, kReconnectGraceSec };
		LOG.info("player {}(uuid '{}') disconnected. holding character {}s for reconnect.",
			player->GetPlayerId(), uuid, kReconnectGraceSec);
		return;
	}

	// 로그인 전/캐릭터 없음 → 즉시 정리.
	if (character != nullptr && character->GetMap() != nullptr)
		character->GetMap()->OnRemoveAgent(character->GetActorId());
	else if (!mapList_.empty())
		mapList_.begin()->get()->leave(player);
}

void World::TickReconnectGrace(float deltaTime)
{
	for (auto it = pendingReconnects_.begin(); it != pendingReconnects_.end(); )
	{
		it->second.remainingSec -= deltaTime;
		if (it->second.remainingSec > 0.0f)
		{
			++it;
			continue;
		}

		// 유예 종료 → 캐릭터를 월드에서 제거하고 데이터를 저장한 뒤 대기열에서 뺀다.
		auto player = it->second.player;
		LOG.info("reconnect grace expired for uuid '{}'. removing character.", it->first);

		// 제거 전에 마지막 위치를 기록한다. 같은 userId 로 다시 로그인하면 이 위치로 스폰된다.
		RememberLastLocation(player);

		auto character = player->GetCharacter();
		if (character != nullptr && character->GetMap() != nullptr)
			character->GetMap()->OnRemoveAgent(character->GetActorId());

		player->SavePlayerData(); // 드롭 전에 영속화(비동기 저장이 shared_ptr 로 수명 유지)

		it = pendingReconnects_.erase(it);
	}
}

std::shared_ptr<Player> World::TryReconnect(const std::string& uuid)
{
	if (uuid.empty())
		return nullptr;

	auto it = pendingReconnects_.find(uuid);
	if (it == pendingReconnects_.end())
		return nullptr;

	auto player = it->second.player;
	pendingReconnects_.erase(it);

	// 월드/맵 브로드캐스트 목록에 다시 등록한다(캐릭터 액터는 유예 동안 유지돼 있었다).
	players_.insert(std::make_pair(player->GetPlayerId(), player));
	auto character = player->GetCharacter();
	if (character != nullptr && character->GetMap() != nullptr)
		character->GetMap()->Enter(player);

	LOG.info("player {}(uuid '{}') reconnected. handing over existing character.",
		player->GetPlayerId(), uuid);
	return player;
}



void World::RememberLastLocation(const std::shared_ptr<Player>& player)
{
	if (player == nullptr || player->GetUserId().empty())
		return;

	auto character = player->GetCharacter();
	if (character == nullptr || character->GetMap() == nullptr)
		return;

	// 캐릭터 위치는 서버 좌표계 → Login 응답/AddAgent 에 그대로 쓸 수 있게 클라 좌표계로 변환해 보관.
	const Vector3& p = character->GetPosition();
	lastLocations_[player->GetUserId()] = LastLocation{
		character->GetMap()->GetMapId(),
		syncnet::Vec3(p.convert_x(), p.convert_y(), p.convert_z()) };
}

bool World::GetLastLocation(const std::string& userId, int& outMapId, syncnet::Vec3& outPos) const
{
	if (userId.empty())
		return false;

	auto it = lastLocations_.find(userId);
	if (it == lastLocations_.end())
		return false;

	outMapId = it->second.mapId;
	outPos = it->second.pos;
	return true;
}

Map* World::FindMap(int mapId)
{
	auto itr = mapById_.find(mapId);
	return itr != mapById_.end() ? itr->second.get() : nullptr;
}

bool World::ChangeMap(std::shared_ptr<Player> player, int mapId, int gateId, syncnet::Vec3& outPos, int& outAgentId)
{
	if (player == nullptr)
	{
		LOG.error("World::ChangeMap error player is nullptr");
		return false;
	}

	auto character = player->GetCharacter();
	if (character == nullptr)
	{
		LOG.error("World::ChangeMap error character is nullptr");
		return false;
	}

	Map* destMap = FindMap(mapId);
	if (destMap == nullptr)
	{
		LOG.error("World::ChangeMap error map {} not found", mapId);
		return false;
	}

	const gamedata::Map* destData = destMap->GetMapData();
	if (destData == nullptr)
	{
		LOG.error("World::ChangeMap error map {} has no data", mapId);
		return false;
	}

	// 목적지 맵에서 gateId 게이트를 찾아 도착 위치를 얻는다.
	const gamedata::MapGate* gate = nullptr;
	for (const auto& g : destData->gates)
	{
		if (g.id == gateId)
		{
			gate = &g;
			break;
		}
	}
	if (gate == nullptr)
	{
		LOG.error("World::ChangeMap error gate {} not found in map {}", gateId, mapId);
		return false;
	}

	Map* oldMap = character->GetMap();
	int oldHealth = character->GetHealth();

	// 이전 맵에서 기존 캐릭터를 제거한다(이전 맵의 players_/actorList_/grid/movement 정리).
	if (oldMap != nullptr)
		oldMap->OnRemoveAgent(character->GetActorId());

	// 새 맵에서 캐릭터를 재생성하려면 기존 빙의를 해제해야 한다(Character::PreCreate 검사 통과).
	player->UnPossess();

	// 게이트 위치(Map.json = 클라 좌표계)에 캐릭터를 새로 배치한다.
	// OnAddAgent 내부에서 클라 AddAgent 와 동일하게 서버 좌표계로 변환된다.
	syncnet::Vec3 gatePos(
		static_cast<float>(gate->position.x),
		static_cast<float>(gate->position.y),
		static_cast<float>(gate->position.z));

	auto newActor = destMap->OnAddAgent(player, syncnet::GameObjectType::GameObjectType_Character, &gatePos);
	if (newActor == nullptr)
	{
		LOG.error("World::ChangeMap error failed to add character to map {}", mapId);
		return false;
	}

	// 이전 캐릭터의 체력을 이어받는다.
	newActor->SetHealth(oldHealth);

	// 새 맵의 players_ 에 플레이어를 등록한다. 상태 동기화(SendStateTo)는 클라가
	// EnterGate 응답을 먼저 받아 맵 프리팹을 교체한 뒤 처리하도록, 호출 측(핸들러)에서
	// 응답 전송 이후에 별도로 수행한다.
	destMap->Enter(player);

	outPos = gatePos;
	outAgentId = newActor->GetActorId();
	LOG.info("World::ChangeMap success: player {} -> map {} gate {}, newAgentId {}, pos({},{},{})",
		player->GetPlayerId(), mapId, gateId, outAgentId, outPos.x(), outPos.y(), outPos.z());
	return true;
}

std::shared_ptr<Actor> World::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	if (mapList_.empty())
	{
		LOG.error("World::OnAddAgent error: no map loaded (ResourceLoader 미로드?)");
		return nullptr;
	}

	Map* map = GetPrimaryMap();

	// 캐릭터 스폰은 로그인 시 결정된 스폰 맵(기본 맵 또는 이전 위치의 맵)으로 라우팅한다.
	// 몬스터(디버그 스폰 등)는 기존대로 기본 맵에 만든다.
	if (type == syncnet::GameObjectType::GameObjectType_Character && player != nullptr)
	{
		Map* spawnMap = FindMap(player->GetSpawnMapId());
		if (spawnMap != nullptr)
			map = spawnMap;
	}

	auto actor = map->OnAddAgent(player, type, pos);

	// 스폰 맵이 기본 맵과 다르면 브로드캐스트 등록을 스폰 맵으로 옮긴다.
	// (접속 시 World::join 이 기본 맵의 players_ 에 등록해 두었기 때문.)
	if (actor != nullptr
		&& type == syncnet::GameObjectType::GameObjectType_Character
		&& player != nullptr
		&& map != GetPrimaryMap())
	{
		GetPrimaryMap()->leave(player);
		map->Enter(player);
		map->SendStateTo(player);
	}

	return actor;
}

void World::OnRemoveAgent(int agent_id)
{
	// todo : map 선택 로직 추가
	if (mapList_.empty())
		return;
	mapList_.begin()->get()->OnRemoveAgent(agent_id);
}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetMoveTarget(agent_id, pos);
}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetRaycast(pos);
}