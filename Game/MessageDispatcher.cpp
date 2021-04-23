#include "MessageDispatcher.h"
#include <iostream>
#include "World.h"
#include "Vector3Converter.h"
#include "DetourNavMeshQuery.h"
#include "LogHelper.h"


void MessageDispatcher::dispatch(const syncnet::AddAgent* msg)
{
	LOG.info("add agent pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
	world_->map()->addAgent(Vector3Converter(msg->pos()).pos());
}

void MessageDispatcher::dispatch(const syncnet::RemoveAgent* msg)
{
	LOG.info("remove agent id :{}", msg->agentId());
	world_->map()->removeAgent(msg->agentId());
}

void MessageDispatcher::dispatch(const syncnet::SetMoveTarget* msg)
{
	LOG.info("move target agent id :{}, pos:({},{},{})", msg->agentId(), msg->pos()->x(), msg->pos()->y(), msg->pos()->z());
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
	LOG.info("SetRaycast pos:({},{},{})", msg->pos()->x(), msg->pos()->y(), msg->pos()->z());

	float hitPoint[3];
	if (world_->map()->raycast(0, Vector3Converter(msg->pos()).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		world_->raycasts.push_back(pos);
	}




}