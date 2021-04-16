#include "MessageDispatcher.h"
#include <iostream>
#include "World.h"

void MessageDispatcher::dispatch(const syncnet::AddAgent* msg)
{
	std::cout << "x : " << msg->pos()->x() << std::endl;
	std::cout << "y : " << msg->pos()->y() << std::endl;
	std::cout << "z : " << msg->pos()->z() << std::endl;

	float* v = new float[3];
	v[0] = msg->pos()->x() * -1;
	v[1] = msg->pos()->y();
	v[2] = msg->pos()->z();
	world_->map()->addAgent(v);
}

void MessageDispatcher::dispatch(const syncnet::RemoveAgent* msg)
{
	//std::cout << "agent id : " << msg->agentId() << std::endl;
}

void MessageDispatcher::dispatch(const syncnet::SetMoveTarget* msg)
{
	std::cout << "agent id : " << msg->agentId() << std::endl;
	std::cout << "x : " << msg->pos()->x() << std::endl;
	std::cout << "y : " << msg->pos()->y() << std::endl;
	std::cout << "z : " << msg->pos()->z() << std::endl;

	float* v = new float[3];
	v[0] = msg->pos()->x() * -1;
	v[1] = msg->pos()->y();
	v[2] = msg->pos()->z();
	world_->map()->setMoveTarget(v, false);
}

void MessageDispatcher::dispatch(const syncnet::GetAgents* msg)
{
}

void MessageDispatcher::dispatch(const syncnet::Ping* msg)
{
	//std::cout << "ping seq : " << msg->seq() << std::endl;
}
