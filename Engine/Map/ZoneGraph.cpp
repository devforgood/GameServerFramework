#include "ZoneGraph.h"

#include <algorithm>
#include <cmath>
#include <cstddef>
#include <limits>
#include <memory_resource>
#include <queue>
#include <span>

#include "GameData/ResourceLoader.h"
#include "LogHelper.h"

namespace
{
	// 게이트 통과 자체의 비용. 순간이동이라 거리를 더하지 않는다.
	constexpr float kPortalCost = 0.0f;

	constexpr float kInfinity = std::numeric_limits<float>::infinity();

	// FindRoute/AreConnected 가 쓰는 스크래치 메모리 크기(스택).
	//
	// 정점 하나에 16바이트(dist/prev/viaGate/edgeCost)를 쓰고, 우선순위 큐가 간선 수만큼
	// 8바이트 항목을 담는다(monotonic 은 재사용하지 않으므로 큐가 커지며 버린 블록도
	// 그대로 쌓인다 - 대략 두 배로 잡는다). 실제 데이터는 정점 7 / 간선 10 규모라
	// 1KB 도 쓰지 않으며, 4KB 면 정점 100 개까지 스택 안에서 끝난다.
	//
	// 모자라도 monotonic_buffer_resource 가 기본 리소스(힙)에서 더 받아 온다. 즉 크기를
	// 잘못 잡으면 느려질 뿐 깨지지 않는다.
	constexpr std::size_t kScratchBytes = 4 * 1024;

	template<typename TPosition>
	syncnet::Vec3 ToVec3(const TPosition& p)
	{
		return syncnet::Vec3(
			static_cast<float>(p.x),
			static_cast<float>(p.y),
			static_cast<float>(p.z));
	}

	float StraightDistance(const syncnet::Vec3& a, const syncnet::Vec3& b)
	{
		const float dx = a.x() - b.x();
		const float dy = a.y() - b.y();
		const float dz = a.z() - b.z();
		return std::sqrt(dx * dx + dy * dy + dz * dz);
	}
}

void ZoneGraph::Clear()
{
	nodes_.clear();
	adjacency_.clear();
	nodeByMarker_.clear();
	nodesByMap_.clear();
	fieldMaps_.clear();
	estimatedEdges_ = 0;
}

int ZoneGraph::AddNode(const Node& node)
{
	auto itr = nodeByMarker_.find(node.markerId);
	if (itr != nodeByMarker_.end())
		return itr->second;

	const int index = static_cast<int>(nodes_.size());
	nodes_.push_back(node);
	adjacency_.emplace_back();
	nodeByMarker_[node.markerId] = index;
	nodesByMap_[node.mapId].push_back(index);
	return index;
}

const ZoneGraph::Node* ZoneGraph::FindNode(int markerId) const
{
	auto itr = nodeByMarker_.find(markerId);
	return itr != nodeByMarker_.end() ? &nodes_[itr->second] : nullptr;
}

size_t ZoneGraph::EdgeCount() const
{
	size_t count = 0;
	for (const auto& list : adjacency_)
		count += list.size();
	return count;
}

bool ZoneGraph::GetWalkCost(int fromMarkerId, int toMarkerId, float& outCost) const
{
	auto from = nodeByMarker_.find(fromMarkerId);
	auto to = nodeByMarker_.find(toMarkerId);
	if (from == nodeByMarker_.end() || to == nodeByMarker_.end())
		return false;

	for (const Edge& edge : adjacency_[from->second])
	{
		if (edge.gateId == 0 && edge.to == to->second)
		{
			outCost = edge.cost;
			return true;
		}
	}
	return false;
}

float ZoneGraph::RecordedCost(const gamedata::Map* map, int fromMarker, int toMarker)
{
	if (map == nullptr)
		return -1.0f;

	// 방향이 반대로 기록돼 있어도 같은 통로다(navmesh 도보 거리는 대칭).
	for (const auto& link : map->gate_links)
	{
		if ((link.from_id == fromMarker && link.to_id == toMarker) ||
			(link.from_id == toMarker && link.to_id == fromMarker))
			return static_cast<float>(link.cost);
	}
	return -1.0f;
}

float ZoneGraph::MeasureWalk(int mapId, const syncnet::Vec3& a, const syncnet::Vec3& b) const
{
	float cost = 0.0f;
	if (measure_ && measure_(mapId, a, b, cost) && cost >= 0.0f)
		return cost;

	// 실측 실패(맵 미로드 / 경로 단절). 직선 거리로 근사한다 — 그래프의 연결 관계는
	// Map.json 이 정하므로 경로 자체는 나오지만, 비용은 실제보다 낙관적일 수 있다.
	return StraightDistance(a, b);
}

