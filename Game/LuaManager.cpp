#include <iostream>
#include <lua.hpp>

#include "LuaManager.h"


// C++에서 제공하는 행동(Action) 함수
int lua_Attack(lua_State* L) {
    std::cout << "Executing Attack!" << std::endl;
    lua_pushstring(L, "SUCCESS"); // 결과 반환
    return 1;
}

int lua_Defend(lua_State* L) {
    std::cout << "Executing Defend!" << std::endl;
    lua_pushstring(L, "FAILURE");
    return 1;
}

int lua_Patrol(lua_State* L) {
    std::cout << "Executing Patrol!" << std::endl;
    lua_pushstring(L, "SUCCESS");
    return 1;
}

int lua_LookAround(lua_State* L) {
    std::cout << "Executing LookAround!" << std::endl;
    lua_pushstring(L, "SUCCESS");
    return 1;
}

// Lua 스크립트 실행 함수
void runLuaScript(lua_State* L, const std::string& script) {
    if (luaL_dofile(L, script.c_str()) != LUA_OK) {
        std::cerr << "Lua Error: " << lua_tostring(L, -1) << std::endl;
    }
}

// C++에서 Lua Behavior Tree 실행
void runBehaviorTree(lua_State* L) {
    lua_getglobal(L, "runTree");
    if (lua_pcall(L, 0, 1, 0) != LUA_OK) {
        std::cerr << "Error calling runTree: " << lua_tostring(L, -1) << std::endl;
    }
    else {
        std::cout << "Behavior Tree Result: " << lua_tostring(L, -1) << std::endl;
    }
    lua_pop(L, 1);
}


void LuaManager::Initialize() {
	// Lua 상태 객체 생성
	lua_State* L = luaL_newstate();
	luaL_openlibs(L);
	// C++ 함수를 Lua 함수로 등록
	lua_register(L, "Attack", lua_Attack);
	lua_register(L, "Defend", lua_Defend);
	lua_register(L, "Patrol", lua_Patrol);
	lua_register(L, "LookAround", lua_LookAround);

	// Lua 스크립트 실행
	runLuaScript(L, "mob.lua");
	// Lua Behavior Tree 실행

    for (int i = 0; i < 10; i++) {
        runBehaviorTree(L);
    }
	//runBehaviorTree(L);
	// Lua 상태 객체 닫기
	lua_close(L);
}

