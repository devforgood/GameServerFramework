#include "WaypointNavMovement.h"
#include "NavMesh.h"
#include "DetourCommon.h"
#include <cstring>
#include <cstdlib>
#include "LogHelper.h"

static float frand()
{
	return (float)rand() / (float)RAND_MAX;
}

static const float kZeroPos[3] = { 0, 0, 0 };

WaypointNavMovement::WaypointNavMovement(NavMesh* nav)
	: nav_(nav), randomRadius_(0.0f)
{
}

bool WaypointNavMovement::Init()
{
	if (nav_ == nullptr || !nav_->IsLoaded())
	{
		LOG.error("WaypointNavMovement::Init navmesh not loaded");
		return false;
	}
	randomRadius_ = nav_->agentRadius() * 30.0f;
	return true;
}

bool WaypointNavMovement::computePath(Agent& a, const float* targetPos)
{
	dtNavMeshQuery* q = nav_->query();
	const dtQueryFilter* filter = nav_->filter();
	const float* ext = nav_->queryHalfExtents();

	dtPolyRef startRef = 0, endRef = 0;
	float startPt[3], endPt[3];
	q->findNearestPoly(a.pos, ext, filter, &startRef, startPt);
	q->findNearestPoly(targetPos, ext, filter, &endRef, endPt);
	if (!startRef || !endRef)
		return false;

	dtPolyRef polys[MAX_POLYS];
	int npolys = 0;
	q->findPath(startRef, endRef, startPt, endPt, filter, polys, &npolys, MAX_POLYS);
	if (npolys == 0)
		return false;

	// 부분 경로(목표 폴리곤에 도달 못함)면 마지막 폴리곤 위 가장 가까운 점으로 목표를 보정한다.
	float epos[3];
	dtVcopy(epos, endPt);
	if (polys[npolys - 1] != endRef)
		q->closestPointOnPoly(polys[npolys - 1], endPt, epos, nullptr);

	float straight[MAX_CORNERS * 3];
	unsigned char straightFlags[MAX_CORNERS];
	dtPolyRef straightRefs[MAX_CORNERS];
	int n = 0;
	q->findStraightPath(startPt, epos, polys, npolys,
		straight, straightFlags, straightRefs, &n, MAX_CORNERS, 0);
	if (n == 0)
		return false;

	a.corners.assign(straight, straight + n * 3);
	a.cornerCount = n;
	a.nextCorner = 0;
	a.paused = false;
	// 현재 위치를 경로 시작점(스냅된 위치)으로 맞춰 미세 어긋남을 제거한다.
	dtVcopy(a.pos, startPt);
	return true;
}

void WaypointNavMovement::Update(float dt)
{
	for (auto& a : agents_)
	{
		if (!a.active || a.paused)
			continue;
		if (a.nextCorner >= a.cornerCount || a.speed <= 0.0f)
		{
			a.vel[0] = a.vel[1] = a.vel[2] = 0.0f;
			continue;
		}

		float prev[3];
		dtVcopy(prev, a.pos);

		float step = a.speed * dt;
		while (a.nextCorner < a.cornerCount && step > 0.0f)
		{
			const float* target = &a.corners[a.nextCorner * 3];
			float dist = dtVdist(a.pos, target);
			if (dist <= step)
			{
				dtVcopy(a.pos, target); // 웨이포인트 도달.
				a.nextCorner++;
				step -= dist;
			}
			else
			{
				float dir[3];
				dtVsub(dir, target, a.pos);
				dtVscale(dir, dir, step / dist);
				dtVadd(a.pos, a.pos, dir);
				step = 0.0f;
			}
		}

		// 속도 추정(dt 기준 변위).
		if (dt > 0.0f)
		{
			dtVsub(a.vel, a.pos, prev);
			dtVscale(a.vel, a.vel, 1.0f / dt);
		}
	}
}

