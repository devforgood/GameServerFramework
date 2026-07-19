#include "Map.h"
#include "syncnet_generated.h"
#include <iostream>
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
#include "ComponentRegistry.h"
#include "NavMesh.h"
#include "INavMovement.h"
#include "NavMovementFactory.h"
#include "BTDebugSync.h"


//const float g_fDistance = std::powf(10.0f, 2);
const float g_fDistance = 10.0f;

Map::Map(World* world)
{
	world_ = world;
	navMesh_ = nullptr;
	movement_ = nullptr;
	gridManager_ = nullptr;

	systemManager_ = nullptr;
}

Map::~Map()
{
	// 살아있는 액터를 먼저 정리한다. Actor 소멸자(Actor::Clear)가 systemManager_ 를
	// 역참조하므로, systemManager_ 보다 먼저 파괴되어야 댕글링 포인터 접근을 막는다.
	actorMap_.clear();
	actorList_.clear();

	if (movement_)
	{
		delete movement_;
		movement_ = nullptr;
	}
	if (navMesh_)
	{
		delete navMesh_;
		navMesh_ = nullptr;
	}
	if (gridManager_)
	{
		delete gridManager_;
		gridManager_ = nullptr;
	}

	if (systemManager_)
	{
		delete systemManager_;
		systemManager_ = nullptr;
	}
}

bool Map::Init(const std::string& movementType, const gamedata::Map* mapData)
{
	mapData_ = mapData;

	if (!InitNavigation(movementType))
		return false;

	gridManager_ = new GridManager(100, 100, 2);
	sendMessageBuilder_ = std::make_shared<send_message>();

	InitEcs();
	return true;
}

bool Map::InitNavigation(const std::string& movementType)
{
	// 맵별 네비메시를 반드시 로드한다. 경로가 지정되지 않았거나 로드에 실패하면
	// 과거처럼 solo_navmesh.bin 으로 폴백하면 씬 지오메트리와 형상이 어긋나므로,
	// 폴백하지 않고 에러로 처리한다(호출자가 해당 맵 로드를 중단하도록 false 반환).
	navMesh_ = new NavMesh();
	if (mapData_ == nullptr || mapData_->navmesh_path.empty())
	{
		LOG.error("Map {} navmesh 경로가 지정되지 않았습니다. 맵 로드를 중단합니다.",
			mapData_ != nullptr ? mapData_->id : 0);
		return false;
	}
	if (!navMesh_->Load((GameDataPath::Resolve() + mapData_->navmesh_path).c_str()))
	{
		LOG.error("Map {} navmesh '{}' 로드에 실패했습니다. 맵 로드를 중단합니다.",
			mapData_->id, mapData_->navmesh_path);
		return false;
	}

	// 씬(Map.json) 좌표와 navmesh 정합 검증. 어긋나면 경고만 남긴다(로드는 계속).
	ValidateMapDataOnNavMesh();

	movement_ = NavMovementFactory::Create(movementType, navMesh_);
	movement_->Init();
	return true;
}

void Map::InitEcs()
{
	systemManager_ = new engine::SystemManager();

	// 컴포넌트 타입 목록은 ECS 모듈이 소유한다(새 컴포넌트 추가 시 Map 은 수정 불필요).
	engine::RegisterGameComponents(systemManager_->GetEntityManager());

	systemManager_->RegisterSystem<engine::TimerComponent>(
		[](float deltaTime, engine::TimerComponent& timer) {
			engine::TimerSystem::Update(deltaTime, timer);
		});

	systemManager_->RegisterSystem<engine::StateComponent, engine::PositionComponent>(
		[this](float, engine::StateComponent& state, engine::PositionComponent& position) {
			SyncActorState(state, position);
		});
}

