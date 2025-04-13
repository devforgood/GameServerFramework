#pragma once
#include <vector>
#include <list>
#include <unordered_map>
#include <memory>
#include "Map.h"
#include "syncnet_generated.h"

class game_session;
class Monster;
class GameObject;
class Character;
class Player;

class World
{
private:

	Map* map_;
	std::list<std::shared_ptr<GameObject>> game_object_list_;
	std::unordered_map<int, std::list<std::shared_ptr<GameObject>>::iterator> game_object_map_;

	std::vector<syncnet::Vec3> raycasts_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;

public:

	void Init();
	void update(float deltaTime);

	Map* map() { return map_; }

	void SendWorldState();

	void OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnRemoveAgent(int agent_id);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);

	int DetectEnemy(int agent_id);

};