float ZoneGraph::WalkCost(int mapId, const syncnet::Vec3& a, const syncnet::Vec3& b)
{
	float cost = 0.0f;
	if (measure_ && measure_(mapId, a, b, cost) && cost >= 0.0f)
		return cost;

	++estimatedEdges_;
	return StraightDistance(a, b);
}

bool ZoneGraph::CanEnter(int mapId, int destMapId) const
{
	if (mapId == destMapId)
		return true;   // 목적지면 인스턴스라도 들어간다.

	auto itr = fieldMaps_.find(mapId);
	// field 가 아닌 맵(레이드 등)은 통과 경로로 쓸 수 없다 — 입장할 때마다 새 인스턴스가
	// 만들어지므로 '반대편으로 빠져나가는 지름길' 이 성립하지 않는다.
	return itr == fieldMaps_.end() || itr->second;
}

bool ZoneGraph::Build(WalkCostFn measure)
{
	std::vector<const gamedata::Map*> maps;
	maps.reserve(ResourceLoader::Instance().GetMaps().size());
	for (const auto& pair : ResourceLoader::Instance().GetMaps())
	{
		if (pair.second != nullptr)
			maps.push_back(pair.second);
	}

	// 그래프 모양이 순회 순서에 흔들리지 않도록 맵 id 로 정렬한다
	// (GetMaps() 는 unordered_map 이라 순서가 비결정적이다).
	std::sort(maps.begin(), maps.end(),
		[](const gamedata::Map* a, const gamedata::Map* b) { return a->id < b->id; });

	return Build(maps, std::move(measure));
}

bool ZoneGraph::Build(const std::vector<const gamedata::Map*>& maps, WalkCostFn measure)
{
	Clear();
	measure_ = std::move(measure);

	auto& resource = ResourceLoader::Instance();

	// ── 1) 맵마다 field 인지 기록. 인스턴스 맵은 경유지로 쓸 수 없다. ──
	for (const gamedata::Map* map : maps)
	{
		if (map == nullptr)
			continue;
		const gamedata::GameMode* mode = resource.GetGameMode(map->game_mode_id);
		fieldMaps_[map->id] = (mode != nullptr && mode->type == "field");
	}

	// ── 2) 정점: 모든 게이트. ──
	for (const gamedata::Map* map : maps)
	{
		if (map == nullptr)
			continue;

		for (const auto& gate : map->gates)
		{
			Node node;
			node.markerId = gate.id;
			node.mapId = map->id;
			node.pos = ToVec3(gate.position);
			node.isGate = true;
			node.requiredLevel = gate.required_level;
			AddNode(node);
		}
	}

	// ── 3) 정점: 게이트가 가리키는 도착 지점(짝 게이트가 없는 입구는 player_spawn 을 가리킨다). ──
	// 스폰 지점은 '누군가 가리킬 때만' 정점이 된다. 맵 하나에 스폰이 수백 개씩 있는데
	// 전부 정점으로 올리면 도보 간선이 O(스폰 수²) 로 폭발하고, 아무도 안 가는 지점끼리
	// 잇느라 그래프만 커진다.
	std::unordered_map<int, std::pair<const gamedata::Map*, const gamedata::MapSpawnPointsPlayerSpawn*>> spawnById;
	for (const gamedata::Map* map : maps)
	{
		if (map == nullptr)
			continue;
		for (const auto& spawn : map->spawn_points.player_spawn)
			spawnById[spawn.id] = { map, &spawn };
	}

	for (const gamedata::Map* map : maps)
	{
		if (map == nullptr)
			continue;

		for (const auto& gate : map->gates)
		{
			if (nodeByMarker_.count(gate.target_id))
				continue;   // 이미 게이트 정점으로 올라와 있다.

			auto found = spawnById.find(gate.target_id);
			if (found == spawnById.end())
				continue;   // 끊어진 참조는 4)에서 경고한다.

			Node node;
			node.markerId = found->second.second->id;
			node.mapId = found->second.first->id;
			node.pos = ToVec3(found->second.second->position);
			node.isGate = false;
			AddNode(node);
		}
	}

	// ── 4) 포탈 간선: 게이트 -> target_id. 맵 경계를 넘는 유일한 수단이다. ──
	for (const gamedata::Map* map : maps)
	{
		if (map == nullptr)
			continue;

		for (const auto& gate : map->gates)
		{
			auto target = nodeByMarker_.find(gate.target_id);
			if (target == nodeByMarker_.end())
			{
				LOG.warn("ZoneGraph: map {} gate {} 의 target_id {} 를 찾지 못해 경로에서 제외합니다.",
					map->id, gate.id, gate.target_id);
				continue;
			}

			const int from = nodeByMarker_[gate.id];
			adjacency_[from].push_back(Edge{ target->second, kPortalCost, gate.id });
		}
	}

	// ── 5) 도보 간선: 같은 맵 안의 정점끼리 전부 잇는다. ──
	// 비용은 Map.json 에 기록된 값 > navmesh 실측 > 직선 거리 순으로 정한다.
	std::unordered_map<int, const gamedata::Map*> mapById;
	for (const gamedata::Map* map : maps)
	{
		if (map != nullptr)
			mapById[map->id] = map;
	}

	for (const auto& pair : nodesByMap_)
	{
		const int mapId = pair.first;
		const std::vector<int>& list = pair.second;
		auto mapData = mapById.find(mapId);

		for (size_t i = 0; i < list.size(); ++i)
		{
			for (size_t j = i + 1; j < list.size(); ++j)
			{
				const Node& a = nodes_[list[i]];
				const Node& b = nodes_[list[j]];

				float cost = RecordedCost(mapData != mapById.end() ? mapData->second : nullptr,
					a.markerId, b.markerId);
				if (cost < 0.0f)
					cost = WalkCost(mapId, a.pos, b.pos);

				adjacency_[list[i]].push_back(Edge{ list[j], cost, 0 });
				adjacency_[list[j]].push_back(Edge{ list[i], cost, 0 });
			}
		}
	}

	return !nodes_.empty();
}