int WaypointNavMovement::AddAgent(const float* p, float speed)
{
	if (nav_ == nullptr || !nav_->IsLoaded())
		return -1;

	dtPolyRef ref = 0;
	float snap[3];
	nav_->query()->findNearestPoly(p, nav_->queryHalfExtents(), nav_->filter(), &ref, snap);
	if (!ref)
		return -1; // 네비메시 인근이 아님.

	int id;
	if (!freeList_.empty())
	{
		id = freeList_.back();
		freeList_.pop_back();
	}
	else
	{
		id = (int)agents_.size();
		agents_.emplace_back();
	}

	Agent& a = agents_[id];
	a = Agent();
	a.active = true;
	a.speed = speed;
	dtVcopy(a.pos, snap);

	LOG.info("add agent {} pos({}, {}, {})", id, -1 * a.pos[0], a.pos[1], a.pos[2]);
	return id;
}

void WaypointNavMovement::RemoveAgent(int id)
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return;
	agents_[id] = Agent(); // active=false 로 초기화.
	freeList_.push_back(id);
}

bool WaypointNavMovement::TeleportAgent(int id, const float* pos)
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return false;

	dtPolyRef ref = 0;
	float snap[3];
	nav_->query()->findNearestPoly(pos, nav_->queryHalfExtents(), nav_->filter(), &ref, snap);
	if (!ref)
		return false;

	Agent& a = agents_[id];
	dtVcopy(a.pos, snap);
	a.cornerCount = 0; // 진행 중 경로 폐기.
	a.nextCorner = 0;
	return true;
}

void WaypointNavMovement::SetMoveTarget(int id, const float* p, bool /*adjust*/)
{
	if (id != -1)
	{
		if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
			return;
		computePath(agents_[id], p);
	}
	else
	{
		for (auto& a : agents_)
		{
			if (a.active)
				computePath(a, p);
		}
	}
}

void WaypointNavMovement::Stop(int id)
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return;
	agents_[id].paused = true;
	agents_[id].vel[0] = agents_[id].vel[1] = agents_[id].vel[2] = 0.0f;
}

void WaypointNavMovement::Resume(int id)
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return;
	agents_[id].paused = false;
}

bool WaypointNavMovement::Patrol(int id, const float* originPos, float radius, float* outDest)
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return false;

	dtNavMeshQuery* q = nav_->query();
	const dtQueryFilter* filter = nav_->filter();

	dtPolyRef originRef = 0;
	float originSnap[3];
	q->findNearestPoly(originPos, nav_->queryHalfExtents(), filter, &originRef, originSnap);
	if (!originRef)
		return false;

	const float searchRadius = (radius > 0.0f) ? radius : randomRadius_;

	float epos[3];
	dtPolyRef endRef;
	dtStatus status = q->findRandomPointAroundCircle(originRef, originSnap, searchRadius, filter, frand, &endRef, epos);
	if (dtStatusSucceed(status))
	{
		if (outDest)
			dtVcopy(outDest, epos);
		return computePath(agents_[id], epos);
	}

	return false;
}

const float* WaypointNavMovement::GetPos(int id) const
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return kZeroPos;
	return agents_[id].pos;
}

bool WaypointNavMovement::IsActive(int id) const
{
	return id >= 0 && id < (int)agents_.size() && agents_[id].active;
}

bool WaypointNavMovement::Raycast(int id, const float* endPos, float* hitPoint) const
{
	if (id < 0 || id >= (int)agents_.size() || !agents_[id].active)
		return false;

	const Agent& a = agents_[id];
	dtNavMeshQuery* q = nav_->query();
	const dtQueryFilter* filter = nav_->filter();

	dtPolyRef startRef = 0;
	float snap[3];
	q->findNearestPoly(a.pos, nav_->queryHalfExtents(), filter, &startRef, snap);
	if (!startRef)
		return false;

	dtRaycastHit rayHit;
	rayHit.maxPath = 0;
	q->raycast(startRef, a.pos, endPos, filter, DT_RAYCAST_USE_COSTS, &rayHit);

	if (rayHit.t > 0.0f && rayHit.t < 1.0f)
	{
		dtVsub(hitPoint, endPos, a.pos);
		dtVscale(hitPoint, hitPoint, rayHit.t);
		dtVadd(hitPoint, a.pos, hitPoint);
		return true;
	}
	return false;
}
