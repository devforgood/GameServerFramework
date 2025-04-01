#pragma once
#include <lua.hpp>
#include <string>
#include <iostream>

template <typename T>
class LuaObject
{
private:

    // Lua ��ũ��Ʈ �ε� �Լ�
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
        // Lua ���� ��ü ����
        L = luaL_newstate();
        luaL_openlibs(L);


        // Lua ��ũ��Ʈ �ε�
        loadLuaScript(L, script_file_name);

	}

	static void close() {
		// Lua ���� ��ü �ݱ�
		lua_close(L);
	}



    // C++���� Lua Behavior Tree ����
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

    // Lua Ʈ�� ���� ����Ʈ �Լ�
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


// ���� ��� ������ ����
template <typename T>
lua_State* LuaObject<T>::L = nullptr;

