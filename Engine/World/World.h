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
namespace gamedata {
	struct Map;
}

class World
{
private:

	RandomUtil* randomUtil_;
	TimeStamp* timeStamp_;

	std::unordered_map<long, std::shared_ptr<Player>> players_;

	// 상시 맵(field). 서버 기동 시 전부 만들어 두고 모든 플레이어가 공유한다.
	std::list<std::shared_ptr<Map>> mapList_;
	// gamedata 맵 id -> 상시 Map. 게이트 이동 등 id 기반 라우팅에 사용.
	std::unordered_map<int, std::shared_ptr<Map>> mapById_;

	// 인스턴스 맵(raid 등 field 가 아닌 모드). 플레이어가 입장할 때 만들어지고,
	// 진행 로직이 종료를 선언하거나 마지막 플레이어가 나가면 파괴된다.
	// 같은 mapId 로 여러 개가 동시에 존재할 수 있어 id 로 색인하지 않는다.
	std::list<std::shared_ptr<Map>> instances_;
	int nextInstanceId_ = 1;

	// Init 에 넘어온 이동 전략 오버라이드. 나중에 만드는 인스턴스도 같은 값을 써야
	// 벤치마크/테스트가 의도한 전략으로 돈다. 비어 있으면 맵의 게임 모드 설정을 따른다.
	std::string movementOverride_;

	// 재접속 핸드오버 대기: 세션이 끊긴 플레이어를 즉시 제거하지 않고 유예 시간 동안
	// 캐릭터를 월드에 유지한다. 키는 플레이어 고유 uuid(재접속 토큰).
	// uuid -> (플레이어, 남은 유예 시간(초)).
	struct PendingReconnect
	{
		std::shared_ptr<Player> player;
		float remainingSec;
	};
	std::unordered_map<std::string, PendingReconnect> pendingReconnects_;

	// 로그아웃(재접속 유예 만료)한 플레이어의 마지막 위치. 같은 userId 로 다시 로그인하면
	// 기본 스폰 대신 이 위치로 스폰한다. 키는 userId, 위치는 클라 좌표계(Map.json 기준).
	// 서버 프로세스 메모리에만 유지된다(재시작 시 초기화. DB 영속화는 추후 과제).
	struct LastLocation
	{
		int mapId;
		syncnet::Vec3 pos;
	};
	std::unordered_map<std::string, LastLocation> lastLocations_;

	// 캐릭터가 월드에서 최종 제거되기 직전에 마지막 위치를 lastLocations_ 에 기록한다.
	void RememberLastLocation(const std::shared_ptr<Player>& player);

	// 진행이 끝났거나 빈 인스턴스를 정리한다. 매 틱 update 에서 호출.
	void CleanupInstances();

	// 종료된 인스턴스에 남은 플레이어를 출구 게이트 밖으로 내보낸다.
	void EvictInstancePlayers(Map* instance);

	// 인스턴스 맵의 출구(첫 게이트)를 찾는다. 없으면 false.
	static bool FindInstanceExit(const gamedata::Map* data, int& outMapId, int& outGateId);


public:
	World();
	virtual ~World();

	// movementOverride 가 비어있지 않으면 게임 모드 데이터의 이동 전략 대신
	// 해당 전략("crowd"/"waypoint")을 강제한다(벤치마크/테스트용).
	void Init(const std::string& movementOverride = "");

	// Map.json <-> GameMode.json 의 참조 정합성을 검사한다. Init 이 맵을 로드하기 전에
	// 호출한다. 반환값은 오류 수이고, outWarnings 가 있으면 경고 수를 채운다.
	//
	//   오류  = 런타임에 실제로 깨지는 참조. 없는 game_mode_id/맵/게이트를 가리키거나
	//           field 맵에 navmesh 가 없는 경우.
	//   경고  = 데이터는 성립하지만 의도와 다른 상태. GameMode.maps 불일치(코드가 읽지
	//           않는 역방향 사본이라 어긋나도 동작은 한다), 들어오는 게이트가 없어
	//           도달할 수 없는 맵.
	//
	// 여기서 서버를 멈추지는 않는다. 잘못된 맵은 어차피 로드 대상에서 빠지는데, 그게
	// 조용히 일어나는 것이 문제였다(모드 3/4 를 지운 뒤 그 모드를 가리키던 맵들이 아무
	// 경고 없이 사라져 있었다). 치명적인 것(field 맵 navmesh 누락)은 Map::Init 이 따로
	// 기동을 중단시킨다.
	static int ValidateGameData(int* outWarnings = nullptr);
	void update(float deltaTime);

