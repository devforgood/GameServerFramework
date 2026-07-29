#pragma once

#include "INavMovement.h"
#include "DetourNavMeshQuery.h"
#include <vector>

class NavMesh;

// recastnavigation 기반 이동 전략.
// SetMoveTarget 시 dtNavMeshQuery 로 경로(직선 웨이포인트 목록)를 한 번 산출하고,
// 매 틱(Update) 다음 웨이포인트로 speed*dt 만큼 직접 전진한다.
// 군집 스티어링/장애물 회피가 없어 가볍고 결정적이다.
class WaypointNavMovement : public INavMovement
{
	static const int MAX_POLYS = 256;
	static const int MAX_CORNERS = 256;

	struct Agent
	{
		bool active = false;
		bool paused = false;          // Stop/Resume 상태.
		float pos[3] = { 0, 0, 0 };
		float vel[3] = { 0, 0, 0 };
		float speed = 0.0f;           // 기준 속도(Resume 기준).

		std::vector<float> corners;   // 직선 경로 점들(평탄화된 x,y,z).
		int cornerCount = 0;
		int nextCorner = 0;           // 다음에 향할 웨이포인트 인덱스.

		float pathTarget[3] = { 0, 0, 0 }; // 마지막으로 경로를 산출한 목표 지점.
		bool hasPathTarget = false;
	};

	// 목표가 이 거리 안에서만 움직였고 기존 경로가 남아 있으면 경로를 다시 내지 않는다.
	// 추격(ActionChase)은 매 틱 SetMoveTarget 을 부르는데, 경로 산출은 한 번에 약 4us 로
	// 틱 안의 다른 작업보다 10~100배 비싸다(Benchmark/PERFORMANCE.md).
	// 몬스터 공격 사거리(3)보다 작게 잡아, 경로가 조금 낡아도 사거리 판정에는 영향이 없게 한다.
	static constexpr float kRepathDistance = 1.5f;

	NavMesh* nav_;                    // 공유 네비메시(소유권 없음).
	std::vector<Agent> agents_;
	std::vector<int> freeList_;       // 재사용 가능한 슬롯 인덱스.

	float randomRadius_;              // patrol 임의 지점 탐색 반경.

	bool computePath(Agent& a, const float* targetPos);
	bool CanReusePath(const Agent& a, const float* target) const;

public:
	explicit WaypointNavMovement(NavMesh* nav);
	~WaypointNavMovement() override = default;

	bool Init() override;
	void Update(float dt) override;

	int  AddAgent(const float* pos, float speed) override;
	void RemoveAgent(int id) override;
	bool TeleportAgent(int id, const float* pos) override;

	void SetMoveTarget(int id, const float* pos, bool adjustVelocity) override;
	void Stop(int id) override;
	void Resume(int id) override;
	bool Patrol(int id, const float* originPos, float radius = 0.0f, float* outDest = nullptr) override;

	const float* GetPos(int id) const override;
	bool IsActive(int id) const override;
	bool Raycast(int id, const float* endPos, float* hitPoint) const override;
};
