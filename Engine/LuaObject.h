#pragma once
#include <lua.hpp>
#include <string>
#include <iostream>

template <typename T>
class LuaObject
{
private:

    // Lua 스크립트 로딩 함수
    static void loadLuaScript(lua_State* L, const std::string& script) {
        if (luaL_loadfile(L, script.c_str()) != LUA_OK) {
            std::cerr << "Lua Error: " << lua_tostring(L, -1) << std::endl;
        }
        else {
            if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
                std::cerr << "Lua Error: " << lua_tostring(L, -1) << std::endl;
            }
        }
    }

public :
    static lua_State* L;

	static void Initialize(const std::string& script_file_name) {
        // Lua 상태 객체 생성
        L = luaL_newstate();
        luaL_openlibs(L);


        // Lua 스크립트 로딩
        loadLuaScript(L, script_file_name);

	}

	static void close() {
		// Lua 상태 객체 닫기
		lua_close(L);
	}



    // C++에서 Lua Behavior Tree 실행
    void runBehaviorTree(T* obj) {
        lua_getglobal(L, "runTree");
        lua_pushlightuserdata(L, obj);
        if (lua_pcall(L, 1, 1, 0) != LUA_OK) {
            std::cerr << "Error calling runTree: " << lua_tostring(L, -1) << std::endl;
        }
        else {
            std::cout << "Behavior Tree Result: " << lua_tostring(L, -1) << std::endl;
        }
        lua_pop(L, 1);
    }

    // Lua 트리 구조 프린트 함수
    void printBehaviorTree() {
        lua_getglobal(L, "printTree");
        if (lua_pcall(L, 0, 0, 0) != LUA_OK) {
            std::cerr << "Error calling printTree: " << lua_tostring(L, -1) << std::endl;
        }
    }

	static void registerLuaFunction(const std::string& name, lua_CFunction func) {
		lua_register(L, name.c_str(), func);
	}
};


// 정적 멤버 변수의 정의
template <typename T>
lua_State* LuaObject<T>::L = nullptr;

