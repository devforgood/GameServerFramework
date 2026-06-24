#pragma once
#include <vector>
#include <list>
#include <unordered_map>
#include <memory>
#include <string>
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
class GameMode;
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

	std::unique_ptr<GameMode> gameMode_;


public:
	World();
	virtual ~World();

	// movementOverride 가 비어있지 않으면 게임 모드 데이터의 이동 전략 대신
	// 해당 전략("crowd"/"waypoint")을 강제한다(벤치마크/테스트용).
	void Init(const std::string& movementOverride = "");
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

