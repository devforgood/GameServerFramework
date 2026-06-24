#pragma once

#include "INavMovement.h"
#include "DetourNavMeshQuery.h"

class NavMesh;
class dtCrowd;

// dtCrowd 기반 이동 전략.
// dtCrowd 가 매 틱 군집 스티어링과 지역 장애물 회피를 내부에서 처리하므로,
// 본 클래스는 이동 목표만 요청하고 결과 위치를 조회한다.
class CrowdNavMovement : public INavMovement
{
	NavMesh* nav_;        // 공유 네비메시(소유권 없음).
	dtCrowd* crowd_;

	float randomRadius_;  // patrol 시 임의 지점 탐색 반경.

	// "타깃을 먼저 설정한 뒤 추가되는" 에이전트에 적용할 전역 목표.
	float targetPos_[3];
	dtPolyRef targetRef_;

	struct CrowdToolParams
	{
		bool anticipateTurns = true;
		bool optimizeVis = true;
		bool optimizeTopo = true;
		bool obstacleAvoidance = true;
		float obstacleAvoidanceType = 3.0f;
		bool separation = false;
		float separationWeight = 2.0f;
	} toolParams_;

	// 크라우드 동시 에이전트 정원. dtCrowd 가 이 수만큼 배열을 미리 할당하므로
	// 값이 클수록 메모리를 더 쓴다. 대규모 몬스터 벤치마크 수용을 위해 16384.
	static const int MAX_AGENTS = 16384;

public:
	explicit CrowdNavMovement(NavMesh* nav);
	~CrowdNavMovement() override;

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
