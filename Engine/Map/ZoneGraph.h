#pragma once
#include <functional>
#include <string>
#include <unordered_map>
#include <vector>
#include "syncnet_generated.h"

namespace gamedata {
	struct Map;
}

// 여러 navmesh 를 가로지르는 광역 경로 탐색.
//
// navmesh 하나는 맵 하나의 지형만 안다. 그래서 Detour 는 "이 맵 안에서 A→B" 는 풀 수 있어도
// "A 마을에서 B 던전까지 몇 개 맵을 거쳐 어떻게 가야 가장 가까운가" 는 답하지 못한다.
// ZoneGraph 는 그 위층을 담당한다 — 맵을 정점이 아니라 '게이트'를 정점으로 두고,
//
//   · 같은 맵 안의 게이트끼리      = 도보 간선(비용 = 그 맵 navmesh 위의 실제 이동 거리)
//   · 게이트와 그 target_id 지점   = 포탈 간선(비용 0, 맵 경계를 넘는다)
//
// 두 종류의 간선으로 잇는다. 이 그래프에서 다익스트라를 돌리면 "지금 위치 → 목적지" 가
// 맵 경계를 넘는 구간 목록(Route)으로 나온다. 실제 걷는 것은 각 구간마다 그 맵의
// navmesh 가 처리한다 — ZoneGraph 는 어느 게이트를 어떤 순서로 밟을지만 정한다.
//
// 그래프는 Map.json(ResourceLoader) 만으로 만들어진다. navmesh 없이도 성립하므로
// 클라/툴/테스트에서도 같은 결과를 얻을 수 있고, navmesh 를 넘겨주면 도보 간선의
// 비용만 직선 거리에서 실측 거리로 바뀐다.
class ZoneGraph
{
public:
	// 그래프의 정점. 맵 사이를 잇는 지점이다 — 게이트이거나, 게이트가 가리키는 도착 지점이다.
	// 맵 안의 일반 좌표는 정점이 아니다(탐색할 때 임시 정점으로 붙인다).
	struct Node
	{
		int markerId = 0;        // 전역 유일 마커 id(게이트 또는 player_spawn).
		int mapId = 0;           // 이 지점이 속한 맵.
		syncnet::Vec3 pos;       // 클라 좌표계(Map.json 값 그대로).
		bool isGate = false;     // false 면 게이트가 가리키는 도착 전용 지점.
		int requiredLevel = 0;   // 게이트 통과에 필요한 레벨.
	};

	// 경로의 한 구간. 하나의 맵(=하나의 navmesh) 안에서 걷는 몫이다.
	struct Step
	{
		int mapId = 0;
		syncnet::Vec3 from;      // 이 맵에서 걷기 시작하는 위치(클라 좌표계).
		syncnet::Vec3 to;        // 이 맵에서 도착할 위치.
		int gateId = 0;          // to 지점에서 통과할 게이트 id. 0 이면 최종 목적지다.
		float walkCost = 0.0f;   // from→to 도보 비용.
	};

	struct Route
	{
		bool found = false;
		float cost = 0.0f;             // 전체 도보 비용의 합(포탈 통과는 0).
		std::vector<Step> steps;       // 순서대로 따라가면 목적지에 닿는다.

		// 거쳐야 하는 맵 전환 횟수.
		int TransitionCount() const { return steps.empty() ? 0 : static_cast<int>(steps.size()) - 1; }
	};

	// 같은 맵 안 두 지점의 도보 비용을 재는 콜백. navmesh 를 아는 쪽(World)이 제공한다.
	// 재지 못하면(맵 미로드, 경로 단절) false 를 반환하면 된다 — 그때는 직선 거리를 쓴다.
	using WalkCostFn = std::function<bool(int mapId, const syncnet::Vec3& from, const syncnet::Vec3& to, float& outCost)>;

	// ResourceLoader 에 올라와 있는 맵 데이터로 그래프를 만든다.
	// measure 가 없으면 도보 비용은 전부 직선 거리다(Map.json 에 기록된 gate_links 는 그래도 우선한다).
	// 정점이 하나도 없으면(게이트가 없는 데이터) false.
	bool Build(WalkCostFn measure = nullptr);

