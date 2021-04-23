#pragma once
#include <vector>
#include <list>
#include <map>
#include <memory>
#include "Map.h"

class game_session;
class Monster;
namespace syncnet
{
	struct Vec3;
}

class World
{
private:

	Map* map_;
	std::list<std::shared_ptr<Monster>> monsters_;
	std::map<int, std::list<std::shared_ptr<Monster>>::iterator> monsters_map_;

	std::vector<syncnet::Vec3> raycasts_;

public:

	void Init();
	void update(float deltaTime);

	Map* map() { return map_; }

	void SendWorldState(game_session* session);

	void OnAddMonster(const syncnet::Vec3* pos);
	void OnRemoveMonster(int agent_id);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);

};

