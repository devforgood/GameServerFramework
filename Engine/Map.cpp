#include "Map.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
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
#include "NavMap.h"
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
	map_ = nullptr;
	gridManager_ = nullptr;

	systemManager_ = nullptr;
}

Map::~Map()
{
	if (map_)
	{
		delete map_;
		map_ = nullptr;
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

void Map::Init()
{
	map_ = new NavMap();
	map_->Init();
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
				map->removedAgents_.push_back(state.agentID);
			}

			const dtCrowdAgent* agent = map->map_->crowd()->getAgent(state.agentID);
			if (agent->active == false)
				return;

			bool changed_position = !Vector3::equal(position.x, position.y, position.z, agent->npos[0], agent->npos[1], agent->npos[2]);
			if (state.changeFlag == 0 && !changed_position)
				return;


			auto itr = map->actorMap_.find(state.agentID);
			if (itr == map->actorMap_.end())
			{
				LOG.error("SendWorldState error agent not found in actorMap_");
				return;
			}
			auto actor = (Actor*)itr->second->get();

			if (changed_position)
			{
				actor->set_position(agent->npos[0], agent->npos[1], agent->npos[2]);
				map->gridManager_->move(actor, agent->npos[0], agent->npos[2]);
			}

			map->agentInfoVector_.push_back(actor->get_actor_info(*map->builderPtr_, actor->get_changed_flag()));

			actor->reset_changed();
		});

}

void Map::update(float deltaTime)
{

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
		(*itr)->update(deltaTime);

	map_->update(deltaTime);

	systemManager_->Update(deltaTime);

	SendWorldState();
	//LOG.info("World update end");
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

	if (itr->second->get()->type() == syncnet::GameObjectType_Character)
	{
		auto character = std::dynamic_pointer_cast<Character>(*itr->second);

		auto itr_player = players_.find(character->player_id());
		if (itr_player != players_.end())
		{
			players_.erase(itr_player);
		}
	}

	gridManager_->remove((Actor*)itr->second->get());
	actorList_.erase(itr->second);
	actorMap_.erase(itr);

	map_->removeAgent(agent_id);

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
	actorMap_.insert(std::make_pair(actor->agent_id(), itr));
	actor->set_changed(static_cast<long>(GameObjectChangeType::All));

	return actor;
}


void Map::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->GetNavMap()->setMoveTarget(Vector3(pos).pos(), false, agent_id);

}

void Map::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->GetNavMap()->raycast(0, Vector3(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

int Map::DetectEnemy(Actor* actor)
{
	const dtCrowdAgent* this_agent = this->GetNavMap()->crowd()->getAgent(actor->agent_id());
	float hitPoint[3];

	auto targets = gridManager_->getEntitiesInViewRange(actor, g_fDistance);

	for (auto itr = targets.begin(); itr != targets.end(); ++itr)
	{
		if (!(*itr)->isCharacter())
			continue;

		const dtCrowdAgent* agent = this->GetNavMap()->crowd()->getAgent((*itr)->getAgentID());


		if (this->GetNavMap()->raycast(actor->agent_id(), agent->npos, hitPoint) == false)
		{
			return (*itr)->getAgentID();
		}
	}
	return -1;
}

std::vector<IGridActor*> Map::get_actors_in_range(Actor* actor, float range, float dirDeg, float angle)
{
	return gridManager_->getEntitiesInAoEMask(actor->get_vecter2_x(), actor->get_vecter2_y(), range, dirDeg, angle);
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

void Map::GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector)
{
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		auto actor = itr->get();
		agent_info_vector.push_back(actor->get_actor_info(*msg, static_cast<long>(GameObjectChangeType::All)));
	}
}
