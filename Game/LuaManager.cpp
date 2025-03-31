#include <iostream>
#include <lua.hpp>

#include "LuaManager.h"


// C++���� �����ϴ� �ൿ(Action) �Լ�
int lua_Attack(lua_State* L) {
    std::cout << "Executing Attack!" << std::endl;
    lua_pushstring(L, "SUCCESS"); // ��� ��ȯ
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

// Lua ��ũ��Ʈ ���� �Լ�
void runLuaScript(lua_State* L, const std::string& script) {
    if (luaL_dofile(L, script.c_str()) != LUA_OK) {
        std::cerr << "Lua Error: " << lua_tostring(L, -1) << std::endl;
    }
}

// C++���� Lua Behavior Tree ����
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
	// Lua ���� ��ü ����
	lua_State* L = luaL_newstate();
	luaL_openlibs(L);
	// C++ �Լ��� Lua �Լ��� ���
	lua_register(L, "Attack", lua_Attack);
	lua_register(L, "Defend", lua_Defend);
	lua_register(L, "Patrol", lua_Patrol);
	lua_register(L, "LookAround", lua_LookAround);

	// Lua ��ũ��Ʈ ����
	runLuaScript(L, "mob.lua");
	// Lua Behavior Tree ����

    for (int i = 0; i < 10; i++) {
        runBehaviorTree(L);
    }
	//runBehaviorTree(L);
	// Lua ���� ��ü �ݱ�
	lua_close(L);
}

