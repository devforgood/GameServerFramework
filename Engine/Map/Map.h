#pragma once
#include <vector>
#include <list>
#include <unordered_map>
#include <unordered_set>
#include <memory>
#include <string>
#include "syncnet_generated.h"
#include "MonsterSpawner.h"

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
	struct StateComponent;
	struct PositionComponent;
}
namespace monsterai {
	class MonsterAISystem;
}
class World;
class NavMesh;
class INavMovement;
class GameMode;
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
	std::shared_ptr<send_message> sendMessageBuilder_;
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> actorPendingUpdates_;
	std::vector<int> removedAgents_;

	// DetectEnemy 의 시야 쿼리 결과 재사용 버퍼(호출당 힙 할당 방지).
	std::vector<IGridActor*> detectScratch_;

	// ── 관심영역(AoI) ──
	// 플레이어는 자기 캐릭터 주변 셀을 구독하고, 액터 변경은 그 액터가 있는 셀의 구독자에게만 간다.
	// 덕분에 전송량이 '맵 전체 액터 × 전체 플레이어'가 아니라 '주변 액터 × 그 자리를 보는 사람'이 된다.
	// 설계 배경과 전체 흐름은 AOI_DESIGN.md 참고.
	//
	// 상태는 통째로 별도 객체(AoIState)에 두고 쓰는 맵에서만 할당한다. 매 틱 액터마다 도는
	// SyncActorState 가 Map 의 멤버 배치에 민감해서, 여기에 컨테이너를 직접 늘리면
	// AoI 를 끄고도 틱이 느려졌다(10,000마리 기준 +6ms). 켜짐 여부는 bool 하나로만 본다.
	struct AoIState;
	std::unique_ptr<AoIState> aoi_;
	bool aoiEnabled_ = false;

	float AoIRadius() const;
	void RefreshAoIMode();

	// 인구가 바뀌는 자리(입장/퇴장)에서 부른다. 필요하면 모드를 바꾼다.
	void UpdateAoIMode();
	void EnableAoI();
	void DisableAoI();
	void SubscribeViewer(long playerId, Actor* character, bool sendSnapshot);
	void UnsubscribeViewer(long playerId);
	void OnViewerCellChanged(long playerId, Actor* character);
	void OnActorCellChanged(Actor* actor, int oldCellX, int oldCellY, int newCellX, int newCellY);
	void QueueUpdate(Actor* actor);

	// 좌표를 직접 써서 옮기는 경로(순간이동)가 관심영역 장부까지 갱신하도록 묶어 둔 것.
	// 평소 이동은 SyncActorState 가 같은 규칙을 인라인으로 수행한다(매 틱 액터마다 도는
	// 자리라 분기 하나가 그대로 비용이 된다).
	void MoveActorAndUpdateView(Actor* actor, float x, float y, float z);
	void SendPendingViews();

	// Init 을 구성하는 단계들. Init 은 이들을 순서대로 호출하는 오케스트레이터다.
	// 내비게이션: 맵별 navmesh 로드/정합 검증 + 이동 전략 생성. 실패 시 false.
	bool InitNavigation(const std::string& movementType);
	// 공간 분할 그리드: 네비메시 실제 범위에 맞춰 만든다(InitNavigation 이후에 호출해야 한다).
	GridManager* CreateGrid() const;
	// ECS: SystemManager 생성, 컴포넌트 등록(engine::RegisterGameComponents), 시스템 등록.
	void InitEcs();

	// 매 틱 ECS 시스템으로 실행: 이동 시뮬레이션 결과/상태 변경을 액터에 반영하고
	// 브로드캐스트 대기열(actorPendingUpdates_)에 누적한다. Destroyed 상태는 제거 목록에 수집.
	void SyncActorState(engine::StateComponent& state, engine::PositionComponent& position);

	// 부활 대기가 끝난 플레이어를 스폰 지점에 되살린다.
	void RespawnPlayer(long playerId);

	// 마커 하나가 요구하는 위치에 몬스터 한 마리를 세운다(최초 스폰/리스폰 공용).
	// 성공하면 actor id, 실패하면 -1. 좌표는 클라이언트 좌표계다.
	int SpawnMonsterAt(const gamedata::MapSpawnPointsMonsterSpawn& marker,
		double x, double y, double z);

	// 사망 시 적용할 부활 대기 시간(초). 게임 모드 규칙이 있으면 그 값, 없으면 기본값.
	float ResolveRespawnSeconds() const;

	// 맵을 떠난 플레이어의 사망/부활 대기 상태를 지운다.
	void ForgetPlayerModeState(long playerId);

	// ── 게임 모드 ──
	// 이 맵에서 진행 중인 모드. Init 이 맵 데이터의 game_mode_id 로 만든다.
	// field 맵은 서버가 사는 동안 하나가 유지되고, instance 맵은 인스턴스마다 새로 생긴다.
	std::unique_ptr<GameMode> gameMode_;

	// 0 이면 상시(field) 맵, 1 이상이면 입장할 때 만들어진 인스턴스다.
	int instanceId_ = 0;

	// GM_SpawnBoss 로 스폰한 보스 액터 id(-1 이면 없음).
	int bossActorId_ = -1;

	// monster_spawn 마커별 스포너(수량 유지 + 리스폰 주기). SpawnMonstersFromData 가 만들고
	// UpdateMonsterSpawns 가 매 틱 돌린다. 마커가 없는 맵에서는 비어 있어 비용이 없다.
	MonsterSpawner monsterSpawner_;

	// 사망 처리를 플레이어당 한 번만 하기 위한 집합(플레이어 id).
	std::unordered_set<long> deadPlayers_;

	// 부활 대기: 플레이어 id -> 남은 시간(초).
	std::unordered_map<long, float> pendingRespawns_;


	GridManager* gridManager_;


	engine::SystemManager* systemManager_;

	// 몬스터 AI(ECS 백엔드). 결정은 공유 결정표에서 나오고 개체는 상태 컴포넌트만 갖는다.
	//
	// 소유권은 systemManager_ 에 있다(PreMovement 단계로 등록). 여기 있는 것은 조회용
	// 포인터다 — 몬스터가 스폰/소멸하며 자기 컴포넌트를 등록·반납하려고 찾아온다.
	// 그래서 액터보다 오래 살아야 하는데, systemManager_ 가 소유하므로 그 안의
	// EntityManager 와 정확히 같은 수명이 된다(액터가 이미 의존하는 그 수명이다).
	monsterai::MonsterAISystem* aiSystem_ = nullptr;

	std::unordered_map<long, std::shared_ptr<Player>> players_;

	// 브로드캐스트 순회 중에는 players_ 도 관심영역 슬롯도 건드릴 수 없다. 순회하면서
	// 부르는 Player::Send 가 세션을 끊으면 그 끝이 leave 이기 때문이다. 순회 깊이를 세고,
	// 그 사이에 들어온 leave 는 미뤘다가 순회가 끝나는 자리에서 처리한다.
	int broadcastDepth_ = 0;
	std::vector<std::shared_ptr<Player>> deferredLeaves_;

	void DrainDeferredLeaves();

	// 위 깊이를 RAII 로 센다. 중첩될 수 있으므로(SendStateTo -> SendPendingViews)
	// 마지막 스코프가 닫힐 때만 미뤄 둔 leave 를 처리한다.
	struct BroadcastScope
	{
		explicit BroadcastScope(Map& map) : map_(map) { ++map_.broadcastDepth_; }
		~BroadcastScope() { if (--map_.broadcastDepth_ == 0) map_.DrainDeferredLeaves(); }
		BroadcastScope(const BroadcastScope&) = delete;
		BroadcastScope& operator=(const BroadcastScope&) = delete;

		Map& map_;
	};


