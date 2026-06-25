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
#include "NavMesh.h"
#include "INavMovement.h"
#include "NavMovementFactory.h"
#include "BTDebugManager.h"


//const float g_fDistance = std::powf(10.0f, 2);
const float g_fDistance = 10.0f;

namespace
{
	syncnet::TreeNodeStatus ToTreeNodeStatus(BT::NodeStatus status)
	{
		switch (status)
		{
		case BT::NodeStatus::IDLE:
			return syncnet::TreeNodeStatus_Idle;
		case BT::NodeStatus::RUNNING:
			return syncnet::TreeNodeStatus_Running;
		case BT::NodeStatus::SUCCESS:
			return syncnet::TreeNodeStatus_Success;
		case BT::NodeStatus::FAILURE:
			return syncnet::TreeNodeStatus_Failure;
		case BT::NodeStatus::SKIPPED:
			return syncnet::TreeNodeStatus_Skipped;
		default:
			return syncnet::TreeNodeStatus_Unknown;
		}
	}

#if defined(ENABLE_BT_DEBUG)
	syncnet::TreeNodeType ToTreeNodeType(BTDebugNodeType node_type)
	{
		switch (node_type)
		{
		case BTDebugNodeType::Control:
			return syncnet::TreeNodeType_Control;
		case BTDebugNodeType::Condition:
			return syncnet::TreeNodeType_Condition;
		case BTDebugNodeType::Action:
		default:
			return syncnet::TreeNodeType_Action;
		}
	}
#endif
}

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

