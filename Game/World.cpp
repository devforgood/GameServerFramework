#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <iostream>
#include "Server.h"
#include "Monster.h"
#include "Vector3Converter.h"
#include "LogHelper.h"

void World::Init()
{
	map_ = new Map();
	map_->Init();

}



void World::update(float deltaTime)
{
	for (std::list<std::shared_ptr<Monster>>::iterator itr = monsters_.begin();itr!=monsters_.end();++itr)
		(*itr)->Update();

	map_->update(deltaTime);
}

void World::SendWorldState(game_session* session)
{
	auto builder_ptr = std::make_shared<send_message>();
	flatbuffers::Offset<syncnet::AgentInfo> agent_info;
	std::vector<flatbuffers::Offset<syncnet::AgentInfo>> agent_info_vector;
	for (int i = 0; i < this->map()->crowd()->getAgentCount(); ++i)
	{
		const dtCrowdAgent* agent = this->map()->crowd()->getAgent(i);
		if (agent->active == false)
			continue;

		syncnet::Vec3 pos(agent->npos[0] * -1, agent->npos[1], agent->npos[2]);
		//std::cout << "agent " << agent->active << " pos (" << pos.x() << "," << pos.y() << "," << pos.z() << ")" << std::endl;

		agent_info = syncnet::CreateAgentInfo(*builder_ptr, i, &pos);
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

void World::OnAddMonster(const syncnet::Vec3* pos)
{
	int agent_id = this->map()->addAgent(Vector3Converter(pos).pos());
	if (agent_id < 0)
	{
		LOG.error("OnAddMonster error in Map.addAgent()");
		return;
	}

	if (monsters_map_.find(agent_id) != monsters_map_.end())
	{
		LOG.error("OnAddMonster error already exist in monsters_map_");
		return;
	}
	
	auto monster = std::make_shared<Monster>(agent_id);
	auto itr = monsters_.insert(monsters_.end(), monster);
	monsters_map_.insert(std::make_pair(agent_id, itr));
}

void World::OnRemoveMonster(int agent_id)
{
	auto itr = monsters_map_.find(agent_id);
	if (itr == monsters_map_.end())
	{
		LOG.error("OnRemoveMonster error not exist in monsters_map_");
		return;
	}
	monsters_.erase(itr->second);
	monsters_map_.erase(itr);

	this->map()->removeAgent(agent_id);

}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	this->map()->setMoveTarget(Vector3Converter(pos).pos(), false);

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