void Map::SyncActorState(engine::StateComponent& state, engine::PositionComponent& position)
{
	if (state.stateID == syncnet::AIState::AIState_Destroyed) {
		removedAgents_.push_back(state.ActorID);
	}

	if (movement_->IsActive(state.ActorID) == false)
		return;

	const float* npos = movement_->GetPos(state.ActorID);
	bool changed_position = !Vector3::equal(position.x, position.y, position.z, npos[0], npos[1], npos[2]);
	if (state.changeFlag == 0 && !changed_position)
		return;

	auto itr = actorMap_.find(state.ActorID);
	if (itr == actorMap_.end())
	{
		LOG.error("SendWorldState error agent not found in actorMap_");
		return;
	}
	auto actor = (Actor*)itr->second->get();

	if (changed_position)
	{
		actor->SetPosition(npos[0], npos[1], npos[2]);
		gridManager_->move(actor, npos[0], npos[2]);
	}

	actorPendingUpdates_.push_back(actor->GetActorInfo(*sendMessageBuilder_, actor->GetChangedFlag()));

	actor->ResetChangedFlag();
}

int Map::GetMapId() const
{
	return mapData_ != nullptr ? mapData_->id : 0;
}

syncnet::Vec3 Map::GetPlayerSpawnPos() const
{
	if (mapData_ != nullptr && !mapData_->spawn_points.player_spawn.empty())
	{
		const auto& p = mapData_->spawn_points.player_spawn[0].position;
		return syncnet::Vec3(static_cast<float>(p.x), static_cast<float>(p.y), static_cast<float>(p.z));
	}
	return syncnet::Vec3(0, 0, 0);
}

int Map::SpawnMonstersFromData()
{
	if (mapData_ == nullptr)
		return 0;

	int spawned = 0;
	for (const auto& s : mapData_->spawn_points.monster_spawn)
	{
		// 스폰 위치는 Map.json(클라 좌표계) 기준. OnAddAgent 내부(Vector3 변환)에서
		// 클라 AddAgent 와 동일하게 서버 좌표계로 변환된다.
		syncnet::Vec3 pos(
			static_cast<float>(s.position.x),
			static_cast<float>(s.position.y),
			static_cast<float>(s.position.z));

		if (OnAddAgent(nullptr, syncnet::GameObjectType::GameObjectType_Monster, &pos) == nullptr)
		{
			LOG.error("Map {} monster_spawn {} 위치({}, {}, {}) 몬스터 스폰 실패",
				GetMapId(), s.id, s.position.x, s.position.y, s.position.z);
			continue;
		}
		++spawned;
	}

	if (spawned > 0)
		LOG.info("Map {} 몬스터 {}마리 스폰(monster_spawn 마커 기준)", GetMapId(), spawned);
	return spawned;
}

int Map::ValidateMapDataOnNavMesh(const gamedata::Map* mapData, const NavMesh* navMesh)
{
	if (mapData == nullptr || navMesh == nullptr || !navMesh->IsLoaded())
		return 0;

	// 수평 허용 오차(m). 마커가 메시 가장자리에서 이 이상 벗어나 있으면 정합 이상으로 본다.
	// 수직은 지형 높이 차이를 감안해 넉넉히 잡는다.
	constexpr float kHorizontalTolerance = 2.0f;
	const float halfExtents[3] = { kHorizontalTolerance, 10.0f, kHorizontalTolerance };

	int mismatches = 0;
	auto check = [&](double cx, double cy, double cz, const char* kind, int id)
	{
		// Map.json은 클라 좌표계 — navmesh(서버 좌표계)로 x 반전 변환 후 질의한다.
		const float pos[3] = {
			Vector3::convert_x(static_cast<float>(cx)),
			static_cast<float>(cy),
			static_cast<float>(cz) };

		dtPolyRef ref = 0;
		float nearest[3] = { 0, 0, 0 };
		dtStatus status = navMesh->query()->findNearestPoly(pos, halfExtents, navMesh->filter(), &ref, nearest);
		if (dtStatusFailed(status) || ref == 0)
		{
			LOG.warn("Map {} navmesh 정합 이상: {} {} 위치 클라({}, {}, {}) 주변 {}m 내에 navmesh 폴리곤이 없습니다. "
				"씬과 navmesh('{}')가 어긋났는지 확인하세요.",
				mapData->id, kind, id, cx, cy, cz, kHorizontalTolerance, mapData->navmesh_path);
			++mismatches;
		}
	};

	for (const auto& gate : mapData->gates)
		check(gate.position.x, gate.position.y, gate.position.z, "gate", gate.id);

	for (const auto& s : mapData->spawn_points.player_spawn)
		check(s.position.x, s.position.y, s.position.z, "player_spawn", s.id);
	for (const auto& s : mapData->spawn_points.monster_spawn)
		check(s.position.x, s.position.y, s.position.z, "monster_spawn", s.id);
	for (const auto& s : mapData->spawn_points.boss_spawn)
		check(s.position.x, s.position.y, s.position.z, "boss_spawn", s.id);

	if (mismatches == 0)
		LOG.info("Map {} navmesh 정합 확인: 게이트/스폰 마커 모두 navmesh 위에 있습니다.", mapData->id);
	else
		LOG.warn("Map {} navmesh 정합 이상 {}건 — 클라 씬과 서버 navmesh가 어긋났을 수 있습니다.", mapData->id, mismatches);

	return mismatches;
}

