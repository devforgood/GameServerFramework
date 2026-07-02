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
	Monster::Initialize("mob.lua");
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

void World::update(float deltaTime)
{
	timeStamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Map>>::iterator itr = mapList_.begin();itr!= mapList_.end();++itr)
		(*itr)->update(deltaTime);

	if (gameMode_)
		gameMode_->Update(deltaTime);
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
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->leave(player);
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
	// todo : map 선택 로직 추가
	return mapList_.begin()->get()->OnAddAgent(player, type, pos);
}

void World::OnRemoveAgent(int agent_id)
{
	// todo : map 선택 로직 추가
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