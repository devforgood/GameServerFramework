#pragma once

// 길찾기/이동 추상 인터페이스.
//
// 게임 모드에 따라 두 가지 구현 중 하나가 주입된다:
//   - CrowdNavMovement   : dtCrowd 기반. 군집 스티어링/지역 장애물 회피를 내부에서 수행.
//   - WaypointNavMovement: recastnavigation 으로 경로를 산출한 뒤 매 틱 웨이포인트로 직접 이동.
//
// 에이전트는 정수 id 로 식별하고, 위치는 float[3](x, y, z) 로 주고받는다.
class INavMovement
{
public:
	virtual ~INavMovement() = default;

	// 네비메시는 이미 로드된 상태로 주입된다. 구현별 내부 상태를 초기화한다.
	virtual bool Init() = 0;
	virtual void Update(float dt) = 0;

	// 에이전트 관리. AddAgent 는 네비메시 인근으로 스냅하며 실패 시 -1 을 반환한다.
	virtual int  AddAgent(const float* pos, float speed) = 0;
	virtual void RemoveAgent(int id) = 0;
	virtual bool TeleportAgent(int id, const float* pos) = 0;

	// 이동 명령.
	virtual void SetMoveTarget(int id, const float* pos, bool adjustVelocity) = 0;
	virtual void Stop(int id) = 0;   // 정지(공격 등). 기준 속도는 유지한다.
	virtual void Resume(int id) = 0; // 기준 속도로 이동을 재개한다.
	// origin 주변 임의 지점으로 이동.
	// radius<=0 이면 구현 기본 반경을 사용한다. outDest 가 주어지면 선택된 목적지를 채운다.
	virtual bool Patrol(int id, const float* originPos, float radius = 0.0f, float* outDest = nullptr) = 0;

	// 상태 조회.
	virtual const float* GetPos(int id) const = 0;
	virtual bool IsActive(int id) const = 0;

	// 에이전트 현재 위치에서 endPos 로의 직선 가시성 검사.
	// 가려지면 hitPoint 에 충돌 지점을 채우고 true 를 반환한다.
	virtual bool Raycast(int id, const float* endPos, float* hitPoint) const = 0;
};

// 이동 전략 종류. GameMode 데이터(movement 필드)로 선택한다.
enum class NavMovementType
{
	Crowd,    // dtCrowd 기반(장애물 회피/군집).
	Waypoint, // recast 경로 + 매 틱 직접 이동.
};
