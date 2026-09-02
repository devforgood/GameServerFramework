#include "GameMode.h"

#include <iostream>
#include <lua.hpp>
#include <mutex>

#include "Common.h" // gamedata::GameMode 전체 정의
#include "LogHelper.h"
#include "Map.h"
#include "Player.h"
#include "Character.h"
#include "PlayerLevel.h"

namespace
{
	// 인스턴스 클리어 기본 보상 경험치. 모드 데이터의 exp_multiplier 를 곱해 지급한다.
	// (몬스터 처치 경험치 PlayerLevel::kExpPerKill 이 50 인 것과 맞춘 값이다.)
	constexpr int kClearBaseExp = 500;
}

lua_State* GameMode::L_ = nullptr;
std::string GameMode::scriptBasePath_ = GameDataPath::Resolve();
std::recursive_mutex GameMode::luaMutex_;

// L_ 를 건드리는 구간을 감싸는 이름 하나. 아래 lua 진입점마다 첫 줄에 둔다.
#define LUA_GUARD() const std::lock_guard<std::recursive_mutex> luaLock(luaMutex_)

//---------------------------------------------------------------------------------------
// 라이프사이클
//---------------------------------------------------------------------------------------

GameMode::~GameMode()
{
	LUA_GUARD();
	if (L_ != nullptr && scriptRef_ != LUA_NOREF)
	{
		luaL_unref(L_, LUA_REGISTRYINDEX, scriptRef_);
		scriptRef_ = LUA_NOREF;
	}
}

bool GameMode::LoadScript()
{
	LUA_GUARD();
	if (L_ == nullptr)
	{
		std::cerr << "[GameMode] LoadScript called before InitializeLua" << std::endl;
		return false;
	}
	if (gamedata == nullptr || gamedata->script.empty())
	{
		// 스크립트가 없는 모드는 진행 로직 없이 동작한다.
		return false;
	}

	const std::string path = scriptBasePath_ + gamedata->script;

	if (luaL_loadfile(L_, path.c_str()) != LUA_OK)
	{
		std::cerr << "[GameMode] load '" << path << "' failed: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1);
		return false;
	}
	if (lua_pcall(L_, 0, 1, 0) != LUA_OK) // 모듈 테이블을 반환받는다.
	{
		std::cerr << "[GameMode] exec '" << path << "' failed: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1);
		return false;
	}
	if (!lua_istable(L_, -1))
	{
		std::cerr << "[GameMode] script '" << path << "' did not return a table" << std::endl;
		lua_pop(L_, 1);
		return false;
	}

	if (scriptRef_ != LUA_NOREF)
		luaL_unref(L_, LUA_REGISTRYINDEX, scriptRef_);
	scriptRef_ = luaL_ref(L_, LUA_REGISTRYINDEX); // 테이블을 pop 하며 ref 보관
	return true;
}

bool GameMode::PushHandler(const char* fn)
{
	if (L_ == nullptr || scriptRef_ == LUA_NOREF)
		return false;

	lua_rawgeti(L_, LUA_REGISTRYINDEX, scriptRef_); // [module]
	if (!lua_istable(L_, -1))
	{
		lua_pop(L_, 1);
		return false;
	}
	lua_getfield(L_, -1, fn); // [module, fn]
	if (!lua_isfunction(L_, -1))
	{
		lua_pop(L_, 2);
		return false;
	}
	return true; // 스택: [module, fn]
}

void GameMode::Start()
{
	LUA_GUARD();
	if (started_)
		return;
	started_ = true;

	if (!PushHandler("on_start"))
		return;

	lua_pushlightuserdata(L_, this); // mode
	if (lua_pcall(L_, 1, 0, 0) != LUA_OK)
	{
		std::cerr << "[GameMode] on_start error: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1); // error
	}
	lua_pop(L_, 1); // module
}

void GameMode::Update(float dt)
{
	LUA_GUARD();
	if (ended_ || !started_)
		return;

	// 제한 시간 카운트다운(타이머가 켜진 모드만).
	if (remainingTime_ > 0.0f)
	{
		remainingTime_ -= dt;
		if (remainingTime_ < 0.0f)
			remainingTime_ = 0.0f;
	}

	if (PushHandler("on_update"))
	{
		lua_pushlightuserdata(L_, this); // mode
		lua_pushnumber(L_, dt);
		if (lua_pcall(L_, 2, 0, 0) != LUA_OK)
		{
			std::cerr << "[GameMode] on_update error: " << lua_tostring(L_, -1) << std::endl;
			lua_pop(L_, 1); // error
		}
		lua_pop(L_, 1); // module
	}

	if (CheckEnd())
		End();
}

