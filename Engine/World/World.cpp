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
#include "NavMesh.h"
#include "GameMode.h"
#include "GameModeFactory.h"
#include "PartyManager.h"

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

int World::ValidateGameData(int* outWarnings)
{
	auto& resource = ResourceLoader::Instance();
	const auto& maps = resource.GetMaps();

	int errors = 0;
	int warnings = 0;

	// 맵 id -> 그 맵으로 들어오는 게이트 수. 0 이면 게이트로는 도달할 수 없는 맵이다.
	std::unordered_map<int, int> incomingGates;

	// 검사 순서를 고정한다 — GetMaps() 는 unordered_map 이라 순회 순서가 비결정적이고,
	// 그대로 두면 로그 순서가 실행마다 달라져 비교하기 어렵다.
	std::vector<const gamedata::Map*> sortedMaps;
	sortedMaps.reserve(maps.size());
	for (const auto& pair : maps)
	{
		if (pair.second != nullptr)
			sortedMaps.push_back(pair.second);
	}
	std::sort(sortedMaps.begin(), sortedMaps.end(),
		[](const gamedata::Map* a, const gamedata::Map* b) { return a->id < b->id; });

	for (const gamedata::Map* map : sortedMaps)
	{
		const gamedata::GameMode* mode = resource.GetGameMode(map->game_mode_id);
		if (mode == nullptr)
		{
			LOG.error("데이터 정합성: map {}('{}') 의 game_mode_id {} 가 GameMode.json 에 없습니다. "
				"이 맵은 로드되지 않습니다.", map->id, map->name, map->game_mode_id);
			++errors;
		}
		else if (mode->type == "field" && map->navmesh_path.empty())
		{
			LOG.error("데이터 정합성: field 맵 {}('{}') 에 navmesh_path 가 없습니다. "
				"Map::Init 이 실패해 서버 기동이 중단됩니다.", map->id, map->name);
			++errors;
		}

		for (const auto& gate : map->gates)
		{
			// target_id 는 전역 유일 마커 id 다(게이트 또는 player_spawn). 목적지 맵은
			// 그 마커의 parent 로 정해진다.
			const gamedata::Map* dest = nullptr;
			syncnet::Vec3 unusedPos(0, 0, 0);
			if (!Map::ResolveGateTarget(gate.target_id, dest, unusedPos))
			{
				LOG.error("데이터 정합성: map {} 의 게이트 {}('{}') 의 target_id {} 에 해당하는 "
					"게이트/player_spawn 이 없습니다.",
					map->id, gate.id, gate.name, gate.target_id);
				++errors;
				continue;
			}

			++incomingGates[dest->id];
		}
	}

	// GameMode.maps 는 Map.game_mode_id 의 역방향 사본이다(코드는 game_mode_id 만 읽는다).
	// 사본이라 어긋나기 쉬우므로 양방향으로 맞춰 본다.
	for (const gamedata::Map* map : sortedMaps)
	{
		const gamedata::GameMode* mode = resource.GetGameMode(map->game_mode_id);
		if (mode == nullptr)
			continue; // 위에서 이미 보고했다.

		if (std::find(mode->maps.begin(), mode->maps.end(), map->id) == mode->maps.end())
		{
			LOG.warn("데이터 정합성: map {} 의 game_mode_id 는 {} 인데 그 모드의 maps 목록에 빠져 있습니다.",
				map->id, mode->id);
			++warnings;
		}
	}

	std::vector<const gamedata::GameMode*> sortedModes;
	sortedModes.reserve(resource.GetGameModes().size());
	for (const auto& pair : resource.GetGameModes())
	{
		if (pair.second != nullptr)
			sortedModes.push_back(pair.second);
	}
	std::sort(sortedModes.begin(), sortedModes.end(),
		[](const gamedata::GameMode* a, const gamedata::GameMode* b) { return a->id < b->id; });

	for (const gamedata::GameMode* mode : sortedModes)
	{
		for (int mapId : mode->maps)
		{
			const gamedata::Map* map = resource.GetMap(mapId);
			if (map == nullptr)
			{
				LOG.warn("데이터 정합성: game mode {}('{}') 의 maps 에 존재하지 않는 맵 {} 이 있습니다.",
					mode->id, mode->name, mapId);
				++warnings;
			}
			else if (map->game_mode_id != mode->id)
			{
				LOG.warn("데이터 정합성: game mode {} 의 maps 에 map {} 이 있지만 그 맵의 game_mode_id 는 {} 입니다.",
					mode->id, mapId, map->game_mode_id);
				++warnings;
			}
		}
	}

	// 들어오는 게이트가 없는 맵. field 맵 중 가장 작은 id 는 로그인 스폰 맵(GetPrimaryMap)이라
	// 게이트 없이도 도달하므로 제외한다.
	int primaryFieldMapId = 0;
	for (const gamedata::Map* map : sortedMaps)
	{
		const gamedata::GameMode* mode = resource.GetGameMode(map->game_mode_id);
		if (mode != nullptr && mode->type == "field")
		{
			primaryFieldMapId = map->id; // sortedMaps 는 id 오름차순이라 첫 field 맵이 primary
			break;
		}
	}

	for (const gamedata::Map* map : sortedMaps)
	{
		if (map->id == primaryFieldMapId || incomingGates[map->id] > 0)
			continue;

		LOG.warn("데이터 정합성: map {}('{}') 으로 들어오는 게이트가 없습니다 — 플레이어가 도달할 수 없습니다.",
			map->id, map->name);
		++warnings;
	}

	if (errors == 0 && warnings == 0)
		LOG.info("데이터 정합성 확인: Map.json / GameMode.json 참조가 모두 맞습니다.");
	else
		LOG.warn("데이터 정합성: 오류 {}건, 경고 {}건 — 위 로그를 확인하세요.", errors, warnings);

	if (outWarnings != nullptr)
		*outWarnings = warnings;
	return errors;
}

