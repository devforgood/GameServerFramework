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
#include "ActorFactory.h"
#include "Common.h"
#include "Map.h"



World::World()
{
	random_util_ = nullptr;
	time_stamp_ = nullptr;
}

World::~World()
{
	if (random_util_)
	{
		delete random_util_;
		random_util_ = nullptr;
	}
	if (time_stamp_)
	{
		delete time_stamp_;
		time_stamp_ = nullptr;
	}
}

void World::Init()
{
	Monster::Initialize("mob.lua");
	Monster::registerLuaFunctionAll();

	random_util_ = new RandomUtil();
	time_stamp_ = new TimeStamp();

	// Initialize maps
	std::shared_ptr<Map> map = std::make_shared<Map>(this);
	map->Init();
	map_list_.push_back(map);
}

void World::update(float deltaTime)
{
	time_stamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Map>>::iterator itr = map_list_.begin();itr!= map_list_.end();++itr)
		(*itr)->update(deltaTime);
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

	// todo : map 선택 로직 추가
	map_list_.begin()->get()->join(player);
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
	// todo : map 선택 로직 추가
	map_list_.begin()->get()->leave(player);
}



std::shared_ptr<Actor> World::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	return map_list_.begin()->get()->OnAddAgent(player, type, pos);
}

void World::OnRemoveAgent(int agent_id)
{
	// todo : map 선택 로직 추가
	map_list_.begin()->get()->OnRemoveAgent(agent_id);
}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	map_list_.begin()->get()->OnSetMoveTarget(agent_id, pos);
}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	map_list_.begin()->get()->OnSetRaycast(pos);
}