void Map::Init(const std::string& movementType)
{
	// 공유 네비메시 로드 후, 게임 모드가 지정한 이동 전략을 생성한다.
	navMesh_ = new NavMesh();
	navMesh_->Load("GameData/solo_navmesh.bin");
	movement_ = NavMovementFactory::Create(movementType, navMesh_);
	movement_->Init();
	gridManager_ = new GridManager(100, 100, 2);

	builderPtr_ = std::make_shared<send_message>();

	systemManager_ = new engine::SystemManager();

	auto& entityManager = systemManager_->GetEntityManager();

	// 모든 컴포넌트 타입 등록
	entityManager.RegisterComponent<engine::PositionComponent>();
	entityManager.RegisterComponent<engine::VelocityComponent>();
	entityManager.RegisterComponent<engine::HealthComponent>();
	entityManager.RegisterComponent<engine::PhysicsComponent>();
	entityManager.RegisterComponent<engine::AIComponent>();
	entityManager.RegisterComponent<engine::CollisionComponent>();
	entityManager.RegisterComponent<engine::InputComponent>();
	entityManager.RegisterComponent<engine::AnimationComponent>();
	entityManager.RegisterComponent<engine::TimerComponent>();
	entityManager.RegisterComponent<engine::ParticleComponent>();
	entityManager.RegisterComponent<engine::NetworkComponent>();
	entityManager.RegisterComponent<engine::StateComponent>();

	systemManager_->RegisterSystem<engine::TimerComponent>(
		[](float deltaTime, engine::TimerComponent& timer) {
			engine::TimerSystem::Update(deltaTime, timer);
		});

	Map* map = this;
	systemManager_->RegisterSystem<engine::StateComponent, engine::PositionComponent>(
		[map](float deltaTime, engine::StateComponent& state, engine::PositionComponent& position) {
			if (state.stateID == syncnet::AIState::AIState_Destroyed) {
				map->removedAgents_.push_back(state.ActorID);
			}

			if (map->movement_->IsActive(state.ActorID) == false)
				return;

			const float* npos = map->movement_->GetPos(state.ActorID);
			bool changed_position = !Vector3::equal(position.x, position.y, position.z, npos[0], npos[1], npos[2]);
			if (state.changeFlag == 0 && !changed_position)
				return;


			auto itr = map->actorMap_.find(state.ActorID);
			if (itr == map->actorMap_.end())
			{
				LOG.error("SendWorldState error agent not found in actorMap_");
				return;
			}
			auto actor = (Actor*)itr->second->get();

			if (changed_position)
			{
				actor->SetPosition(npos[0], npos[1], npos[2]);
				map->gridManager_->move(actor, npos[0], npos[2]);
			}

			map->agentInfoVector_.push_back(actor->GetActorInfo(*map->builderPtr_, actor->GetChangedFlag()));

			actor->ResetChangedFlag();
		});

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
	auto agents = builderPtr_->CreateVector(agentInfoVector_);

	// ----------------------------
	flatbuffers::Offset<syncnet::DebugRaycast> debug_raycast;
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debug_raycast_vector;
	for (int i = 0; i < this->raycasts_.size(); ++i)
	{
		debug_raycast = syncnet::CreateDebugRaycast(*builderPtr_, 0, &this->raycasts_[i]);
		debug_raycast_vector.push_back(debug_raycast);
	}
	this->raycasts_.clear();
	auto debug_raycasts = builderPtr_->CreateVector(debug_raycast_vector);
	// ----------------------------

	auto updateActorNotify = syncnet::CreateUpdateActorNotify(*builderPtr_, agents, debug_raycasts);

	auto send_msg = syncnet::CreateGameMessage(*builderPtr_, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builderPtr_->Finish(send_msg);

	SendBroadcast(builderPtr_);

	SendTreeDebugSync();

	for (auto& agent_id : removedAgents_)
	{
		OnRemoveAgent(agent_id);
	}

	builderPtr_ = std::make_shared<send_message>();
	agentInfoVector_.clear();
	removedAgents_.clear();
}

void Map::SendTreeDebugSync()
{
#if defined(ENABLE_BT_DEBUG)
	auto snapshot = BTDebugManager::Instance().ConsumeSnapshot();
	if (snapshot.empty())
		return;

	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::TreeDebugDefinition>> definition_vector;
	definition_vector.reserve(snapshot.definitions.size());
	for (const auto& definition : snapshot.definitions)
	{
		std::vector<flatbuffers::Offset<syncnet::TreeDebugNodeDefinition>> node_vector;
		node_vector.reserve(definition.nodes.size());
		for (const auto& node : definition.nodes)
		{
			auto name = builder_ptr->CreateString(node.name);
			node_vector.push_back(syncnet::CreateTreeDebugNodeDefinition(
				*builder_ptr,
				node.node_id,
				node.parent_node_id,
				name,
				ToTreeNodeType(node.node_type)));
		}

		auto tree_id = builder_ptr->CreateString(definition.tree_id);
		auto nodes = builder_ptr->CreateVector(node_vector);
		definition_vector.push_back(syncnet::CreateTreeDebugDefinition(
			*builder_ptr,
			tree_id,
			definition.monster_id,
			nodes));
	}

	std::vector<flatbuffers::Offset<syncnet::TreeDebugRuntimeFrame>> frame_vector;
	frame_vector.reserve(snapshot.frames.size());
	for (const auto& frame : snapshot.frames)
	{
		std::vector<flatbuffers::Offset<syncnet::TreeDebugNodeChange>> change_vector;
		change_vector.reserve(frame.changes.size());
		for (const auto& change : frame.changes)
		{
			auto name = builder_ptr->CreateString(change.node_name);
			auto reason = builder_ptr->CreateString(change.reason);
			change_vector.push_back(syncnet::CreateTreeDebugNodeChange(
				*builder_ptr,
				change.node_id,
				name,
				ToTreeNodeStatus(change.status),
				reason,
				change.success_count,
				change.failure_count,
				change.running_count));
		}

		auto tree_id = builder_ptr->CreateString(frame.tree_id);
		auto executed_path = builder_ptr->CreateVector(frame.executed_path);
		auto changes = builder_ptr->CreateVector(change_vector);
		frame_vector.push_back(syncnet::CreateTreeDebugRuntimeFrame(
			*builder_ptr,
			tree_id,
			frame.monster_id,
			frame.tick,
			frame.ai_state,
			frame.target_agent_id,
			executed_path,
			changes));
	}

	auto definition_offsets = builder_ptr->CreateVector(definition_vector);
	auto frame_offsets = builder_ptr->CreateVector(frame_vector);
	auto tree_debug_sync = syncnet::CreateTreeDebugSync(*builder_ptr, definition_offsets, frame_offsets);
	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_TreeDebugSync,
		tree_debug_sync.Union());
	builder_ptr->Finish(send_msg);

	SendBroadcast(builder_ptr);
#endif
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

void Map::join(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::join error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr != players_.end())
	{
		LOG.error("Map::join error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->GetPlayerId(), player));

	// 유닛 상태 동기화
	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agents;
	GetAgentsInfo(builder_ptr, agents);
	auto updateActorNotify = syncnet::CreateUpdateActorNotifyDirect(*builder_ptr, &agents, nullptr);
	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);
	player->Send(builder_ptr);
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