void World::Init(const std::string& movementOverride)
{
	Monster::Initialize(GameDataPath::Resolve() + "mob.lua");
	Monster::registerLuaFunctionAll();

	// 게임 모드용 공유 lua 상태 생성 + 호스트 함수(GM_*) 등록.
	GameMode::InitializeLua();

	// 맵을 로드하기 전에 데이터 참조를 검사한다(문제는 로그로만 남기고 계속 진행).
	ValidateGameData();

	randomUtil_ = new RandomUtil();
	timeStamp_ = new TimeStamp();

	// 이동 전략은 각 맵이 자기 게임 모드 데이터에서 읽는다(Map::Init). 여기서 넘기는 값은
	// 명시적 오버라이드이며, 비어 있지 않으면 맵 설정보다 우선한다(벤치마크/테스트).
	const std::string& movementType = movementOverride;

	// 로드된 맵 데이터 중 field 타입(소속 게임 모드 type == "field") 맵을 모두 로드한다.
	// field 가 아닌 맵(raid 등 인스턴스)은 여기서 만들지 않는다 — 플레이어가 입장할 때
	// CreateInstance 로 매번 새로 만든다.
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

	// 이동 전략을 오버라이드했으면 인스턴스도 같은 값으로 만들어야 한다(벤치마크/테스트).
	movementOverride_ = movementOverride;

	// 맵을 넘나드는 경로 탐색용 그래프. 도보 비용을 각 맵의 navmesh 로 실측하므로
	// 맵을 다 만든 뒤에 빌드해야 한다.
	RebuildZoneGraph();
	zoneGraph_.LogSummary();
}

void World::RebuildZoneGraph()
{
	zoneGraph_.Build(MakeWalkCostFn());
}

ZoneGraph::WalkCostFn World::MakeWalkCostFn()
{
	return [this](int mapId, const syncnet::Vec3& from, const syncnet::Vec3& to, float& outCost)
	{
		Map* map = FindMap(mapId);
		const NavMesh* nav = (map != nullptr) ? map->GetNavMesh() : nullptr;
		if (nav == nullptr || !nav->IsLoaded())
			return false;   // 인스턴스 맵 등 지금 로드돼 있지 않은 맵.

		// Map.json 은 클라 좌표계 — navmesh 질의는 서버 좌표계로 x 를 뒤집어야 한다.
		const float start[3] = { Vector3::convert_x(from.x()), from.y(), from.z() };
		const float end[3] = { Vector3::convert_x(to.x()), to.y(), to.z() };
		return nav->PathLength(start, end, outCost);
	};
}

