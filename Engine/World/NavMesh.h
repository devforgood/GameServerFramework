#pragma once

#include "DetourNavMesh.h"
#include "DetourNavMeshQuery.h"

// 폴리곤 파티션/플래그/영역 정의 (recastnavigation 샘플과 동일).
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
	SAMPLE_POLYFLAGS_DISABLED = 0x10,	// Disabled polygon
	SAMPLE_POLYFLAGS_ALL = 0xffff		// All abilities.
};

enum SamplePolyAreas
{
	SAMPLE_POLYAREA_GROUND,
	SAMPLE_POLYAREA_WATER,
	SAMPLE_POLYAREA_ROAD,
	SAMPLE_POLYAREA_DOOR,
	SAMPLE_POLYAREA_GRASS,
	SAMPLE_POLYAREA_JUMP,
};

// 네비메시 데이터와 쿼리/필터를 소유하는 공유 자원.
// 이동 전략(INavMovement) 구현들이 공통으로 참조한다(소유권은 Map 이 가진다).
class NavMesh
{
	dtNavMesh* navMesh_ = nullptr;
	dtNavMeshQuery* navQuery_ = nullptr;
	dtQueryFilter filter_;

	// 에이전트 기하 설정(에이전트 반경, 쿼리 박스 등에 사용).
	float agentRadius_ = 0.6f;
	float agentHeight_ = 2.0f;
	float queryHalfExtents_[3];

public:
	NavMesh();
	~NavMesh();

	// 네비메시 바이너리를 로드하고 쿼리/필터를 구성한다. 성공 시 true.
	bool Load(const char* path);

	bool IsLoaded() const { return navMesh_ != nullptr; }

	dtNavMesh* mesh() const { return navMesh_; }
	dtNavMeshQuery* query() const { return navQuery_; }
	const dtQueryFilter* filter() const { return &filter_; }

	float agentRadius() const { return agentRadius_; }
	float agentHeight() const { return agentHeight_; }
	const float* queryHalfExtents() const { return queryHalfExtents_; }
};
