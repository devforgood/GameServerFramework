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
	};

	NavMesh* nav_;                    // 공유 네비메시(소유권 없음).
	std::vector<Agent> agents_;
	std::vector<int> freeList_;       // 재사용 가능한 슬롯 인덱스.

	float randomRadius_;              // patrol 임의 지점 탐색 반경.

	bool computePath(Agent& a, const float* targetPos);

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
	bool Patrol(int id, const float* originPos) override;

	const float* GetPos(int id) const override;
	bool IsActive(int id) const override;
	bool Raycast(int id, const float* endPos, float* hitPoint) const override;
};
