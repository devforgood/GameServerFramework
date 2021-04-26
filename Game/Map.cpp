#include "Map.h"
#include <math.h>
#include <stdio.h>
#include <cstring>
#include "Recast.h"
//#include "RecastDebugDraw.h"
//#include "RecastDump.h"
#include "DetourNavMesh.h"
#include "DetourNavMeshBuilder.h"
#include "DetourNavMeshQuery.h"
//#include "DetourDebugDraw.h"
//#include "NavMeshTesterTool.h"
//#include "NavMeshPruneTool.h"
//#include "OffMeshConnectionTool.h"
//#include "ConvexVolumeTool.h"
//#include "CrowdTool.h"
#include "DetourCrowd.h"
#include "PerfTimer.h"
#include "DetourCommon.h"
#include <iostream>
#include "LogHelper.h"

static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

struct NavMeshSetHeader
{
	int magic;
	int version;
	int numTiles;
	dtNavMeshParams params;
};

struct NavMeshTileHeader
{
	dtTileRef tileRef;
	int dataSize;
};


dtNavMesh* loadAll(const char* path)
{
	FILE* fp;
	errno_t err = fopen_s(&fp, path, "rb");
	if (err != 0)
	{
		return 0;
	}

	// Read header.
	NavMeshSetHeader header;
	size_t readLen = fread(&header, sizeof(NavMeshSetHeader), 1, fp);
	if (readLen != 1)
	{
		fclose(fp);
		return 0;
	}
	if (header.magic != NAVMESHSET_MAGIC)
	{
		fclose(fp);
		return 0;
	}
	if (header.version != NAVMESHSET_VERSION)
	{
		fclose(fp);
		return 0;
	}

	dtNavMesh* mesh = dtAllocNavMesh();
	if (!mesh)
	{
		fclose(fp);
		return 0;
	}
	dtStatus status = mesh->init(&header.params);
	if (dtStatusFailed(status))
	{
		fclose(fp);
		return 0;
	}

	// Read tiles.
	for (int i = 0; i < header.numTiles; ++i)
	{
		NavMeshTileHeader tileHeader;
		readLen = fread(&tileHeader, sizeof(tileHeader), 1, fp);
		if (readLen != 1)
		{
			fclose(fp);
			return 0;
		}

		if (!tileHeader.tileRef || !tileHeader.dataSize)
			break;

		unsigned char* data = (unsigned char*)dtAlloc(tileHeader.dataSize, DT_ALLOC_PERM);
		if (!data) break;
		memset(data, 0, tileHeader.dataSize);
		readLen = fread(data, tileHeader.dataSize, 1, fp);
		if (readLen != 1)
		{
			dtFree(data);
			fclose(fp);
			return 0;
		}

		mesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
	}

	fclose(fp);

	return mesh;
}

void Map::Init()
{
	m_navQuery = dtAllocNavMeshQuery();
	m_crowd = dtAllocCrowd();

	dtFreeNavMesh(m_navMesh);
	m_navMesh = loadAll("solo_navmesh.bin");
	m_navQuery->init(m_navMesh, 2048);

	dtNavMesh* nav = m_navMesh;
	dtCrowd* crowd = m_crowd;

	//if (nav && crowd && (m_navMesh != nav || m_crowd != crowd))
	{
		m_navMesh = nav;
		m_crowd = crowd;

		crowd->init(MAX_AGENTS, m_agentRadius, nav);

		// Make polygons with 'disabled' flag invalid.
		crowd->getEditableFilter(0)->setExcludeFlags(SAMPLE_POLYFLAGS_DISABLED);

		// Setup local avoidance params to different qualities.
		dtObstacleAvoidanceParams params;
		// Use mostly default settings, copy from dtCrowd.
		memcpy(&params, crowd->getObstacleAvoidanceParams(0), sizeof(dtObstacleAvoidanceParams));

		// Low (11)
		params.velBias = 0.5f;
		params.adaptiveDivs = 5;
		params.adaptiveRings = 2;
		params.adaptiveDepth = 1;
		crowd->setObstacleAvoidanceParams(0, &params);

		// Medium (22)
		params.velBias = 0.5f;
		params.adaptiveDivs = 5;
		params.adaptiveRings = 2;
		params.adaptiveDepth = 2;
		crowd->setObstacleAvoidanceParams(1, &params);

		// Good (45)
		params.velBias = 0.5f;
		params.adaptiveDivs = 7;
		params.adaptiveRings = 2;
		params.adaptiveDepth = 3;
		crowd->setObstacleAvoidanceParams(2, &params);

		// High (66)
		params.velBias = 0.5f;
		params.adaptiveDivs = 7;
		params.adaptiveRings = 3;
		params.adaptiveDepth = 3;

		crowd->setObstacleAvoidanceParams(3, &params);
	}
}

