#include <iostream>
#include <lua.hpp>

#include "LuaObject.h"
#include "Monster.h"

// LuaObject<Monster>�� ���� ��� ���� ����
template <>
lua_State* LuaObject<Monster>::L = nullptr;



