#pragma once

#include <string>
#include "INavMovement.h"

class NavMesh;

// 게임 모드 데이터의 movement 타입에 따라 이동 전략을 생성한다.
class NavMovementFactory
{
public:
	// 문자열("crowd" | "waypoint")을 타입으로 변환한다. 알 수 없으면 Crowd.
	static NavMovementType ParseType(const std::string& type);

	// 이동 전략을 생성한다. nav 는 공유 네비메시(소유권 비이전).
	// 반환 객체는 호출자가 소유(delete)한다.
	static INavMovement* Create(NavMovementType type, NavMesh* nav);
	static INavMovement* Create(const std::string& type, NavMesh* nav);
};