void ZoneGraph::LogSummary() const
{
	LOG.info("ZoneGraph: 정점 {} 개(맵 {} 개), 간선 {} 개.",
		nodes_.size(), nodesByMap_.size(), EdgeCount());

	if (estimatedEdges_ > 0)
	{
		LOG.warn("ZoneGraph: 도보 간선 {} 개의 비용을 실측하지 못해 직선 거리로 대체했습니다. "
			"해당 맵의 navmesh 가 로드되지 않았거나 두 지점이 이어져 있지 않습니다 "
			"— Map.json 의 gate_links 에 비용을 기록하면 정확해집니다.", estimatedEdges_);
	}
}

ZoneGraph::Route ZoneGraph::FindRoute(int fromMapId, const syncnet::Vec3& fromPos,
	int toMapId, const syncnet::Vec3& toPos, int level) const
{
	Route route;

	// 출발/목적 맵 중 하나라도 그래프에 없으면 탐색할 것이 없다. 이 검사가 없으면
	// 없는 맵 id 하나에 '도달 가능한 정점 전부 훑기'(최악 비용)를 그대로 물게 된다
	// — 클라가 보낸 값으로 조회하는 자리라면 그게 곧 부하가 된다.
	if (fromMapId != toMapId &&
		(nodesByMap_.count(fromMapId) == 0 || nodesByMap_.count(toMapId) == 0))
		return route;

	const int nodeCount = static_cast<int>(nodes_.size());
	const int start = nodeCount;        // 출발 위치를 나타내는 임시 정점.
	const int goal = nodeCount + 1;     // 목적지 위치를 나타내는 임시 정점.
	const int total = nodeCount + 2;

	// 아래 컨테이너들은 전부 이 함수 안에서 태어나 이 함수 안에서 죽는다. 종류가
	// 제각각이고(float/int/Edge/pair) 크기가 정점 수에 따라 변해서, 스크래치 멤버로는
	// 대체할 수 없다 — 멤버로 올리면 FindRoute 가 재진입 불가능해진다.
	//
	// 이런 모양이 std::pmr 이 이기는 자리다. 스택 버퍼 하나에 전부 밀어 넣고, 함수가
	// 끝나면 통째로 버린다(개별 해제 없음). 예전에는 호출마다 힙 할당이 7번이었다.
	alignas(std::max_align_t) std::byte scratchBuffer[kScratchBytes];
	std::pmr::monotonic_buffer_resource scratch(scratchBuffer, sizeof(scratchBuffer));

	// 임시 정점의 간선. 출발 위치는 자기 맵의 모든 마커로, 목적지 맵의 모든 마커는 목적지로 잇는다.
	std::pmr::vector<Edge> startEdges(&scratch);
	auto fromList = nodesByMap_.find(fromMapId);
	if (fromList != nodesByMap_.end())
	{
		for (int index : fromList->second)
			startEdges.push_back(Edge{ index, MeasureWalk(fromMapId, fromPos, nodes_[index].pos), 0 });
	}
	// 같은 맵이면 게이트를 거치지 않고 곧장 걸어가는 선택지도 있다.
	if (fromMapId == toMapId)
		startEdges.push_back(Edge{ goal, MeasureWalk(fromMapId, fromPos, toPos), 0 });

	std::pmr::vector<float> dist(total, kInfinity, &scratch);
	std::pmr::vector<int> prev(total, -1, &scratch);
	std::pmr::vector<int> viaGate(total, 0, &scratch);
	std::pmr::vector<float> edgeCost(total, 0.0f, &scratch);

	using Entry = std::pair<float, int>;
	std::priority_queue<Entry, std::pmr::vector<Entry>, std::greater<Entry>> queue(
		std::greater<Entry>{}, std::pmr::vector<Entry>(&scratch));
	dist[start] = 0.0f;
	queue.push({ 0.0f, start });

	while (!queue.empty())
	{
		const auto [d, u] = queue.top();
		queue.pop();
		if (d > dist[u])
			continue;   // 이미 더 짧은 경로로 확정된 정점.
		if (u == goal)
			break;

		// u 에서 나가는 간선. 임시 출발 정점만 별도 목록을 쓴다.
		// (startEdges 는 스크래치 할당자를 쓰므로 adjacency_ 와 타입이 다르다 — span 으로 받는다)
		const std::span<const Edge> edges = (u == start)
			? std::span<const Edge>(startEdges)
			: std::span<const Edge>(adjacency_[u]);
		for (const Edge& edge : edges)
		{
			if (edge.gateId != 0)
			{
				// 포탈: 레벨 제한과 인스턴스 통과 금지를 여기서 거른다.
				const Node& gateNode = nodes_[nodeByMarker_.at(edge.gateId)];
				if (level > 0 && gateNode.requiredLevel > level)
					continue;
				if (!CanEnter(nodes_[edge.to].mapId, toMapId))
					continue;
			}

			const float next = d + edge.cost;
			if (next >= dist[edge.to])
				continue;

			dist[edge.to] = next;
			prev[edge.to] = u;
			viaGate[edge.to] = edge.gateId;
			edgeCost[edge.to] = edge.cost;
			queue.push({ next, edge.to });
		}

		// 목적지 맵의 마커에 서 있으면 목적지까지 걸어갈 수 있다.
		if (u < nodeCount && nodes_[u].mapId == toMapId)
		{
			const float walk = MeasureWalk(toMapId, nodes_[u].pos, toPos);
			if (d + walk < dist[goal])
			{
				dist[goal] = d + walk;
				prev[goal] = u;
				viaGate[goal] = 0;
				edgeCost[goal] = walk;
				queue.push({ dist[goal], goal });
			}
		}
	}

	if (dist[goal] == kInfinity)
		return route;   // found = false.

	// ── 경로 복원 ──
	std::pmr::vector<int> sequence(&scratch);
	for (int at = goal; at != -1; at = prev[at])
		sequence.push_back(at);
	std::reverse(sequence.begin(), sequence.end());

	route.found = true;
	route.cost = dist[goal];

	Step current;
	current.mapId = fromMapId;
	current.from = fromPos;
	current.to = fromPos;

	for (size_t i = 1; i < sequence.size(); ++i)
	{
		const int v = sequence[i];
		const syncnet::Vec3 pos = (v == goal) ? toPos : nodes_[v].pos;

		if (viaGate[v] != 0)
		{
			// 게이트 통과. 여기서 구간이 끊기고 다음 맵에서 새 구간이 시작된다.
			current.gateId = viaGate[v];
			route.steps.push_back(current);

			current = Step{};
			current.mapId = (v == goal) ? toMapId : nodes_[v].mapId;
			current.from = pos;
			current.to = pos;
			continue;
		}

		current.to = pos;
		current.walkCost += edgeCost[v];
	}
	route.steps.push_back(current);

	return route;
}

