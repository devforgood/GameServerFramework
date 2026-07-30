#include "NavMesh.h"
#include <cstdio>
#include <cstring>
#include "DetourCommon.h"
#include "LogHelper.h"

static const int NAVMESHSET_MAGIC = 'M' << 24 | 'S' << 16 | 'E' << 8 | 'T'; //'MSET';
static const int NAVMESHSET_VERSION = 1;

namespace
{
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
			return nullptr;

		// Read header.
		NavMeshSetHeader header;
		size_t readLen = fread(&header, sizeof(NavMeshSetHeader), 1, fp);
		if (readLen != 1) { fclose(fp); return nullptr; }
		if (header.magic != NAVMESHSET_MAGIC) { fclose(fp); return nullptr; }
		if (header.version != NAVMESHSET_VERSION) { fclose(fp); return nullptr; }

		dtNavMesh* mesh = dtAllocNavMesh();
		if (!mesh) { fclose(fp); return nullptr; }

		dtStatus status = mesh->init(&header.params);
		if (dtStatusFailed(status)) { fclose(fp); return nullptr; }

		// Read tiles.
		for (int i = 0; i < header.numTiles; ++i)
		{
			NavMeshTileHeader tileHeader;
			readLen = fread(&tileHeader, sizeof(tileHeader), 1, fp);
			if (readLen != 1) { fclose(fp); return nullptr; }

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
				return nullptr;
			}

			mesh->addTile(data, tileHeader.dataSize, DT_TILE_FREE_DATA, tileHeader.tileRef, 0);
		}

		fclose(fp);
		return mesh;
	}
}

NavMesh::NavMesh()
{
	// dtCrowd 의 기본 쿼리 박스와 유사하게 에이전트 반경 기준으로 설정한다.
	queryHalfExtents_[0] = agentRadius_ * 2.0f;
	queryHalfExtents_[1] = agentHeight_;
	queryHalfExtents_[2] = agentRadius_ * 2.0f;
}

NavMesh::~NavMesh()
{
	if (navQuery_)
	{
		dtFreeNavMeshQuery(navQuery_);
		navQuery_ = nullptr;
	}
	if (navMesh_)
	{
		dtFreeNavMesh(navMesh_);
		navMesh_ = nullptr;
	}
}

bool NavMesh::Load(const char* path)
{
	navMesh_ = loadAll(path);
	if (navMesh_ == nullptr)
	{
		// 자산을 못 찾으면(작업 디렉터리 문제 등) 이후 쿼리가 null 네비메시를
		// 역참조하며 크래시한다. 원인을 명확히 남긴다.
		LOG.error("NavMesh::Load failed to load '{}' (check working directory)", path);
		return false;
	}

	navQuery_ = dtAllocNavMeshQuery();
	navQuery_->init(navMesh_, 2048);

	// 공유 필터: 비활성 폴리곤 제외 + 영역별 비용.
	filter_.setIncludeFlags(SAMPLE_POLYFLAGS_ALL ^ SAMPLE_POLYFLAGS_DISABLED);
	filter_.setExcludeFlags(0);
	filter_.setAreaCost(SAMPLE_POLYAREA_GROUND, 1.0f);
	filter_.setAreaCost(SAMPLE_POLYAREA_WATER, 10.0f);
	filter_.setAreaCost(SAMPLE_POLYAREA_ROAD, 1.0f);
	filter_.setAreaCost(SAMPLE_POLYAREA_DOOR, 1.0f);
	filter_.setAreaCost(SAMPLE_POLYAREA_GRASS, 2.0f);
	filter_.setAreaCost(SAMPLE_POLYAREA_JUMP, 1.5f);

	return true;
}

bool NavMesh::Bounds(float* outMin, float* outMax) const
{
	if (navMesh_ == nullptr || outMin == nullptr || outMax == nullptr)
		return false;

	bool found = false;
	for (int i = 0; i < navMesh_->getMaxTiles(); ++i)
	{
		// const 오버로드로 받아야 dtNavMesh 내부 배열을 그대로 볼 수 있다.
		const dtMeshTile* tile = const_cast<const dtNavMesh*>(navMesh_)->getTile(i);
		if (tile == nullptr || tile->header == nullptr)
			continue;   // 비어 있는 타일 슬롯.

		if (!found)
		{
			dtVcopy(outMin, tile->header->bmin);
			dtVcopy(outMax, tile->header->bmax);
			found = true;
			continue;
		}
		dtVmin(outMin, tile->header->bmin);
		dtVmax(outMax, tile->header->bmax);
	}
	return found;
}
