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
namespace gamedata {
	struct Map;
}

class Map
{
private:
	World* world_;
	const gamedata::Map* mapData_ = nullptr;
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

	// 맵을 초기화한다. 맵별 네비메시 로드에 실패하면(경로 미지정/파일 없음 등)
	// false 를 반환한다 — 호출자는 해당 맵을 등록하지 말아야 한다.
	bool Init(const std::string& movementType, const gamedata::Map* mapData = nullptr);
	void update(float deltaTime);

	const gamedata::Map* GetMapData() const { return mapData_; }
	int GetMapId() const;

	// 맵 데이터의 첫 번째 player_spawn 위치(클라 좌표계). 없으면 (0,0,0).
	syncnet::Vec3 GetPlayerSpawnPos() const;

	// Map.json(클라 씬 좌표)의 게이트/스폰 위치가 서버 navmesh 위에 있는지 검증한다.
	// 씬과 navmesh가 어긋나면 마커가 메시 밖에 놓이므로 정합 이상을 로드 시점에 잡아낸다.
	// 어긋난 마커 수를 반환한다(경고 로그만 남기고 로드는 막지 않는다).
	// static 버전은 맵 인스턴스 없이도(예: 단위 테스트) 데이터+navmesh 만으로 검증할 때 사용한다.
	static int ValidateMapDataOnNavMesh(const gamedata::Map* mapData, const NavMesh* navMesh);
	int ValidateMapDataOnNavMesh() const { return ValidateMapDataOnNavMesh(mapData_, navMesh_); }

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

	// join 을 두 단계로 분리한 것. 게이트 이동 시 클라가 먼저 응답을 받아 맵을 교체한 뒤
	// 동기화를 받도록, players_ 등록(Enter)과 상태 전송(SendStateTo)을 별도로 호출한다.
	void Enter(std::shared_ptr<Player> player);
	void SendStateTo(std::shared_ptr<Player> player);

	std::shared_ptr<Player> FindPlayer(long player_id);
	std::shared_ptr<Actor> FindActor(int actor_id);

	friend class Actor;
	friend class ActorFactory;
	friend class Monster;
	friend class Character;
};

