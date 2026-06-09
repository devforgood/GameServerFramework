#include <iostream>
#include <lua.hpp>

#include "LuaObject.h"
#include "Monster.h"

// LuaObject<Monster>의 정적 멤버 변수 정의
template <>
lua_State* LuaObject<Monster>::L = nullptr;