void Map::update(float deltaTime)
{
	//LOG.info("World update begin");
	// 4단계로 분리해 호출한다. 단계별 메서드는 프로파일링/벤치마크에서 개별 측정할 수 있도록
	// public 으로 노출돼 있다. 호출 순서/동작은 기존과 동일하다.
	UpdateActors(deltaTime);
	UpdateMovement(deltaTime);
	UpdateSystems(deltaTime);
	SendWorldState();
	//LOG.info("World update end");
}

// 단계 1: 살아있는 액터의 BehaviorTree tick(몬스터 AI: 적 탐지/추격/배회 등).
void Map::UpdateActors(float deltaTime)
{
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
		(*itr)->Update(deltaTime);
}

// 단계 2: 네비게이션 이동 전략(Detour crowd / waypoint) 시뮬레이션.
void Map::UpdateMovement(float deltaTime)
{
	movement_->Update(deltaTime);
}

// 단계 3: ECS 시스템(위치 변경 감지 → ActorInfo 누적, 제거 대상 수집 등).
void Map::UpdateSystems(float deltaTime)
{
	systemManager_->Update(deltaTime);
}

// 프로파일링용: 모든 액터에 대해 DetectEnemy 만 1회씩 호출(적 탐지 비용 격리 측정).
int Map::ProfileDetectEnemyAll()
{
	int found = 0;
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		if (DetectEnemy((Actor*)itr->get()) >= 0)
			++found;
	}
	return found;
}

void Map::SendWorldState()
{
	auto agents = sendMessageBuilder_->CreateVector(actorPendingUpdates_);

	// ----------------------------
	flatbuffers::Offset<syncnet::DebugRaycast> debug_raycast;
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debug_raycast_vector;
	for (int i = 0; i < this->raycasts_.size(); ++i)
	{
		debug_raycast = syncnet::CreateDebugRaycast(*sendMessageBuilder_, 0, &this->raycasts_[i]);
		debug_raycast_vector.push_back(debug_raycast);
	}
	this->raycasts_.clear();
	auto debug_raycasts = sendMessageBuilder_->CreateVector(debug_raycast_vector);
	// ----------------------------

	auto updateActorNotify = syncnet::CreateUpdateActorNotify(*sendMessageBuilder_, agents, debug_raycasts);

	auto send_msg = syncnet::CreateGameMessage(*sendMessageBuilder_, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	sendMessageBuilder_->Finish(send_msg);

	SendBroadcast(sendMessageBuilder_);

	SendTreeDebugSync();

	for (auto& agent_id : removedAgents_)
	{
		OnRemoveAgent(agent_id);
	}

	sendMessageBuilder_ = std::make_shared<send_message>();
	actorPendingUpdates_.clear();
	removedAgents_.clear();
}

void Map::SendTreeDebugSync()
{
	// 직렬화는 BT 디버그 모듈(BTDebugSync)이 담당하고, 맵은 브로드캐스트만 한다.
	// 보낼 스냅샷이 없거나 BT 디버그가 꺼진 빌드면 nullptr 라 아무것도 하지 않는다.
	auto msg = BTDebugSync::BuildMessage();
	if (msg != nullptr)
		SendBroadcast(msg);
}

void Map::SendBroadcast(std::shared_ptr<send_message> msg)
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		itr->second->Send(msg);
	}
}