public:
	Map(World* world);
	virtual ~Map();

	// 맵을 초기화한다. 맵별 네비메시 로드에 실패하면(경로 미지정/파일 없음 등)
	// false 를 반환한다 — 호출자는 해당 맵을 등록하지 말아야 한다.
	bool Init(const std::string& movementType, const gamedata::Map* mapData = nullptr);
	void update(float deltaTime);

	const gamedata::Map* GetMapData() const { return mapData_; }
	int GetMapId() const;

	// 프로파일링/벤치마크용: 이 맵의 navmesh. 맵의 실제 크기를 알거나(Bounds)
	// 그 위에서 좌표를 뽑을 때 쓴다. 로드 전이면 nullptr.
	const NavMesh* GetNavMesh() const { return navMesh_; }

	// 맵 데이터의 첫 번째 player_spawn 위치(클라 좌표계). 없으면 (0,0,0).
	syncnet::Vec3 GetPlayerSpawnPos() const;

	// 맵 데이터(Map.json)의 monster_spawn 마커마다 count 마리를 radius 안에 흩뿌려 스폰한다.
	// spawn_delay 가 걸린 마커는 여기서 세우지 않고 UpdateMonsterSpawns 가 시간이 찬 뒤 채운다.
	// 스폰에 성공한 수를 반환한다. 서버 기동 시 World::SpawnMapMonsters 에서 호출된다
	// (벤치마크/테스트는 World::Init 만 호출하므로 몬스터가 자동 스폰되지 않는다).
	int SpawnMonstersFromData();

	// npc.json 에서 이 맵에 속하고 hp 가 있는 NPC 를 액터로 스폰한다.
	// hp 가 없는 NPC(퀘스트를 주는 마을 사람 등)는 데이터로만 존재하며 여기 오지 않는다.
	// 서버 기동 시 SpawnMapMonsters 와 함께 1회 호출한다.
	int SpawnNpcsFromData();

	// 게이트의 target_id(전역 유일 마커 id)가 가리키는 도착 지점을 푼다.
	// 마커는 게이트일 수도 스폰 지점일 수도 있다 — 둘 다 parent 로 소속 맵을 알 수 있어서
	// 목적지 맵 id 를 따로 들고 다닐 필요가 없다.
	// 찾으면 outMap/outPos(클라 좌표계)를 채우고 true, 없으면 false.
	static bool ResolveGateTarget(int targetId, const gamedata::Map*& outMap, syncnet::Vec3& outPos);

	// Map.json(클라 씬 좌표)의 게이트/스폰 위치가 서버 navmesh 위에 있는지 검증한다.
	// 씬과 navmesh가 어긋나면 마커가 메시 밖에 놓이므로 정합 이상을 로드 시점에 잡아낸다.
	// 어긋난 마커 수를 반환한다(경고 로그만 남기고 로드는 막지 않는다).
	// static 버전은 맵 인스턴스 없이도(예: 단위 테스트) 데이터+navmesh 만으로 검증할 때 사용한다.
	static int ValidateMapDataOnNavMesh(const gamedata::Map* mapData, const NavMesh* navMesh);
	int ValidateMapDataOnNavMesh() const { return ValidateMapDataOnNavMesh(mapData_, navMesh_); }

	// 몬스터 AI 시스템(ECS 백엔드). Init 전이면 nullptr.
	monsterai::MonsterAISystem* GetAISystem() const { return aiSystem_; }

	// update(deltaTime) 를 구성하는 단계들. 단계별 프로파일링을 위해 분리해 노출한다.
	// update() 는 이들을 순서대로 호출할 뿐이라 직접 호출해도 동작은 동일하다.
	void UpdateActors(float deltaTime);
	void UpdateMovement(float deltaTime);
	void UpdateSystems(float deltaTime);

	// 진행 로직 단계: 보스 처치/플레이어 사망 판정과 부활 타이머를 처리한 뒤
	// 게임 모드 lua(on_update -> check_end)를 돌린다.
	void UpdateGameMode(float deltaTime);

	// ── 게임 모드 연동 ──
	// 이 맵의 진행 모드. 모드 데이터가 없는 맵(테스트 등)은 nullptr.
	GameMode* GetGameMode() const { return gameMode_.get(); }

	// 인스턴스 식별자. 0 이면 상시 맵이고, 1 이상이면 입장할 때 만들어진 인스턴스다.
	// 같은 mapId 로 여러 인스턴스가 동시에 존재할 수 있어 구분자가 필요하다.
	int  GetInstanceId() const { return instanceId_; }
	void SetInstanceId(int id) { instanceId_ = id; }
	bool IsInstance() const { return instanceId_ != 0; }

	size_t GetPlayerCount() const { return players_.size(); }

	// 순회 도중 맵의 플레이어 목록이 바뀔 수 있을 때 쓴다(ChangeMap 처럼 Enter/leave 를
	// 부르는 순회). 복사본이라 안전한 대신 호출마다 벡터 할당 + 인원수만큼의 참조 수
	// 증감이 붙는다 — 그럴 필요가 없으면 GetPlayerMap() 을 쓴다.
	std::vector<std::shared_ptr<Player>> GetPlayers() const;

	// 읽기 전용 순회용. 할당도 참조 수 증감도 없다.
	// 순회 중에 플레이어가 맵에 들어오거나 나가면 반복자가 깨지므로, 목록을 바꿀 수
	// 있는 일(맵 이동, 이벤트 발행 등)을 하는 순회는 GetPlayers() 를 써야 한다.
	const std::unordered_map<long, std::shared_ptr<Player>>& GetPlayerMap() const { return players_; }

	// boss_spawn 마커에 보스를 스폰한다. 액터 id 를 반환하며 실패 시 -1.
	// 스폰한 보스는 UpdateGameMode 가 사망을 감시한다.
	int SpawnBoss(int bossId);

	// GM_SpawnBoss 로 스폰된 보스의 액터 id. 아직 없으면 -1.
	int GetBossActorId() const { return bossActorId_; }

	// 캐릭터 체력이 남아 있는 플레이어 수. lua 의 전멸 판정에 쓰인다.
	int CountAlivePlayers();

	// 단계 4: 플레이어 사망 판정과 부활 타이머. 게임 모드와 무관하게 매 틱 돈다.
	void UpdatePlayerDeath(float deltaTime);

	// 단계 4: monster_spawn 마커의 정원(count)을 유지한다. 죽은 자리는 마커의
	// spawn_interval 초 뒤에 다시 채우고, spawn_delay 가 걸린 마커는 그만큼 늦게 연다.
	// 이번 틱에 새로 세운 마리 수를 반환한다.
	int UpdateMonsterSpawns(float deltaTime);

	// 스포너가 관리 중인 몬스터 수 / 마커가 요구하는 총 마리 수.
	int SpawnedMonsterCount() const { return monsterSpawner_.AliveCount(); }
	int DesiredMonsterCount() const { return monsterSpawner_.DesiredCount(); }

	// seconds 뒤에 플레이어를 스폰 지점에 되살린다(GM_SchedulePlayerRespawn).
	void SchedulePlayerRespawn(long playerId, float seconds);

	// 프로파일링용: 모든 액터에 대해 DetectEnemy 를 1회씩 호출하고 탐지 성공 수를 반환한다.
	// BehaviorTree tick 안에서 적 탐지(그리드 쿼리)가 차지하는 비용을 격리 측정하기 위한 훅.
	int ProfileDetectEnemyAll();

	// 프로파일링용: 모든 액터의 ActorInfo 를 1회씩 직렬화하고 만들어진 개수를 반환한다.
	// systems 단계(SyncActorState)가 액터마다 지불하는 flatbuffer 직렬화 비용을 격리 측정한다.
	int ProfileSerializeAll();

	World* world() { return world_; }
	INavMovement* GetNavMap() { return movement_; }

	void SendWorldState();
	void SendTreeDebugSync();
	void SendDebugRaycasts();
	void SendBroadcastState();
	void SendBroadcast(std::shared_ptr<send_message> msg);
	void SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except);

	// 관심영역이 켜져 있으면 source 가 있는 셀의 구독자에게만, 꺼져 있으면 전원에게 보낸다.
	// 특정 액터 주변에서 일어난 일(스킬 시전 등)을 알릴 때 쓴다.
	void SendToViewersOf(Actor* source, std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except);
	void OnRemoveAgent(int actor_id);
	std::shared_ptr<Actor> OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnSetMoveTarget(int actor_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);
	int DetectEnemy(Actor* actor);

	// 이미 잡은 대상이 아직 유효한지(살아있고, 시야 반경 안이고, 가려지지 않았는지) 확인한다.
	// 교전 중에는 전체 재탐색 대신 이 검사만 돌려 탐지 비용을 줄인다.
	bool IsTargetVisible(Actor* viewer, int targetActorId);

	// 관심영역을 켜는/끄는 인구. 어느 쪽이 싼지는 인구가 정하고, 교차점은 봇 부하 측정에서
	// 600~800명 사이였다(PERFORMANCE.md 17절). 경계에서 모드가 오가지 않도록 벌려 둔다.
	static constexpr size_t kAoIEnablePlayers = 700;
	static constexpr size_t kAoIDisablePlayers = 600;

	// 지금 이 맵이 관심영역으로 돌고 있는가(진단/테스트용).
	bool AoIEnabled() const { return aoiEnabled_; }

	// 관심영역 반경을 강제로 지정한다(0 이하면 맵 데이터 값). 테스트/벤치에서 효과를 관찰하기 위한 것으로,
	// 이미 구독 중인 플레이어에게는 다음 구독 갱신부터 적용된다.
	// 인구와 무관하게 켠다 — 소수의 플레이어로 관심영역 동작을 재현하는 통로다.
	void SetAoIRadius(float radius);

	// 진단/테스트용: 이 액터가 해당 플레이어의 관심영역(구독 셀) 안에 있는가.
	bool IsInViewOf(long playerId, int actorId);
	std::vector<IGridActor*> get_actors_in_range(Actor* actor, float range, float dirDeg, float angle);
	// 캐스터가 아닌 임의의 지점(서버 좌표계 x,z) 중심의 원형 범위. 시전 지점에 떨어지는
	// 광역기(메테오/블리자드 등, aoe_damage 효과)에 쓴다. get_actors_in_range 가 캐스터
	// 중심 부채꼴인 것과 대비된다.
	std::vector<IGridActor*> get_actors_in_radius(float centerX, float centerZ, float radius);
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