void Map::update(float dt)
{
	dtNavMesh* nav = m_navMesh;
	dtCrowd* crowd = m_crowd;
	if (!nav || !crowd) return;

	//TimeVal startTime = getPerfTime();

	crowd->update(dt, nullptr);

	//TimeVal endTime = getPerfTime();
}


int Map::addAgent(const float* p, float speed = 3.5f)
{
	dtCrowd* crowd = m_crowd;

	dtCrowdAgentParams ap;
	memset(&ap, 0, sizeof(ap));
	ap.radius = m_agentRadius;
	ap.height = m_agentHeight;
	ap.maxAcceleration = 8.0f;
	ap.maxSpeed = speed;
	ap.collisionQueryRange = ap.radius * 12.0f;
	ap.pathOptimizationRange = ap.radius * 30.0f;
	ap.updateFlags = 0;
	if (m_toolParams.m_anticipateTurns)
		ap.updateFlags |= DT_CROWD_ANTICIPATE_TURNS;
	if (m_toolParams.m_optimizeVis)
		ap.updateFlags |= DT_CROWD_OPTIMIZE_VIS;
	if (m_toolParams.m_optimizeTopo)
		ap.updateFlags |= DT_CROWD_OPTIMIZE_TOPO;
	if (m_toolParams.m_obstacleAvoidance)
		ap.updateFlags |= DT_CROWD_OBSTACLE_AVOIDANCE;
	if (m_toolParams.m_separation)
		ap.updateFlags |= DT_CROWD_SEPARATION;
	ap.obstacleAvoidanceType = (unsigned char)m_toolParams.m_obstacleAvoidanceType;
	ap.separationWeight = m_toolParams.m_separationWeight;

	int idx = crowd->addAgent(p, &ap);
	if (idx != -1)
	{
		if (m_targetRef)
			crowd->requestMoveTarget(idx, m_targetRef, m_targetPos);

	}

	auto agent = crowd->getAgent(idx);
	LOG.info("add agent {} pos({}, {}, {})", idx, -1 * agent->npos[0], agent->npos[1], agent->npos[2]);
	return idx;
}

void Map::removeAgent(const int idx)
{
	m_crowd->removeAgent(idx);
}

static void calcVel(float* vel, const float* pos, const float* tgt, const float speed)
{
	dtVsub(vel, tgt, pos);
	vel[1] = 0.0;
	dtVnormalize(vel);
	dtVscale(vel, vel, speed);
}

