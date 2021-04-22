#include "MessageDispatcher.h"
#include <iostream>
#include "World.h"
#include "Vector3Converter.h"
#include "DetourNavMeshQuery.h"



void MessageDispatcher::dispatch(const syncnet::AddAgent* msg)
{
	std::cout << "add agent pos : (" << msg->pos()->x() << ", " << msg->pos()->y() << ", " << msg->pos()->z() << ")" << std::endl;
	world_->map()->addAgent(Vector3Converter(msg->pos()).pos());
}

void MessageDispatcher::dispatch(const syncnet::RemoveAgent* msg)
{
	//std::cout << "agent id : " << msg->agentId() << std::endl;
}

void MessageDispatcher::dispatch(const syncnet::SetMoveTarget* msg)
{
	std::cout << "move target agent id : " << msg->agentId() << " pos : (" << msg->pos()->x() << ", " << msg->pos()->y() << ", " << msg->pos()->z() << ")" << std::endl;
	world_->map()->setMoveTarget(Vector3Converter(msg->pos()).pos(), false);
}

void MessageDispatcher::dispatch(const syncnet::GetAgents* msg)
{
}

void MessageDispatcher::dispatch(const syncnet::Ping* msg)
{
	//std::cout << "ping seq : " << msg->seq() << std::endl;
}


void MessageDispatcher::dispatch(const syncnet::SetRaycast* msg)
{
	std::cout << "SetRaycast pos : (" << msg->pos()->x() << "," << msg->pos()->y() << "," << msg->pos()->z() << ")" << std::endl;

	float hitPoint[3];
	if (world_->map()->raycast(0, Vector3Converter(msg->pos()).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		world_->raycasts.push_back(pos);
	}




}