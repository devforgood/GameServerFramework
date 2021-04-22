#pragma once

#include "Recast.h"
#include "DetourNavMesh.h"

enum SamplePartitionType
{
	SAMPLE_PARTITION_WATERSHED,
	SAMPLE_PARTITION_MONOTONE,
	SAMPLE_PARTITION_LAYERS,
};

enum SamplePolyFlags
{
	SAMPLE_POLYFLAGS_WALK = 0x01,		// Ability to walk (ground, grass, road)
	SAMPLE_POLYFLAGS_SWIM = 0x02,		// Ability to swim (water).
	SAMPLE_POLYFLAGS_DOOR = 0x04,		// Ability to move through doors.
	SAMPLE_POLYFLAGS_JUMP = 0x08,		// Ability to jump.
	SAMPLE_POLYFLAGS_DISABLED = 0x10,		// Disabled polygon
	SAMPLE_POLYFLAGS_ALL = 0xffff	// All abilities.
};

struct CrowdToolParams
{
	bool m_expandSelectedDebugDraw;
	bool m_showCorners;
	bool m_showCollisionSegments;
	bool m_showPath;
	bool m_showVO;
	bool m_showOpt;
	bool m_showNeis;

	bool m_expandDebugDraw;
	bool m_showLabels;
	bool m_showGrid;
	bool m_showNodes;
	bool m_showPerfGraph;
	bool m_showDetailAll;

	bool m_expandOptions;
	bool m_anticipateTurns;
	bool m_optimizeVis;
	bool m_optimizeTopo;
	bool m_obstacleAvoidance;
	float m_obstacleAvoidanceType;
	bool m_separation;
	float m_separationWeight;
};


struct dtRaycastHit;
class Map
{
protected:
	class InputGeom* m_geom;
	class dtNavMesh* m_navMesh;
	class dtNavMeshQuery* m_navQuery;
	class dtCrowd* m_crowd;


	// setting
	float m_cellSize;
	float m_cellHeight;
	float m_agentHeight;
	float m_agentRadius;
	float m_agentMaxClimb;
	float m_agentMaxSlope;
	float m_regionMinSize;
	float m_regionMergeSize;
	float m_edgeMaxLen;
	float m_edgeMaxError;
	float m_vertsPerPoly;
	float m_detailSampleDist;
	float m_detailSampleMaxError;
	int m_partitionType;

	static const int MAX_AGENTS = 128;

	CrowdToolParams m_toolParams;

	float m_targetPos[3];
	dtPolyRef m_targetRef;

public:
	Map()
	{
		m_cellSize = 0.3f;
		m_cellHeight = 0.2f;
		m_agentHeight = 2.0f;
		m_agentRadius = 0.6f;
		m_agentMaxClimb = 0.9f;
		m_agentMaxSlope = 45.0f;
		m_regionMinSize = 8;
		m_regionMergeSize = 20;
		m_edgeMaxLen = 12.0f;
		m_edgeMaxError = 1.3f;
		m_vertsPerPoly = 6.0f;
		m_detailSampleDist = 6.0f;
		m_detailSampleMaxError = 1.0f;
		m_partitionType = SAMPLE_PARTITION_WATERSHED;


		m_toolParams.m_expandSelectedDebugDraw = true;
		m_toolParams.m_showCorners = false;
		m_toolParams.m_showCollisionSegments = false;
		m_toolParams.m_showPath = false;
		m_toolParams.m_showVO = false;
		m_toolParams.m_showOpt = false;
		m_toolParams.m_showNeis = false;
		m_toolParams.m_expandDebugDraw = false;
		m_toolParams.m_showLabels = false;
		m_toolParams.m_showGrid = false;
		m_toolParams.m_showNodes = false;
		m_toolParams.m_showPerfGraph = false;
		m_toolParams.m_showDetailAll = false;
		m_toolParams.m_expandOptions = true;
		m_toolParams.m_anticipateTurns = true;
		m_toolParams.m_optimizeVis = true;
		m_toolParams.m_optimizeTopo = true;
		m_toolParams.m_obstacleAvoidance = true;
		m_toolParams.m_obstacleAvoidanceType = 3.0f;
		m_toolParams.m_separation = false;
		m_toolParams.m_separationWeight = 2.0f;

		m_targetRef = 0;
	}

	void Init();
	void update(float dt);
	void addAgent(const float* p);
	void removeAgent(const int idx);
	void setMoveTarget(const float* p, bool adjust);

	dtCrowd* crowd() { return m_crowd; }

	bool raycast(int agent_idx, const float* endPos, float* hitPoint);

};

