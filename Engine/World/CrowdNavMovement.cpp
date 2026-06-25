#include "CrowdNavMovement.h"
#include "NavMesh.h"
#include "DetourCrowd.h"
#include "DetourCommon.h"
#include <cstring>
#include <cstdlib>
#include "LogHelper.h"

static float frand()
{
	return (float)rand() / (float)RAND_MAX;
}

CrowdNavMovement::CrowdNavMovement(NavMesh* nav)
	: nav_(nav), crowd_(nullptr), randomRadius_(0.0f), targetRef_(0)
{
	targetPos_[0] = targetPos_[1] = targetPos_[2] = 0.0f;
}

CrowdNavMovement::~CrowdNavMovement()
{
	if (crowd_)
	{
		dtFreeCrowd(crowd_);
		crowd_ = nullptr;
	}
}

bool CrowdNavMovement::Init()
{
	if (nav_ == nullptr || !nav_->IsLoaded())
	{
		LOG.error("CrowdNavMovement::Init navmesh not loaded");
		return false;
	}

	crowd_ = dtAllocCrowd();
	crowd_->init(MAX_AGENTS, nav_->agentRadius(), nav_->mesh());

	// Make polygons with 'disabled' flag invalid.
	crowd_->getEditableFilter(0)->setExcludeFlags(SAMPLE_POLYFLAGS_DISABLED);

	// Setup local avoidance params to different qualities.
	dtObstacleAvoidanceParams params;
	memcpy(&params, crowd_->getObstacleAvoidanceParams(0), sizeof(dtObstacleAvoidanceParams));

	// Low (11)
	params.velBias = 0.5f;
	params.adaptiveDivs = 5;
	params.adaptiveRings = 2;
	params.adaptiveDepth = 1;
	crowd_->setObstacleAvoidanceParams(0, &params);

	// Medium (22)
	params.velBias = 0.5f;
	params.adaptiveDivs = 5;
	params.adaptiveRings = 2;
	params.adaptiveDepth = 2;
	crowd_->setObstacleAvoidanceParams(1, &params);

	// Good (45)
	params.velBias = 0.5f;
	params.adaptiveDivs = 7;
	params.adaptiveRings = 2;
	params.adaptiveDepth = 3;
	crowd_->setObstacleAvoidanceParams(2, &params);

	// High (66)
	params.velBias = 0.5f;
	params.adaptiveDivs = 7;
	params.adaptiveRings = 3;
	params.adaptiveDepth = 3;
	crowd_->setObstacleAvoidanceParams(3, &params);

	randomRadius_ = nav_->agentRadius() * 30.0f;
	return true;
}

void CrowdNavMovement::Update(float dt)
{
	if (!crowd_) return;
	crowd_->update(dt, nullptr);
}

int CrowdNavMovement::AddAgent(const float* p, float speed)
{
	// 네비메시 로드 실패 시 크라우드 쿼리가 null 을 역참조하므로 미리 차단한다.
	if (nav_ == nullptr || !nav_->IsLoaded() || crowd_ == nullptr)
		return -1;

	dtCrowdAgentParams ap;
	memset(&ap, 0, sizeof(ap));
	ap.radius = nav_->agentRadius();
	ap.height = nav_->agentHeight();
	ap.maxAcceleration = 8.0f;
	ap.maxSpeed = speed;
	ap.collisionQueryRange = ap.radius * 12.0f;
	ap.pathOptimizationRange = ap.radius * 30.0f;
	ap.updateFlags = 0;
	if (toolParams_.anticipateTurns)
		ap.updateFlags |= DT_CROWD_ANTICIPATE_TURNS;
	if (toolParams_.optimizeVis)
		ap.updateFlags |= DT_CROWD_OPTIMIZE_VIS;
	if (toolParams_.optimizeTopo)
		ap.updateFlags |= DT_CROWD_OPTIMIZE_TOPO;
	if (toolParams_.obstacleAvoidance)
		ap.updateFlags |= DT_CROWD_OBSTACLE_AVOIDANCE;
	if (toolParams_.separation)
		ap.updateFlags |= DT_CROWD_SEPARATION;
	ap.obstacleAvoidanceType = (unsigned char)toolParams_.obstacleAvoidanceType;
	ap.separationWeight = toolParams_.separationWeight;

	int idx = crowd_->addAgent(p, &ap);
	if (idx == -1)
	{
		// 네비메시 인근이 아니면 크라우드가 -1 을 반환한다.
		return -1;
	}

	if (targetRef_)
		crowd_->requestMoveTarget(idx, targetRef_, targetPos_);

	auto agent = crowd_->getAgent(idx);
	LOG.info("add agent {} pos({}, {}, {})", idx, -1 * agent->npos[0], agent->npos[1], agent->npos[2]);
	return idx;
}

void CrowdNavMovement::RemoveAgent(int id)
{
	if (crowd_)
		crowd_->removeAgent(id);
}

