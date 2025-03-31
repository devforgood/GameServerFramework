#include <iostream>
#include <lua.hpp>

#include "LuaManager.h"
#include "Monster.h"


// C++���� �����ϴ� �ൿ(Action) �Լ�
int lua_Attack(lua_State* L) {
    Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
    std::cout << "Executing Attack!" << std::endl;
    lua_pushstring(L, "SUCCESS"); // ��� ��ȯ
    return 1;
}

int lua_Defend(lua_State* L) {
    Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
    std::cout << "Executing Defend!" << std::endl;
    lua_pushstring(L, "FAILURE");
    return 1;
}

int lua_Patrol(lua_State* L) {
    Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
    std::cout << "Executing Patrol!" << std::endl;
    lua_pushstring(L, "SUCCESS");
    return 1;
}

int lua_LookAround(lua_State* L) {
    Monster* monster = static_cast<Monster*>(lua_touserdata(L, 1));
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
void runBehaviorTree(lua_State* L, Monster* monster) {
    lua_getglobal(L, "runTree");
    lua_pushlightuserdata(L, monster);
    if (lua_pcall(L, 1, 1, 0) != LUA_OK) {
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
	Monster* monster = new Monster(0, nullptr);
	monster->name_ = "TEST01";

    for (int i = 0; i < 10; i++) {
        runBehaviorTree(L, monster);
    }

    Monster* monster2 = new Monster(0, nullptr);
    monster2->name_ = "TEST02";

    for (int i = 0; i < 10; i++) {
        runBehaviorTree(L, monster2);
    }

	//runBehaviorTree(L);
    // 
	// Lua ���� ��ü �ݱ�
	lua_close(L);
}

