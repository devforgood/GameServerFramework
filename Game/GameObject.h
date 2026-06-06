#pragma once
#include <memory>
#include "syncnet_generated.h"



class GameObject {


public:
	GameObject() 
	{
	}
	virtual ~GameObject() = default; // 반드시 virtual 소멸자를 추가

	virtual void update(float dt) {};

};