void Map::setMoveTarget(const float* p, bool adjust, const int agent_idx)
{

	// Find nearest point on navmesh and set move request to that location.
	dtNavMeshQuery* navquery = m_navQuery;
	dtCrowd* crowd = m_crowd;
	const dtQueryFilter* filter = crowd->getFilter(0);
	const float* halfExtents = crowd->getQueryExtents();

	if (adjust)
	{
		float vel[3];
		// Request velocity
		if (agent_idx != -1)
		{
			const dtCrowdAgent* ag = crowd->getAgent(agent_idx);
			if (ag && ag->active)
			{
				calcVel(vel, ag->npos, p, ag->params.maxSpeed);
				crowd->requestMoveVelocity(agent_idx, vel);
			}
		}
		else
		{
			for (int i = 0; i < crowd->getAgentCount(); ++i)
			{
				const dtCrowdAgent* ag = crowd->getAgent(i);
				if (!ag->active) continue;
				calcVel(vel, ag->npos, p, ag->params.maxSpeed);
				crowd->requestMoveVelocity(i, vel);
			}
		}
	}
	else
	{
		navquery->findNearestPoly(p, halfExtents, filter, &m_targetRef, m_targetPos);

		if (agent_idx != -1)
		{
			const dtCrowdAgent* ag = crowd->getAgent(agent_idx);
			if (ag && ag->active)
				crowd->requestMoveTarget(agent_idx, m_targetRef, m_targetPos);
		}
		else
		{
			for (int i = 0; i < crowd->getAgentCount(); ++i)
			{
				const dtCrowdAgent* ag = crowd->getAgent(i);
				if (!ag->active) continue;
				crowd->requestMoveTarget(i, m_targetRef, m_targetPos);
			}
		}
	}
}

bool Map::raycast(int agent_idx, const float* endPos, float * hitPoint)
{
	auto agent = m_crowd->getAgent(agent_idx);
	auto filter = m_crowd->getFilter(agent->params.queryFilterType);

	const float* startPos = agent->npos;
	dtPolyRef startRef = agent->corridor.getPath()[0];

	dtRaycastHit rayHit;
	rayHit.maxPath = 0;
	auto ret = m_navQuery->raycast(startRef, startPos, endPos, filter, DT_RAYCAST_USE_COSTS, &rayHit);

	if (rayHit.t > 0.0f && rayHit.t < 1.0f)
	{
		// hitPoint = startPos + (endPos - startPos) * t
		dtVsub(hitPoint, endPos, startPos);
		dtVscale(hitPoint, hitPoint, rayHit.t);
		dtVadd(hitPoint, startPos, hitPoint);
		return true;
	}
	LOG.info("raycast hit t : {}, ret:{}", rayHit.t, ret);
	LOG.info("raycast dtStatusSucceed {}", dtStatusSucceed(ret));
	LOG.info("raycast dtStatusFailed {}", dtStatusFailed(ret));
	LOG.info("raycast dtStatusInProgress {}", dtStatusInProgress(ret));

	LOG.info("raycast dtStatusDetail DT_WRONG_MAGIC {}", dtStatusDetail(ret, DT_WRONG_MAGIC));
	LOG.info("raycast dtStatusDetail DT_WRONG_VERSION {}", dtStatusDetail(ret, DT_WRONG_VERSION));
	LOG.info("raycast dtStatusDetail DT_OUT_OF_MEMORY {}", dtStatusDetail(ret, DT_OUT_OF_MEMORY));
	LOG.info("raycast dtStatusDetail DT_INVALID_PARAM {}", dtStatusDetail(ret, DT_INVALID_PARAM));
	LOG.info("raycast dtStatusDetail DT_BUFFER_TOO_SMALL {}", dtStatusDetail(ret, DT_BUFFER_TOO_SMALL));
	LOG.info("raycast dtStatusDetail DT_OUT_OF_NODES {}", dtStatusDetail(ret, DT_OUT_OF_NODES));
	LOG.info("raycast dtStatusDetail DT_PARTIAL_RESULT {}", dtStatusDetail(ret, DT_PARTIAL_RESULT));
	LOG.info("raycast dtStatusDetail DT_ALREADY_OCCUPIED {}", dtStatusDetail(ret, DT_ALREADY_OCCUPIED));

	return false;
}

const float* Map::getPos(const int agent_idx)
{
	return m_crowd->getAgent(agent_idx)->npos;
}