	// 로드된 모든 맵의 monster_spawn 마커 위치에 몬스터를 스폰한다.
	// 서버 기동 시(Init 이후) 1회 호출한다. 벤치마크/테스트가 쓰는 Init 과 분리해
	// 측정/검증 환경에서는 몬스터가 자동 스폰되지 않게 한다.
	void SpawnMapMonsters();

	// userId 의 마지막 로그아웃 위치를 조회한다. 있으면 out 파라미터를 채우고 true.
	// 위치는 클라 좌표계(Login 응답에 그대로 실어 보낼 수 있다).
	bool GetLastLocation(const std::string& userId, int& outMapId, syncnet::Vec3& outPos) const;

	// 프로파일링/벤치마크용: 첫 번째(기본) 맵 접근자. 맵이 없으면 nullptr.
	Map* GetPrimaryMap() { return mapList_.empty() ? nullptr : mapList_.front().get(); }

	// 프로파일링/벤치마크용: 로드된 모든 맵. 맵마다 크기가 달라서
	// (예: 대규모 측정은 가장 넓은 맵에서 해야 한다) 고르려면 목록이 필요하다.
	std::vector<Map*> GetMaps() const;

	// gamedata 맵 id로 상시(field) Map 을 찾는다. 없으면 nullptr.
	// 인스턴스 맵은 여기서 찾을 수 없다 — 같은 mapId 로 여러 개가 동시에 살아 있어서
	// id 만으로는 어느 것인지 정할 수 없다. 인스턴스는 플레이어의 캐릭터가 가진
	// Map* 로만 접근한다.
	Map* FindMap(int mapId);

	// mapId 맵의 인스턴스를 새로 만든다(raid 등 field 가 아닌 모드 전용).
	// 몬스터 스폰과 게임 모드 시작까지 마친 Map* 을 반환하며, 실패 시 nullptr.
	Map* CreateInstance(int mapId);

	// 진단/테스트용: 현재 살아 있는 인스턴스 수.
	size_t GetInstanceCount() const { return instances_.size(); }

	// 플레이어의 캐릭터를 mapId 맵의 gateId 게이트 위치로 이동시킨다.
	// 성공 시 outPos 에 도착 위치, outAgentId 에 (재생성된) 새 actor id 를 채우고 true 를 반환한다.
	bool ChangeMap(std::shared_ptr<Player> player, int mapId, int gateId, syncnet::Vec3& outPos, int& outAgentId);

	RandomUtil* random_util() { return randomUtil_; }

	void SendWorldState();
	void GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector);

	std::shared_ptr<Actor> OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos);
	void OnRemoveAgent(int agent_id);
	void OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos);
	void OnSetRaycast(const syncnet::Vec3* pos);


	void join(std::shared_ptr<Player> player);
	void leave(std::shared_ptr<Player> player);

	// 세션 끊김 처리. 캐릭터가 맵에 있으면 즉시 제거하지 않고 유예 시간 동안 캐릭터를
	// 유지(핸드오버 대기, 키=플레이어 uuid)한다. 그 외에는 즉시 정리한다.
	void BeginDisconnect(std::shared_ptr<Player> player);

	// 유예 시간 경과한 대기 플레이어를 정리한다. World::update 에서 매 틱 호출.
	void TickReconnectGrace(float deltaTime);

	// uuid(재접속 토큰)로 유예 대기 중인 플레이어를 찾아 반환하고 대기 목록에서 제거한다
	// (재바인딩은 호출 측에서 수행). 없으면 nullptr. 반환 시 월드/맵 브로드캐스트 목록에
	// 다시 등록한다.
	std::shared_ptr<Player> TryReconnect(const std::string& uuid);

	friend class Actor;
	friend class ActorFactory;
	friend class Monster;
	friend class Character;

};

