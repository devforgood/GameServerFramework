#pragma once

#include "Recast.h"

class Map
{
protected:
	class InputGeom* m_geom;
	class dtNavMesh* m_navMesh;
	class dtNavMeshQuery* m_navQuery;
	class dtCrowd* m_crowd;

public:
	void Init();
	void update(float deltaTime);
};