bool GameMode::CheckEnd()
{
	if (!PushHandler("check_end"))
		return endRequested_; // check_end 가 없으면 RequestEnd 여부로 판단

	lua_pushlightuserdata(L_, this); // mode
	bool result = false;
	if (lua_pcall(L_, 1, 1, 0) != LUA_OK)
	{
		std::cerr << "[GameMode] check_end error: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1); // error
	}
	else
	{
		result = lua_toboolean(L_, -1) != 0;
		lua_pop(L_, 1); // 반환값
	}
	lua_pop(L_, 1); // module
	return result;
}

void GameMode::OnPlayerJoin(void* player)
{
	LUA_GUARD();
	if (!PushHandler("on_player_join"))
		return;

	lua_pushlightuserdata(L_, this); // mode
	if (player) lua_pushlightuserdata(L_, player); else lua_pushnil(L_);
	if (lua_pcall(L_, 2, 0, 0) != LUA_OK)
	{
		std::cerr << "[GameMode] on_player_join error: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1); // error
	}
	lua_pop(L_, 1); // module
}

void GameMode::OnPlayerDead(void* player)
{
	LUA_GUARD();
	if (!PushHandler("on_player_dead"))
		return;

	lua_pushlightuserdata(L_, this); // mode
	if (player) lua_pushlightuserdata(L_, player); else lua_pushnil(L_);
	if (lua_pcall(L_, 2, 0, 0) != LUA_OK)
	{
		std::cerr << "[GameMode] on_player_dead error: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1); // error
	}
	lua_pop(L_, 1); // module
}

void GameMode::End()
{
	LUA_GUARD();
	if (ended_)
		return;
	ended_ = true;

	if (!PushHandler("on_end"))
		return;

	lua_pushlightuserdata(L_, this); // mode
	if (lua_pcall(L_, 1, 0, 0) != LUA_OK)
	{
		std::cerr << "[GameMode] on_end error: " << lua_tostring(L_, -1) << std::endl;
		lua_pop(L_, 1); // error
	}
	lua_pop(L_, 1); // module
}

//---------------------------------------------------------------------------------------
// 공유 lua 상태
//---------------------------------------------------------------------------------------

void GameMode::InitializeLua(const std::string& script_base_path)
{
	LUA_GUARD();
	scriptBasePath_ = script_base_path;
	if (L_ == nullptr)
	{
		L_ = luaL_newstate();
		luaL_openlibs(L_);
		RegisterHostFunctions();
	}
}

void GameMode::CloseLua()
{
	LUA_GUARD();
	if (L_ != nullptr)
	{
		lua_close(L_);
		L_ = nullptr;
	}
}

//---------------------------------------------------------------------------------------
// 호스트 함수(GM_*): lua 스크립트가 진행 상태를 조회/조작하기 위해 호출한다.
// 모든 함수는 첫 번째 인자로 mode(GameMode* lightuserdata)를 받는다.
//---------------------------------------------------------------------------------------

namespace
{
	GameMode* ArgMode(lua_State* L)
	{
		return static_cast<GameMode*>(lua_touserdata(L, 1));
	}

	// --- gamedata 조회 ---
	int GM_GetTimeLimit(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushinteger(L, (m && m->gamedata) ? m->gamedata->rules.time_limit : 0);
		return 1;
	}
	int GM_GetRespawnTime(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushinteger(L, (m && m->gamedata) ? m->gamedata->rules.respawn_time : 0);
		return 1;
	}
	int GM_GetBossId(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushinteger(L, (m && m->gamedata) ? m->gamedata->boss_info.boss_id : 0);
		return 1;
	}

	// --- 타이머 ---
	int GM_StartTimer(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		if (m) m->set_remaining_time(static_cast<float>(lua_tonumber(L, 2)));
		return 0;
	}
	int GM_GetRemainingTime(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushnumber(L, m ? m->remaining_time() : 0.0);
		return 1;
	}

