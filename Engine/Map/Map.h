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
namespace engine {
	class SystemManager;
}
class World;
class NavMesh;
class INavMovement;

class Map
{
private:
	World* world_;
	NavMesh* navMesh_;
	INavMovement* movement_;
	std::list<std::shared_ptr<Actor>> actorList_;
	std::unordered_map<int, std::list<std::shared_ptr<Actor>>::iterator> actorMap_;

	std::vector<syncnet::Vec3> raycasts_;
	std::shared_ptr<send_message> builderPtr_;
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agentInfoVector_;
	std::vector<int> removedAgents_;

	// DetectEnemy 의 시야 쿼리 결과 재사용 버퍼(호출당 힙 할당 방지).
	std::vector<IGridActor*> detectScratch_;


	GridManager* gridManager_;


	engine::SystemManager* systemManager_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;


public:
	Map(World* world);
	virtual ~Map();

	void Init(const std::string& movementType);
	void update(float deltaTime);

	// update(deltaTime) 를 구성하는 단계들. 단계별 프로파일링을 위해 분리해 노출한다.
	// update() 는 이들을 순서대로 호출할 뿐이라 직접 호출해도 동작은 동일하다.
	void UpdateActors(float deltaTime);
	void UpdateMovement(float deltaTime);
	void UpdateSystems(float deltaTime);

	// 프로파일링용: 모든 액터에 대해 DetectEnemy 를 1회씩 호출하고 탐지 성공 수를 반환한다.
	// BehaviorTree tick 안에서 적 탐지(그리드 쿼리)가 차지하는 비용을 격리 측정하기 위한 훅.
	int ProfileDetectEnemyAll();

	World* world() { return world_; }
	INavMovement* GetNavMap() { return movement_; }

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

	std::shared_ptr<Player> FindPlayer(long player_id);
	std::shared_ptr<Actor> FindActor(int actor_id);

	friend class Actor;
	friend class ActorFactory;
	friend class Monster;
	friend class Character;
};

