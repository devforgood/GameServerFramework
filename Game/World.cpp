#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <iostream>
#include "Server.h"

void World::Init()
{
	m_map = new Map();
	m_map->Init();

}

void World::update(float deltaTime)
{
	m_map->update(deltaTime);
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
	auto getAgents = syncnet::CreateGetAgents(*builder_ptr, agents);

	auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_GetAgents, getAgents.Union());
	builder_ptr->Finish(send_msg);

	session->send(builder_ptr);
}