void Map::SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except)
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		if (itr->second.get() == except.get())
			continue;

		itr->second->Send(msg);
	}
}

void Map::OnRemoveAgent(int agent_id)
{
	auto itr = actorMap_.find(agent_id);
	if (itr == actorMap_.end())
	{
		LOG.error("OnRemoveAgent error not exist in monstersMap_");
		return;
	}

	if (itr->second->get()->GetType() == syncnet::GameObjectType_Character)
	{
		auto character = std::dynamic_pointer_cast<Character>(*itr->second);

		auto itr_player = players_.find(character->GetPlayerId());
		if (itr_player != players_.end())
		{
			players_.erase(itr_player);
		}
	}

	gridManager_->remove((Actor*)itr->second->get());
	actorList_.erase(itr->second);
	actorMap_.erase(itr);

	movement_->RemoveAgent(agent_id);

}

std::shared_ptr<Actor> Map::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	auto actor = ActorFactory::CreateActor(this, player, type, pos);
	if (actor == nullptr)
	{
		LOG.error("OnAddAgent error in ActorFactory::CreateActor()");
		return nullptr;
	}

	auto itr = actorList_.insert(actorList_.end(), actor);
	actorMap_.insert(std::make_pair(actor->GetActorId(), itr));
	actor->SetChangedFlag(static_cast<long>(GameObjectChangeType::All));

	return actor;
}


void Map::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->GetNavMap()->SetMoveTarget(agent_id, Vector3(pos).pos(), false);

}

void Map::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->GetNavMap()->Raycast(0, Vector3(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

int Map::DetectEnemy(Actor* actor)
{
	float hitPoint[3];

	// 캐릭터만, 실제 거리로 컬링해서 가져온다. detectScratch_ 버퍼를 재사용해 호출당 힙 할당이 없다.
	gridManager_->getCharactersInViewRange(actor, g_fDistance, detectScratch_);

	for (auto* target : detectScratch_)
	{
		const float* targetPos = this->GetNavMap()->GetPos(target->GetActorId());

		if (this->GetNavMap()->Raycast(actor->GetActorId(), targetPos, hitPoint) == false)
		{
			return target->GetActorId();
		}
	}
	return -1;
}

std::vector<IGridActor*> Map::get_actors_in_range(Actor* actor, float range, float dirDeg, float angle)
{
	return gridManager_->getEntitiesInAoEMask(actor->GetVecter2X(), actor->GetVecter2Y(), range, dirDeg, angle);
}

void Map::Enter(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::Enter error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr != players_.end())
	{
		LOG.error("Map::Enter error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->GetPlayerId(), player));
}

void Map::SendStateTo(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::SendStateTo error player is nullptr");
		return;
	}

	// 현재 맵의 모든 액터 상태를 해당 플레이어에게 전송한다.
	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agents;
	GetAgentsInfo(builder_ptr, agents);
	auto updateActorNotify = syncnet::CreateUpdateActorNotifyDirect(*builder_ptr, &agents, nullptr);
	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);
	player->Send(builder_ptr);
}

void Map::join(std::shared_ptr<Player> player)
{
	Enter(player);
	SendStateTo(player);
}

void Map::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr == players_.end())
	{
		LOG.error("Map::leave error player not found");
		return;
	}
	players_.erase(itr);
}

std::shared_ptr<Player> Map::FindPlayer(long player_id)
{
	auto itr = players_.find(player_id);
	if (itr == players_.end())
		return nullptr;
	return itr->second;
}

std::shared_ptr<Actor> Map::FindActor(int actor_id)
{
	auto itr = actorMap_.find(actor_id);
	if (itr == actorMap_.end())
		return nullptr;
	return *itr->second;
}

void Map::GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector)
{
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		auto actor = itr->get();
		agent_info_vector.push_back(actor->GetActorInfo(*msg, static_cast<long>(GameObjectChangeType::All)));
	}
}