	// 넘겨받은 맵 집합만으로 그래프를 만든다. 마커 참조(target_id)도 이 집합 안에서만 푼다
	// — ResourceLoader 의 전역 인덱스를 보지 않으므로, 테스트/벤치마크가 합성 맵으로
	// 규모나 위상을 바꿔 가며 검증할 수 있다.
	bool Build(const std::vector<const gamedata::Map*>& maps, WalkCostFn measure);

	void Clear();

	// fromPos(fromMapId) 에서 toPos(toMapId) 까지 맵을 넘나드는 최단 경로.
	// level 이 1 이상이면 required_level 이 그보다 높은 게이트는 지나지 않는다(0 이면 제한 없음).
	Route FindRoute(int fromMapId, const syncnet::Vec3& fromPos,
		int toMapId, const syncnet::Vec3& toPos, int level = 0) const;

	// 진단용: 두 맵이 게이트로 이어져 있는가(도보 비용은 보지 않는다).
	bool AreConnected(int fromMapId, int toMapId, int level = 0) const;

	// 그래프 요약을 로그로 남긴다(정점/간선 수, 비용을 실측하지 못한 간선 수).
	void LogSummary() const;

	size_t NodeCount() const { return nodes_.size(); }
	size_t EdgeCount() const;
	// 비용을 실측하지 못하고 직선 거리로 대체한 도보 간선 수. 0 이 아니면 그만큼 라우팅이 부정확하다.
	int EstimatedEdgeCount() const { return estimatedEdges_; }

	// 진단/툴용: 같은 맵 안 두 마커를 잇는 도보 간선의 비용. 간선이 없으면 false.
	// 그래프가 어떤 가중치를 쓰고 있는지 눈으로 확인할 때 쓴다(Map.json 에 기록할 값을 뽑을 때도).
	bool GetWalkCost(int fromMarkerId, int toMarkerId, float& outCost) const;

	const std::vector<Node>& GetNodes() const { return nodes_; }
	// 마커 id 로 정점을 찾는다. 없으면 nullptr.
	const Node* FindNode(int markerId) const;

private:
	struct Edge
	{
		int to = 0;           // 도착 정점 인덱스.
		float cost = 0.0f;
		int gateId = 0;       // 0 이 아니면 포탈(이 게이트를 통과해 맵을 넘는다).
	};

	std::vector<Node> nodes_;
	std::vector<std::vector<Edge>> adjacency_;
	std::unordered_map<int, int> nodeByMarker_;          // 마커 id -> 정점 인덱스.
	std::unordered_map<int, std::vector<int>> nodesByMap_;
	// 맵 id -> field 맵인가. field 가 아닌 맵(레이드 등 인스턴스)은 목적지일 때만 지나갈 수 있다.
	std::unordered_map<int, bool> fieldMaps_;

	WalkCostFn measure_;
	int estimatedEdges_ = 0;

	// 정점을 추가하고 인덱스를 반환한다. 이미 있으면 기존 인덱스.
	int AddNode(const Node& node);

	// 같은 맵 안 두 지점의 도보 비용. 실측(navmesh)에 실패하면 직선 거리를 쓴다.
	float MeasureWalk(int mapId, const syncnet::Vec3& a, const syncnet::Vec3& b) const;

	// MeasureWalk 와 같지만, 직선 거리로 대체한 경우를 estimatedEdges_ 에 센다(그래프 빌드 전용).
	float WalkCost(int mapId, const syncnet::Vec3& a, const syncnet::Vec3& b);

	// Map.json 에 기록된 gate_links 비용. 없으면 -1.
	static float RecordedCost(const gamedata::Map* map, int fromMarker, int toMarker);

	// 인스턴스 맵으로 들어가는 포탈인지 — 목적지가 아니면 지나갈 수 없다.
	bool CanEnter(int mapId, int destMapId) const;
};
