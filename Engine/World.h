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
class Map;
namespace engine {
	class SystemManager;
}

class World
{
private:

	RandomUtil* randomUtil_;
	TimeStamp* timeStamp_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;
	std::list<std::shared_ptr<Map>> mapList_;


public:
	World();
	virtual ~World();

	void Init();
	void update(float deltaTime);

	RandomUtil* random_util() { return randomUtil_; }

	void SendWorldState();
	void GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector);

	std::shared_ptr<Actor> OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnRemoveAgent(int agent_id);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);


	void join(std::shared_ptr<Player> player);
	void leave(std::shared_ptr<Player> player);

	friend class Actor;
	friend class ActorFactory;
	friend class Monster;
	friend class Character;

};

