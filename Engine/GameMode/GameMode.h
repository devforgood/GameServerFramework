#pragma once
#include <lua.hpp>
#include <mutex>
#include <string>
#include "GameDataPath.h"

namespace gamedata
{
	struct GameMode; // Forward declaration of gamedata::GameMode
}

class Map;

// 게임 모드 인스턴스.
// 데이터(gamedata)는 GameMode.json 에서 로드되고, 진행 로직은 gamedata->script 가
// 가리키는 lua 스크립트(모듈 테이블)에 위임한다.
//
// lua 모듈 계약(없는 함수는 호출하지 않음):
//   on_start(mode) / on_update(mode, dt) / on_player_join(mode, player)
//   on_player_dead(mode, player) / check_end(mode) -> bool / on_end(mode)
// 여기서 mode 는 이 GameMode* 를 가리키는 lightuserdata 이며, 스크립트는
// 전역으로 등록된 호스트 함수(GM_*)로 진행 상태를 조회/조작한다.
class GameMode
{
public:
	const gamedata::GameMode* gamedata = nullptr; // Pointer to gamedata for GameMode information

	GameMode() = default;
	virtual ~GameMode();

	// gamedata->script 가 가리키는 lua 스크립트를 로드해 모듈 테이블 ref 를 보관한다.
	bool LoadScript();

	void SetMap(Map* map) { map_ = map; }
	Map* GetMap() const { return map_; }

	// 라이프사이클: 대응하는 lua 함수가 있으면 호출한다.
	void Start();                    // on_start
	void Update(float dt);           // on_update -> check_end -> (종료 시) End
	void OnPlayerJoin(void* player); // on_player_join
	void OnPlayerDead(void* player); // on_player_dead
	void End();                      // on_end

	bool IsEnded() const { return ended_; }

	// ---- 진행 상태 (lua 가 호출하는 GM_* 호스트 함수에서 조회/조작) ----
	float remaining_time() const { return remainingTime_; }
	void  set_remaining_time(float t) { remainingTime_ = t; }
	bool  boss_dead() const { return bossDead_; }
	void  set_boss_dead(bool v) { bossDead_ = v; }
	int   alive_player_count() const { return alivePlayers_; }
	void  set_alive_player_count(int n) { alivePlayers_ = n; }
	bool  end_requested() const { return endRequested_; }
	void  request_end() { endRequested_ = true; }
	const std::string& result() const { return result_; }
	void  set_result(const std::string& r) { result_ = r; }

	// ---- 공유 lua 상태 ----
	//
	// lua_State 는 프로세스에 하나뿐이고 모든 맵의 게임 모드가 그것을 함께 쓴다.
	// 월드는 스레드마다 따로 돌지만(ServerConfig.world.thread_count) 이 VM 은 공유되므로,
	// lua 에 들어가는 모든 경로는 luaMutex_ 로 직렬화한다. 잠그지 않으면 두 스레드가 같은
	// lua 스택을 동시에 밀고 당겨 스택이 망가진다.
	//
	// 한 틱에 맵당 on_update/check_end 두 번뿐이라 이 직렬화가 병목이 되지는 않는다.
	// (몬스터 AI 는 lua 를 타지 않는다 — behaviortree_cpp 로 돈다.)
	static void InitializeLua(const std::string& script_base_path = GameDataPath::Resolve());
	static void CloseLua();
	static void RegisterHostFunctions();
	static lua_State* Lua() { return L_; }

private:
	// 모듈 테이블의 함수 fn 을 스택에 push 한다. 함수가 있으면 true 를 반환하며,
	// 이때 스택은 [..., module, fn] 상태가 된다(호출 후 module 을 pop 해야 함).
	// 함수가 없으면 스택을 원상복구하고 false 를 반환한다.
	bool PushHandler(const char* fn);

	// check_end lua 함수를 호출한다(없으면 endRequested_ 로 판단).
	bool CheckEnd();

	static lua_State* L_;
	static std::string scriptBasePath_;

	// L_ 접근 직렬화용. Update 가 End 를 부르는 등 잠금 구간이 겹치므로 recursive 를 쓴다.
	static std::recursive_mutex luaMutex_;

	int scriptRef_ = LUA_NOREF; // 스크립트가 반환한 모듈 테이블 ref
	Map* map_ = nullptr;

	bool  started_ = false;
	bool  ended_ = false;
	bool  endRequested_ = false;
	bool  bossDead_ = false;
	int   alivePlayers_ = 0;
	float remainingTime_ = 0.0f;
	std::string result_;
};
