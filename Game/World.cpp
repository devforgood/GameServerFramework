#include "World.h"
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
#include "GridManager.h"
#include "GameObjectFactory.h"
#include "TimeStamp.h"
#include "RandomUtil.h"

//const float g_fDistance = std::powf(10.0f, 2);
const float g_fDistance = 10.0f;

World::World()
{
	map_ = nullptr;
	grid_manager_ = nullptr;
	time_stamp_ = nullptr;
	random_util_ = nullptr;
}

World::~World()
{
	if (map_)
	{
		delete map_;
		map_ = nullptr;
	}
	if (grid_manager_)
	{
		delete grid_manager_;
		grid_manager_ = nullptr;
	}
	if (time_stamp_)
	{
		delete time_stamp_;
		time_stamp_ = nullptr;
	}
	if (random_util_)
	{
		delete random_util_;
		random_util_ = nullptr;
	}
}

void World::Init()
{
	map_ = new Map();
	map_->Init();
	Monster::Initialize("mob.lua");
	Monster::registerLuaFunctionAll();
	grid_manager_ = new GridManager(100, 100, 2);
	time_stamp_ = new TimeStamp();
	random_util_ = new RandomUtil();
}

void World::update(float deltaTime)
{
	time_stamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin();itr!= game_object_list_.end();++itr)
		(*itr)->update(deltaTime);

	map_->update(deltaTime);
	SendWorldState();
	//LOG.info("World update end");
}

void World::join(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::join error player is nullptr");
		return;
	}
	auto itr = players_.find(player->player_id());
	if (itr != players_.end())
	{
		LOG.error("World::join error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->player_id(), player));

	// 유닛 상태 동기화
	auto builder_ptr = std::make_shared<send_message>();
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agents;
	GetAgentsInfo(builder_ptr, agents);
	auto updateActorNotify = syncnet::CreateUpdateActorNotifyDirect(*builder_ptr, &agents, nullptr);
	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);
	player->send(builder_ptr);
}

void World::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->player_id());
	if (itr == players_.end())
	{
		LOG.error("World::leave error player not found");
		return;
	}
	players_.erase(itr);
}

void World::GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector)
{
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		auto game_object = itr->get();
		agent_info_vector.push_back(game_object->get_actor_info(*msg, static_cast<long>(GameObjectChangeType::All)));
	}
}

void World::SendWorldState()
{
	auto builder_ptr = std::make_shared<send_message>();
	flatbuffers::Offset<syncnet::ActorInfo> agent_info;
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agent_info_vector;
	std::vector<int> removed_agents;
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		auto game_object = itr->get();
		if (game_object->state() == syncnet::AIState::AIState_Destroyed) {
			removed_agents.push_back(game_object->agent_id());
		}

		const dtCrowdAgent* agent = this->map()->crowd()->getAgent(game_object->agent_id());
		if (agent->active == false)
			continue;

		if (game_object->is_changed_position(agent->npos[0], agent->npos[1], agent->npos[2]))
		{
			game_object->set_position(agent->npos[0], agent->npos[1], agent->npos[2]);
			grid_manager_->move((Actor*)itr->get(), agent->npos[0], agent->npos[2]);
		}
		
		if (!game_object->is_changed()) 
			continue;


		agent_info_vector.push_back(game_object->get_actor_info(*builder_ptr, game_object->get_changed_flag()));

		game_object->reset_changed();
	}
	auto agents = builder_ptr->CreateVector(agent_info_vector);

	// ----------------------------
	flatbuffers::Offset<syncnet::DebugRaycast> debug_raycast;
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debug_raycast_vector;
	for (int i = 0; i < this->raycasts_.size(); ++i)
	{
		debug_raycast = syncnet::CreateDebugRaycast(*builder_ptr, 0, &this->raycasts_[i]);
		debug_raycast_vector.push_back(debug_raycast);
	}
	this->raycasts_.clear();
	auto debug_raycasts = builder_ptr->CreateVector(debug_raycast_vector);
	// ----------------------------

	auto updateActorNotify = syncnet::CreateUpdateActorNotify(*builder_ptr, agents, debug_raycasts);

	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
	builder_ptr->Finish(send_msg);

	SendBroadcast(builder_ptr);

	for (auto& agent_id : removed_agents)
	{
		OnRemoveAgent(agent_id);
	}
}

void World::SendBroadcast(std::shared_ptr<send_message> msg) 
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		itr->second->send(msg);
	}
}
void World::SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except)
{
	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		if (itr->second.get() == except.get())
			continue;

		itr->second->send(msg);
	}
}

std::shared_ptr<GameObject> World::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	auto game_object = GameObjectFactory::CreateGameObject(this, player, type, pos);
	if (game_object == nullptr)
	{
		LOG.error("OnAddAgent error in GameObjectFactory::CreateGameObject()");
		return nullptr;
	}

	auto itr = game_object_list_.insert(game_object_list_.end(), game_object);
	game_object_map_.insert(std::make_pair(game_object->agent_id(), itr));
	game_object->set_changed(static_cast<long>(GameObjectChangeType::All));

	return game_object;
}

void World::OnRemoveAgent(int agent_id)
{
	auto itr = game_object_map_.find(agent_id);
	if (itr == game_object_map_.end())
	{
		LOG.error("OnRemoveAgent error not exist in monsters_map_");
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

	grid_manager_->remove((Actor*)itr->second->get());
	game_object_list_.erase(itr->second);
	game_object_map_.erase(itr);

	this->map()->removeAgent(agent_id);

}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->map()->setMoveTarget(Vector3(pos).pos(), false, agent_id);

}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->map()->raycast(0, Vector3(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

int World::DetectEnemy(Actor * actor)
{
	const dtCrowdAgent* this_agent = this->map()->crowd()->getAgent(actor->agent_id());
	float hitPoint[3];

	auto targets = grid_manager_->getEntitiesInViewRange(actor, g_fDistance);

	for (auto itr = targets.begin(); itr != targets.end(); ++itr)
	{
		if ((*itr)->type() != syncnet::GameObjectType_Character)
			continue;

		const dtCrowdAgent* agent = this->map()->crowd()->getAgent((*itr)->agent_id());


		if (this->map()->raycast(actor->agent_id(), agent->npos, hitPoint) == false)
		{
			return (*itr)->agent_id();
		}
	}
	return -1;
}




std::vector<Actor*> World::get_actors_in_range(Actor* actor, float range, float angle) 
{ 
	return grid_manager_->getEntitiesInAoEMask(actor->get_vecter2_x(), actor->get_vecter2_y(), range, angle);
}