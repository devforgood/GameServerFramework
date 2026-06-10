#pragma once
#include <vector>
#include <list>
#include <unordered_map>
#include <memory>
#include "NavMap.h"
#include "syncnet_generated.h"

class GameSession;
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
namespace Engine {
	class SystemManager;
}
class World;
class NavMap;

class Map
{
private:
	World* world_;
	NavMap* map_;
	std::list<std::shared_ptr<Actor>> actor_list_;
	std::unordered_map<int, std::list<std::shared_ptr<Actor>>::iterator> actor_map_;

	std::vector<syncnet::Vec3> raycasts_;
	std::shared_ptr<send_message> builder_ptr_;
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agent_info_vector_;
	std::vector<int> removed_agents_;


	GridManager* grid_manager_;


	Engine::SystemManager* system_manager_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;


public:
	Map(World* world);
	virtual ~Map();

	void Init();
	void update(float deltaTime);

	World* world() { return world_; }
	NavMap* GetNavMap() { return map_; }

	void SendWorldState();
	void SendTreeDebugSync();
	void SendBroadcast(std::shared_ptr<send_message> msg);
	void SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except);
	void OnRemoveAgent(int agent_id);
	std::shared_ptr<Actor> OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);
	int DetectEnemy(Actor* actor);
	std::vector<IGridActor*> get_actors_in_range(Actor* actor, float range, float dirDeg, float angle);
	void GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector);

	void join(std::shared_ptr<Player> player);
	void leave(std::shared_ptr<Player> player);

	friend class Actor;
	friend class ActorFactory;
	friend class Monster;
	friend class Character;
};

