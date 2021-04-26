#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <iostream>
#include "Server.h"
#include "Monster.h"
#include "Character.h"
#include "Vector3Converter.h"
#include "LogHelper.h"
#include "DetourCommon.h"

void World::Init()
{
	map_ = new Map();
	map_->Init();

}



void World::update(float deltaTime)
{
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin();itr!= game_object_list_.end();++itr)
		(*itr)->Update();

	map_->update(deltaTime);
}

void World::SendWorldState(game_session* session)
{
	auto builder_ptr = std::make_shared<send_message>();
	flatbuffers::Offset<syncnet::AgentInfo> agent_info;
	std::vector<flatbuffers::Offset<syncnet::AgentInfo>> agent_info_vector;
	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		const dtCrowdAgent* agent = this->map()->crowd()->getAgent(itr->get()->agent_id());
		if (agent->active == false)
			continue;

		syncnet::Vec3 pos(agent->npos[0] * -1, agent->npos[1], agent->npos[2]);
		//std::cout << "agent " << agent->active << " pos (" << pos.x() << "," << pos.y() << "," << pos.z() << ")" << std::endl;

		agent_info = syncnet::CreateAgentInfo(*builder_ptr, itr->get()->agent_id(), &pos, itr->get()->GetType(), itr->get()->state());
		agent_info_vector.push_back(agent_info);
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

	auto getAgents = syncnet::CreateGetAgents(*builder_ptr, agents, debug_raycasts);

	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_GetAgents, getAgents.Union());
	builder_ptr->Finish(send_msg);

	session->send(builder_ptr);
}

void World::OnAddAgent(syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	float speed = 3.5f;
	if (type == syncnet::GameObjectType::GameObjectType_Character)
		speed = 4.5f;

	int agent_id = this->map()->addAgent(Vector3Converter(pos).pos(), speed);
	if (agent_id < 0)
	{
		LOG.error("OnAddAgent error in Map.addAgent()");
		return;
	}

	if (game_object_map_.find(agent_id) != game_object_map_.end())
	{
		LOG.error("OnAddAgent error already exist in monsters_map_");
		return;
	}
	
	std::shared_ptr<GameObject> game_object;

	switch (type)
	{
	case syncnet::GameObjectType::GameObjectType_Character:
		game_object = std::make_shared<Character>(agent_id, this);
		break;
	case syncnet::GameObjectType::GameObjectType_Monster:
		game_object = std::make_shared<Monster>(agent_id, this);
		break;
	}

	auto itr = game_object_list_.insert(game_object_list_.end(), game_object);
	game_object_map_.insert(std::make_pair(agent_id, itr));
}

void World::OnRemoveAgent(int agent_id)
{
	auto itr = game_object_map_.find(agent_id);
	if (itr == game_object_map_.end())
	{
		LOG.error("OnRemoveAgent error not exist in monsters_map_");
		return;
	}
	game_object_list_.erase(itr->second);
	game_object_map_.erase(itr);

	this->map()->removeAgent(agent_id);

}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->map()->setMoveTarget(Vector3Converter(pos).pos(), false, agent_id);

}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->map()->raycast(0, Vector3Converter(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

int World::DetectEnemy(int agent_id)
{
	const dtCrowdAgent* this_agent = this->map()->crowd()->getAgent(agent_id);
	float hitPoint[3];


	for (std::list<std::shared_ptr<GameObject>>::iterator itr = game_object_list_.begin(); itr != game_object_list_.end(); ++itr)
	{
		if (itr->get()->GetType() != syncnet::GameObjectType_Character)
			continue;

		const dtCrowdAgent* agent = this->map()->crowd()->getAgent(itr->get()->agent_id());

		if (dtVdistSqr(this_agent->npos, agent->npos) > 100.0f)
			continue;

		if (this->map()->raycast(agent_id, agent->npos, hitPoint) == false)
		{
			return itr->get()->agent_id();
		}
	}
	return -1;
}