void World::SpawnMapMonsters()
{
	for (auto& map : mapList_)
		map->SpawnMonstersFromData();
}

Map* World::CreateInstance(int mapId)
{
	const gamedata::Map* mapData = ResourceLoader::Instance().GetMap(mapId);
	if (mapData == nullptr)
	{
		LOG.error("World::CreateInstance map {} 데이터가 없습니다.", mapId);
		return nullptr;
	}

	auto map = std::make_shared<Map>(this);
	map->SetInstanceId(nextInstanceId_);

	// Init 이 게임 모드를 만들고 on_start 까지 부른다(레이드는 여기서 보스가 스폰된다).
	if (!map->Init(movementOverride_, mapData))
	{
		LOG.error("World::CreateInstance map {} 초기화 실패(navmesh 확인).", mapId);
		return nullptr;
	}

	map->SpawnMonstersFromData();

	++nextInstanceId_;
	instances_.push_back(map);
	LOG.info("인스턴스 생성: map {} instance {} (현재 {}개)", mapId, map->GetInstanceId(), instances_.size());
	return map.get();
}

bool World::FindInstanceExit(const gamedata::Map* data, int& outTargetId)
{
	// 인스턴스의 출구는 그 맵의 첫 게이트다(Dragon's Lair 의 "Lair Exit" 처럼
	// 인스턴스 맵의 게이트는 밖으로 나가는 문 하나뿐이다).
	if (data == nullptr || data->gates.empty())
		return false;

	outTargetId = data->gates.front().target_id;
	return true;
}

void World::EvictInstancePlayers(Map* instance)
{
	if (instance == nullptr)
		return;

	int exitTargetId = 0;
	if (!FindInstanceExit(instance->GetMapData(), exitTargetId))
	{
		LOG.error("인스턴스 map {} 에 나갈 게이트가 없어 플레이어를 내보내지 못했습니다.",
			instance->GetMapId());
		return;
	}

	// ChangeMap 이 players_ 를 건드리므로 목록을 먼저 복사해 두고 순회한다.
	for (auto& player : instance->GetPlayers())
	{
		syncnet::Vec3 outPos(0, 0, 0);
		int outActorId = 0;
		int outMapId = 0;
		if (!ChangeMap(player, exitTargetId, outMapId, outPos, outActorId))
		{
			LOG.error("인스턴스 퇴장 실패: player {} -> target {}",
				player->GetPlayerId(), exitTargetId);
			continue;
		}

		// 클라가 요청하지 않은 이동이므로 EnterGate 를 그대로 밀어 준다(id 0 = 서버 통보).
		// 클라는 응답 대기 콜백이 아니라 메시지 종류로 받아 처리한다(Session.OnReceive).
		player->Send(
			syncnet::CreateEnterGate
			, syncnet::GameMessages::GameMessages_EnterGate
			, 0
			, syncnet::StatusCode::StatusCode_Success
			, outMapId
			, exitTargetId
			, &outPos
			, outActorId
		);

		auto& character = player->GetCharacter();
		Map* destMap = character != nullptr ? character->GetMap() : nullptr;
		if (destMap != nullptr)
			destMap->SendStateTo(player);
	}
}

void World::CleanupInstances()
{
	for (auto itr = instances_.begin(); itr != instances_.end(); )
	{
		Map* instance = itr->get();
		GameMode* mode = instance->GetGameMode();
		const bool ended = (mode != nullptr && mode->IsEnded());
		const bool empty = (instance->GetPlayerCount() == 0);

		if (!ended && !empty)
		{
			++itr;
			continue;
		}

		if (ended && !empty)
		{
			// 진행이 끝났는데 아직 사람이 남아 있으면 밖으로 내보낸 뒤에 파괴한다.
			// 내보내기에 실패하면(나갈 게이트가 없는 등) 파괴하지 않는다 — 여기서 맵을
			// 지우면 남은 캐릭터가 갈 곳을 잃는다.
			EvictInstancePlayers(instance);
			if (instance->GetPlayerCount() > 0)
			{
				++itr;
				continue;
			}
		}

		LOG.info("인스턴스 파괴: map {} instance {} (사유: {})",
			instance->GetMapId(), instance->GetInstanceId(), ended ? "진행 종료" : "인원 없음");
		itr = instances_.erase(itr);
	}
}