bool CrowdNavMovement::TeleportAgent(int id, const float* pos)
{
	if (!crowd_) return false;
	crowd_->teleportAgent(id, pos);
	return true;
}

static void calcVel(float* vel, const float* pos, const float* tgt, const float speed)
{
	dtVsub(vel, tgt, pos);
	vel[1] = 0.0;
	dtVnormalize(vel);
	dtVscale(vel, vel, speed);
}

void CrowdNavMovement::SetMoveTarget(int id, const float* p, bool adjust)
{
	dtNavMeshQuery* navquery = nav_->query();
	const dtQueryFilter* filter = crowd_->getFilter(0);
	const float* halfExtents = crowd_->getQueryExtents();

	if (adjust)
	{
		float vel[3];
		if (id != -1)
		{
			const dtCrowdAgent* ag = crowd_->getAgent(id);
			if (ag && ag->active)
			{
				calcVel(vel, ag->npos, p, ag->params.maxSpeed);
				crowd_->requestMoveVelocity(id, vel);
			}
		}
		else
		{
			for (int i = 0; i < crowd_->getAgentCount(); ++i)
			{
				const dtCrowdAgent* ag = crowd_->getAgent(i);
				if (!ag->active) continue;
				calcVel(vel, ag->npos, p, ag->params.maxSpeed);
				crowd_->requestMoveVelocity(i, vel);
			}
		}
	}
	else
	{
		navquery->findNearestPoly(p, halfExtents, filter, &targetRef_, targetPos_);

		if (id != -1)
		{
			const dtCrowdAgent* ag = crowd_->getAgent(id);
			if (ag && ag->active)
				crowd_->requestMoveTarget(id, targetRef_, targetPos_);
		}
		else
		{
			for (int i = 0; i < crowd_->getAgentCount(); ++i)
			{
				const dtCrowdAgent* ag = crowd_->getAgent(i);
				if (!ag->active) continue;
				crowd_->requestMoveTarget(i, targetRef_, targetPos_);
			}
		}
	}
}

void CrowdNavMovement::Stop(int id)
{
	dtCrowdAgent* ag = crowd_->getEditableAgent(id);
	if (ag && ag->active)
		ag->desiredSpeed = 0.0f;
}

void CrowdNavMovement::Resume(int id)
{
	dtCrowdAgent* ag = crowd_->getEditableAgent(id);
	if (ag && ag->active)
		ag->desiredSpeed = ag->params.maxSpeed; // AddAgent 시 설정한 기준 속도.
}

bool CrowdNavMovement::Patrol(int id, const float* originPos, float radius, float* outDest)
{
	dtNavMeshQuery* navquery = nav_->query();
	const dtQueryFilter* filter = nav_->filter();
	const float* halfExtents = nav_->queryHalfExtents();

	// origin 의 폴리곤을 다시 찾아 임의 지점 탐색의 기준으로 삼는다.
	dtPolyRef originRef = 0;
	float originSnap[3];
	navquery->findNearestPoly(originPos, halfExtents, filter, &originRef, originSnap);
	if (!originRef)
		return false;

	const float searchRadius = (radius > 0.0f) ? radius : randomRadius_;

	float epos[3];
	dtPolyRef endRef;
	dtStatus status = navquery->findRandomPointAroundCircle(originRef, originSnap, searchRadius, filter, frand, &endRef, epos);
	if (dtStatusSucceed(status))
	{
		SetMoveTarget(id, epos, false);
		if (outDest)
			dtVcopy(outDest, epos);
		return true;
	}
	return false;
}

const float* CrowdNavMovement::GetPos(int id) const
{
	return crowd_->getAgent(id)->npos;
}

bool CrowdNavMovement::IsActive(int id) const
{
	const dtCrowdAgent* ag = crowd_->getAgent(id);
	return ag && ag->active;
}

bool CrowdNavMovement::Raycast(int id, const float* endPos, float* hitPoint) const
{
	const dtCrowdAgent* agent = crowd_->getAgent(id);
	if (!agent || !agent->active)
		return false;

	const dtQueryFilter* filter = crowd_->getFilter(agent->params.queryFilterType);
	const float* startPos = agent->npos;
	dtPolyRef startRef = agent->corridor.getPath()[0];

	dtRaycastHit rayHit;
	rayHit.maxPath = 0;
	nav_->query()->raycast(startRef, startPos, endPos, filter, DT_RAYCAST_USE_COSTS, &rayHit);

	if (rayHit.t > 0.0f && rayHit.t < 1.0f)
	{
		// hitPoint = startPos + (endPos - startPos) * t
		dtVsub(hitPoint, endPos, startPos);
		dtVscale(hitPoint, hitPoint, rayHit.t);
		dtVadd(hitPoint, startPos, hitPoint);
		return true;
	}
	return false;
}