	// --- 종료/결과 ---
	int GM_SetResult(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		const char* r = lua_tostring(L, 2);
		if (m && r) m->set_result(r);
		return 0;
	}
	int GM_GetResult(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushstring(L, m ? m->result().c_str() : "");
		return 1;
	}
	int GM_RequestEnd(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		if (m) m->request_end();
		return 0;
	}
	int GM_IsEndRequested(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushboolean(L, (m && m->end_requested()) ? 1 : 0);
		return 1;
	}

	// --- 보스/플레이어 ---
	int GM_SpawnBoss(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		const int bossId = static_cast<int>(lua_tointeger(L, 2));
		if (m == nullptr || m->GetMap() == nullptr)
		{
			// 맵 없이 도는 환경(단위 테스트 등)에서는 스폰할 대상이 없다.
			LOG.debug("[GameMode] SpawnBoss id={} 무시(맵 없음)", bossId);
			return 0;
		}

		m->GetMap()->SpawnBoss(bossId);
		return 0;
	}
	int GM_IsBossDead(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushboolean(L, (m && m->boss_dead()) ? 1 : 0);
		return 1;
	}
	int GM_GetAlivePlayerCount(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		lua_pushinteger(L, m ? m->alive_player_count() : 0);
		return 1;
	}
	int GM_SchedulePlayerRespawn(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		Player* player = static_cast<Player*>(lua_touserdata(L, 2));
		const double secs = lua_tonumber(L, 3);
		if (m == nullptr || m->GetMap() == nullptr || player == nullptr)
		{
			LOG.debug("[GameMode] SchedulePlayerRespawn 무시(맵/플레이어 없음)");
			return 0;
		}

		m->GetMap()->SchedulePlayerRespawn(player->GetPlayerId(), static_cast<float>(secs));
		return 0;
	}
	int GM_GrantRewards(lua_State* L)
	{
		GameMode* m = ArgMode(L);
		if (m == nullptr || m->gamedata == nullptr || m->GetMap() == nullptr)
		{
			LOG.debug("[GameMode] GrantRewards 무시(맵/데이터 없음)");
			return 0;
		}

		// 클리어 보상. 아직 지급 대상은 경험치뿐이라 exp_multiplier 만 쓴다.
		// (gold/drop 은 인벤토리·드랍 테이블이 붙은 뒤에 이어서 처리한다.)
		const double expMul = m->gamedata->rewards.exp_multiplier;
		const int exp = static_cast<int>(kClearBaseExp * (expMul > 0.0 ? expMul : 1.0));

		int granted = 0;
		for (const auto& player : m->GetMap()->GetPlayers())
		{
			if (player == nullptr)
				continue;
			auto character = player->GetCharacter();
			if (character == nullptr)
				continue;

			// 죽은 채로 남아 있는 플레이어에게는 주지 않는다.
			if (character->GetHealth() <= 0)
				continue;

			if (auto* level = player->GetComponent<PlayerLevel>())
			{
				level->GainExp(exp);
				++granted;
			}
		}

		LOG.info("[GameMode] 클리어 보상 지급: 경험치 {} x {}명 (배수 {})", exp, granted, expMul);
		return 0;
	}
} // namespace

void GameMode::RegisterHostFunctions()
{
	if (L_ == nullptr)
		return;

	const struct
	{
		const char* name;
		lua_CFunction fn;
	} funcs[] = {
		{ "GM_GetTimeLimit",          GM_GetTimeLimit },
		{ "GM_GetRespawnTime",        GM_GetRespawnTime },
		{ "GM_GetBossId",             GM_GetBossId },
		{ "GM_StartTimer",            GM_StartTimer },
		{ "GM_GetRemainingTime",      GM_GetRemainingTime },
		{ "GM_SetResult",             GM_SetResult },
		{ "GM_GetResult",             GM_GetResult },
		{ "GM_RequestEnd",            GM_RequestEnd },
		{ "GM_IsEndRequested",        GM_IsEndRequested },
		{ "GM_SpawnBoss",             GM_SpawnBoss },
		{ "GM_IsBossDead",            GM_IsBossDead },
		{ "GM_GetAlivePlayerCount",   GM_GetAlivePlayerCount },
		{ "GM_SchedulePlayerRespawn", GM_SchedulePlayerRespawn },
		{ "GM_GrantRewards",          GM_GrantRewards },
	};

	for (const auto& f : funcs)
		lua_register(L_, f.name, f.fn);
}
