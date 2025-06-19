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
class GridManager;
class Actor;
class GameObjectFactory;
class TimeStamp;
class send_message;
class RandomUtil;
class IGridActor;

class World
{
private:

	Map* map_;
	std::list<std::shared_ptr<GameObject>> game_object_list_;
	std::unordered_map<int, std::list<std::shared_ptr<GameObject>>::iterator> game_object_map_;

	std::vector<syncnet::Vec3> raycasts_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;

	GridManager* grid_manager_;
	TimeStamp* time_stamp_;
	RandomUtil* random_util_;

public:
	World();
	virtual ~World();

	void Init();
	void update(float deltaTime);

	Map* map() { return map_; }
	RandomUtil* random_util() { return random_util_; }

	void SendWorldState();
	void GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector);

	std::shared_ptr<GameObject> OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnRemoveAgent(int agent_id);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);

	int DetectEnemy(Actor* actor);
	void SendBroadcast(std::shared_ptr<send_message> msg);	
	void SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except);
	std::vector<IGridActor*> get_actors_in_range(Actor* actor, float range, float dirDeg, float angle);

	void join(std::shared_ptr<Player> player);
	void leave(std::shared_ptr<Player> player);

	friend class Actor;
	friend class GameObjectFactory;
	friend class Monster;
	friend class Character;

};