bool ZoneGraph::AreConnected(int fromMapId, int toMapId, int level) const
{
	if (fromMapId == toMapId)
		return true;

	auto fromList = nodesByMap_.find(fromMapId);
	if (fromList == nodesByMap_.end())
		return false;

	// 포탈 간선만 따라가는 너비 우선 탐색(도보 비용은 보지 않는다).
	// FindRoute 와 같은 이유로 스크래치에 담는다 — 둘 다 이 함수 안에서만 산다.
	alignas(std::max_align_t) std::byte scratchBuffer[kScratchBytes];
	std::pmr::monotonic_buffer_resource scratch(scratchBuffer, sizeof(scratchBuffer));

	std::pmr::vector<bool> visited(nodes_.size(), false, &scratch);
	std::pmr::vector<int> frontier(fromList->second.begin(), fromList->second.end(), &scratch);
	for (int index : frontier)
		visited[index] = true;

	while (!frontier.empty())
	{
		const int u = frontier.back();
		frontier.pop_back();

		for (const Edge& edge : adjacency_[u])
		{
			if (visited[edge.to])
				continue;

			if (edge.gateId != 0)
			{
				const Node& gateNode = nodes_[nodeByMarker_.at(edge.gateId)];
				if (level > 0 && gateNode.requiredLevel > level)
					continue;
				if (!CanEnter(nodes_[edge.to].mapId, toMapId))
					continue;
			}

			if (nodes_[edge.to].mapId == toMapId)
				return true;

			visited[edge.to] = true;
			frontier.push_back(edge.to);
		}
	}
	return false;
}