void World::update(float deltaTime)
{
	timeStamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Map>>::iterator itr = mapList_.begin();itr!= mapList_.end();++itr)
		(*itr)->update(deltaTime);

	for (auto& instance : instances_)
		instance->update(deltaTime);

	CleanupInstances();

	TickReconnectGrace(deltaTime);

	// 답하지 않은 파티 초대를 만료시킨다. 파티는 맵에 속하지 않으므로 월드가 돌린다.
	PartyManager::Instance().Update(deltaTime);
}

std::shared_ptr<Player> World::FindPlayer(long player_id) const
{
	auto it = players_.find(player_id);
	return it != players_.end() ? it->second : nullptr;
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

std::vector<Map*> World::GetMaps() const
{
	std::vector<Map*> maps;
	maps.reserve(mapList_.size());
	for (const auto& map : mapList_)
		maps.push_back(map.get());
	return maps;
}

bool World::ChangeMap(std::shared_ptr<Player> player, int targetId, int& outMapId, syncnet::Vec3& outPos, int& outActorId)
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

	// 도착 지점과 목적지 맵을 target_id 하나로 푼다. 마커(게이트/스폰 지점)가 parent 로
	// 소속 맵을 들고 있어서, 어느 맵인지 따로 받을 필요가 없다.
	const gamedata::Map* destData = nullptr;
	syncnet::Vec3 arrivalPos(0, 0, 0);
	if (!Map::ResolveGateTarget(targetId, destData, arrivalPos))
	{
		LOG.error("World::ChangeMap error target {} not found (게이트/player_spawn 아님)", targetId);
		return false;
	}

	const int mapId = destData->id;

	// 상시 맵이면 그것을 쓰고, 인스턴스 모드(field 가 아닌 모드)면 새로 하나 만든다.
	// 인스턴스는 mapById_ 에 없으므로 FindMap 으로는 절대 찾히지 않는다.
	Map* destMap = FindMap(mapId);
	if (destMap == nullptr)
	{
		const gamedata::GameMode* destMode = ResourceLoader::Instance().GetGameMode(destData->game_mode_id);
		if (destMode == nullptr || destMode->type == "field")
		{
			// field 맵인데 상시 목록에 없다 = 기동 때 로드에 실패했거나 데이터가 없다.
			LOG.error("World::ChangeMap error map {} not found", mapId);
			return false;
		}

		destMap = CreateInstance(mapId);
		if (destMap == nullptr)
			return false;
	}

	Map* oldMap = character->GetMap();
	int oldHealth = character->GetHealth();

	// 이전 맵에서 기존 캐릭터를 제거한다(이전 맵의 players_/actorList_/grid/movement 정리).
	if (oldMap != nullptr)
		oldMap->OnRemoveAgent(character->GetActorId());

	// 새 맵에서 캐릭터를 재생성하려면 기존 빙의를 해제해야 한다(Character::PreCreate 검사 통과).
	player->UnPossess();

	// 도착 위치(클라 좌표계)에 캐릭터를 새로 배치한다.
	// OnAddAgent 내부에서 클라 AddAgent 와 동일하게 서버 좌표계로 변환된다.
	auto newActor = destMap->OnAddAgent(player, syncnet::GameObjectType::GameObjectType_Character, &arrivalPos);
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

	outMapId = mapId;
	outPos = arrivalPos;
	outActorId = newActor->GetActorId();
	LOG.info("World::ChangeMap success: player {} -> map {} target {}, newActorId {}, pos({},{},{})",
		player->GetPlayerId(), mapId, targetId, outActorId, outPos.x(), outPos.y(), outPos.z());
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

void World::OnRemoveAgent(int actor_id)
{
	// todo : map 선택 로직 추가
	if (mapList_.empty())
		return;
	mapList_.begin()->get()->OnRemoveAgent(actor_id);
}

void World::OnSetMoveTarget(int actor_id, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetMoveTarget(actor_id, pos);
}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetRaycast(pos);
}