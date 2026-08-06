#include "pch.h"
#include <gtest/gtest.h>
#include <cmath>
#include <filesystem>
#include <memory>
#include <string>
#include <unordered_map>
#include "spdlog/spdlog.h"
#include "spdlog/sinks/stdout_sinks.h"

#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "SyntheticMaps.h"
#include "ZoneGraph.h"
#include "World.h"
#include "Map.h"
#include "NavMesh.h"
#include "Vector3.h"
#include "GameMode.h"
#include "syncnet_generated.h"

//---------------------------------------------------------------------------------------
// 존 그래프 — 여러 navmesh 를 가로지르는 경로 탐색.
//
// navmesh 하나는 맵 하나의 지형만 안다. "A 마을에서 B 던전까지 몇 개 맵을 거쳐 가야
// 가장 가까운가" 는 그 위층에서 풀어야 하는 문제다. 여기서 고정하는 것은:
//   - 그래프가 Map.json 만으로(navmesh 없이) 만들어진다
//   - 게이트의 target_id 가 맵 경계를 넘는 유일한 간선이다
//   - 여러 맵을 거치는 경로가 맵별 구간(Step)으로 쪼개져 나온다
//   - 게이트가 없어 고립된 맵은 경로가 나오지 않는다
//   - 인스턴스 맵(레이드)은 목적지일 때만 지나갈 수 있다
//   - required_level 이 모자란 게이트는 경로에서 빠진다
//   - Map.json 의 gate_links 비용이 계산된 비용을 덮어쓴다
//   - navmesh 가 있으면 도보 비용이 직선 거리가 아니라 실제 이동 거리가 된다
//---------------------------------------------------------------------------------------

namespace
{
	void EnsureNetLogger()
	{
		if (!spdlog::get("net"))
		{
			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::stdout_sink_mt>());
			logger->set_level(spdlog::level::warn);
			spdlog::register_logger(logger);
		}
	}

	syncnet::Vec3 ToVec3(const gamedata::MapGatePosition& p)
	{
		return syncnet::Vec3(
			static_cast<float>(p.x), static_cast<float>(p.y), static_cast<float>(p.z));
	}

	float Straight(const syncnet::Vec3& a, const syncnet::Vec3& b)
	{
		const float dx = a.x() - b.x(), dy = a.y() - b.y(), dz = a.z() - b.z();
		return std::sqrt(dx * dx + dy * dy + dz * dz);
	}

	// 게이트가 없어 어느 맵에서도 걸어 들어갈 수 없는 맵. 없으면 0.
	int FindIsolatedMap(const ZoneGraph& graph)
	{
		for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
		{
			if (map == nullptr || !map->gates.empty())
				continue;

			bool targeted = false;
			for (const ZoneGraph::Node& node : graph.GetNodes())
				targeted |= (node.mapId == map->id);
			if (!targeted)
				return map->id;
		}
		return 0;
	}
}

class ZoneGraphTest : public ::testing::Test
{
protected:
	ZoneGraph graph_;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"))
			<< "통합 GameData 폴더를 찾지 못했습니다: " << dataPath;
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath)) << "LoadResources 실패";

		// navmesh 없이 Map.json 만으로 빌드한다 — 도보 비용은 직선 거리다.
		ASSERT_TRUE(graph_.Build()) << "그래프에 정점이 하나도 없습니다(게이트 없음).";
	}

	// mapId 맵에 있는 아무 게이트. 없으면 nullptr.
	static const gamedata::MapGate* AnyGate(int mapId)
	{
		const gamedata::Map* map = ResourceLoader::Instance().GetMap(mapId);
		return (map != nullptr && !map->gates.empty()) ? &map->gates.front() : nullptr;
	}
};

// 정점은 '모든 게이트' + '게이트가 가리키는 도착 지점' 이다. 맵 안의 일반 좌표는 정점이 아니다.
TEST_F(ZoneGraphTest, NodesAreGatesAndTheirTargets)
{
	size_t gateCount = 0;
	for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
		gateCount += map->gates.size();

	ASSERT_GT(gateCount, 0u) << "게이트가 없어 검증할 수 없습니다.";
	EXPECT_GE(graph_.NodeCount(), gateCount) << "게이트가 전부 정점으로 올라오지 않았습니다.";

	for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
	{
		for (const auto& gate : map->gates)
		{
			const ZoneGraph::Node* node = graph_.FindNode(gate.id);
			ASSERT_NE(node, nullptr) << "게이트 " << gate.id << " 가 정점에 없습니다.";
			EXPECT_TRUE(node->isGate);
			EXPECT_EQ(node->mapId, map->id);

			// 도착 지점도 정점이어야 포탈 간선이 성립한다.
			EXPECT_NE(graph_.FindNode(gate.target_id), nullptr)
				<< "게이트 " << gate.id << " 의 target_id " << gate.target_id << " 가 정점에 없습니다.";
		}
	}
}

// 게이트를 밟고 나면 도착한 맵에서 다시 걸어야 한다 — 경로가 맵별 구간으로 쪼개져 나온다.
TEST_F(ZoneGraphTest, RouteCrossesMapsAsSeparateSteps)
{
	// 서로 다른 맵에 있고 이어져 있는 두 게이트를 찾는다.
	const ZoneGraph::Node* start = nullptr;
	const ZoneGraph::Node* goal = nullptr;
	for (const ZoneGraph::Node& a : graph_.GetNodes())
	{
		for (const ZoneGraph::Node& b : graph_.GetNodes())
		{
			if (a.mapId == b.mapId || !graph_.AreConnected(a.mapId, b.mapId))
				continue;
			start = &a;
			goal = &b;
			break;
		}
		if (start != nullptr)
			break;
	}
	ASSERT_NE(start, nullptr) << "맵을 넘나드는 연결이 데이터에 없습니다.";

	ZoneGraph::Route route = graph_.FindRoute(start->mapId, start->pos, goal->mapId, goal->pos);
	ASSERT_TRUE(route.found) << "map " << start->mapId << " -> " << goal->mapId << " 경로를 찾지 못했습니다.";
	ASSERT_GE(route.steps.size(), 2u) << "다른 맵으로 가는데 구간이 하나뿐입니다.";

	// 첫 구간은 출발 맵에서 시작하고, 마지막 구간은 목적지 맵에서 끝난다.
	EXPECT_EQ(route.steps.front().mapId, start->mapId);
	EXPECT_EQ(route.steps.back().mapId, goal->mapId);
	EXPECT_EQ(route.steps.back().gateId, 0) << "마지막 구간에는 통과할 게이트가 없어야 합니다.";
	EXPECT_EQ(route.TransitionCount(), static_cast<int>(route.steps.size()) - 1);

	// 중간 구간은 전부 '이 맵에서 이 게이트까지 걸어라' 여야 하고,
	// 그 게이트를 통과하면 다음 구간의 맵으로 넘어가야 한다.
	for (size_t i = 0; i + 1 < route.steps.size(); ++i)
	{
		const ZoneGraph::Step& step = route.steps[i];
		ASSERT_NE(step.gateId, 0) << i << "번째 구간에 통과할 게이트가 없습니다.";

		const gamedata::MapGate* gate = ResourceLoader::Instance().GetMapGate(step.gateId);
		ASSERT_NE(gate, nullptr);
		ASSERT_NE(gate->parent, nullptr);
		EXPECT_EQ(gate->parent->id, step.mapId) << "구간의 게이트가 그 맵의 게이트가 아닙니다.";

		const gamedata::Map* dest = nullptr;
		syncnet::Vec3 arrival(0, 0, 0);
		ASSERT_TRUE(Map::ResolveGateTarget(gate->target_id, dest, arrival));
		EXPECT_EQ(dest->id, route.steps[i + 1].mapId) << "게이트 도착 맵과 다음 구간의 맵이 다릅니다.";
	}
}

// 같은 맵 안에서는 게이트를 거치지 않고 곧장 걷는다.
TEST_F(ZoneGraphTest, SameMapRouteHasNoTransition)
{
	const gamedata::MapGate* gate = nullptr;
	int mapId = 0;
	for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
	{
		if (map != nullptr && !map->gates.empty())
		{
			gate = &map->gates.front();
			mapId = map->id;
			break;
		}
	}
	ASSERT_NE(gate, nullptr);

	const syncnet::Vec3 from = ToVec3(gate->position);
	const syncnet::Vec3 to(from.x() + 10.0f, from.y(), from.z() + 10.0f);

	ZoneGraph::Route route = graph_.FindRoute(mapId, from, mapId, to);
	ASSERT_TRUE(route.found);
	EXPECT_EQ(route.steps.size(), 1u) << "같은 맵인데 게이트를 거쳤습니다.";
	EXPECT_EQ(route.TransitionCount(), 0);
	EXPECT_EQ(route.steps.front().gateId, 0);
	EXPECT_NEAR(route.cost, Straight(from, to), 0.01f);
}

// 들어오는 게이트가 없는 맵은 걸어서 갈 수 없다 — 없는 경로를 지어내지 않는다.
TEST_F(ZoneGraphTest, IsolatedMapIsUnreachable)
{
	const int isolated = FindIsolatedMap(graph_);
	if (isolated == 0)
	{
		// 검증할 데이터가 없다 — 모든 맵이 게이트로 이어져 있으면 확인할 것도 없다.
		SUCCEED() << "고립된 맵이 데이터에 없습니다.";
		return;
	}

	const ZoneGraph::Node& start = graph_.GetNodes().front();
	ASSERT_NE(start.mapId, isolated);

	const syncnet::Vec3 target(0, 0, 0);
	ZoneGraph::Route route = graph_.FindRoute(start.mapId, start.pos, isolated, target);
	EXPECT_FALSE(route.found) << "게이트가 없는 맵 " << isolated << " 로 가는 경로가 나왔습니다.";
	EXPECT_FALSE(graph_.AreConnected(start.mapId, isolated));
}

// 인스턴스 맵(레이드)은 입장할 때마다 새로 만들어진다. 반대편으로 빠져나가는 지름길이
// 될 수 없으므로, 목적지가 아닌 이상 경로가 그 맵을 지나가면 안 된다.
TEST_F(ZoneGraphTest, InstanceMapIsNotUsedAsAShortcut)
{
	auto& resource = ResourceLoader::Instance();

	// 경유가 필요한 경로를 하나 잡는다: 인스턴스 맵에서 출발해 다른 field 맵으로 간다.
	const gamedata::Map* instance = nullptr;
	for (const auto& [id, map] : resource.GetMaps())
	{
		if (map == nullptr || map->gates.empty())
			continue;
		const gamedata::GameMode* mode = resource.GetGameMode(map->game_mode_id);
		if (mode != nullptr && mode->type != "field")
		{
			instance = map;
			break;
		}
	}
	if (instance == nullptr)
	{
		SUCCEED() << "게이트를 가진 인스턴스 맵이 데이터에 없습니다.";
		return;
	}

	// 인스턴스에서 나가는 게이트가 닿는 맵(경유 맵).
	const gamedata::Map* transit = nullptr;
	syncnet::Vec3 arrival(0, 0, 0);
	ASSERT_TRUE(Map::ResolveGateTarget(instance->gates.front().target_id, transit, arrival));

	// 경유 맵을 인스턴스 모드로 바꾸면, 그 맵을 지나가는 경로는 끊겨야 한다.
	// (데이터를 잠깐 뒤집어 규칙만 확인하고 곧바로 되돌린다.)
	const gamedata::MapGate& exitGate = instance->gates.front();
	const gamedata::Map* beyond = nullptr;
	for (const auto& gate : transit->gates)
	{
		const gamedata::Map* dest = nullptr;
		syncnet::Vec3 unused(0, 0, 0);
		if (Map::ResolveGateTarget(gate.target_id, dest, unused) && dest->id != instance->id)
		{
			beyond = dest;
			break;
		}
	}
	if (beyond == nullptr)
	{
		SUCCEED() << "경유 맵 너머로 이어지는 게이트가 없습니다.";
		return;
	}

	const syncnet::Vec3 start = ToVec3(exitGate.position);
	const syncnet::Vec3 goal(0, 0, 0);

	ZoneGraph::Route before = graph_.FindRoute(instance->id, start, beyond->id, goal);
	ASSERT_TRUE(before.found) << "인스턴스 밖으로 나가는 경로가 원래부터 없습니다.";

	const int savedMode = transit->game_mode_id;
	const_cast<gamedata::Map*>(transit)->game_mode_id = instance->game_mode_id;
	ZoneGraph blocked;
	ASSERT_TRUE(blocked.Build());
	const_cast<gamedata::Map*>(transit)->game_mode_id = savedMode;

	EXPECT_FALSE(blocked.FindRoute(instance->id, start, beyond->id, goal).found)
		<< "인스턴스가 된 맵 " << transit->id << " 를 지나가는 경로가 나왔습니다.";
}

// 레벨이 모자라면 그 게이트는 없는 것과 같다.
TEST_F(ZoneGraphTest, RequiredLevelBlocksGate)
{
	const gamedata::MapGate* gate = nullptr;
	const gamedata::Map* dest = nullptr;
	syncnet::Vec3 arrival(0, 0, 0);
	for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
	{
		if (map == nullptr || map->gates.empty())
			continue;
		if (Map::ResolveGateTarget(map->gates.front().target_id, dest, arrival) && dest->id != map->id)
		{
			gate = &map->gates.front();
			break;
		}
	}
	ASSERT_NE(gate, nullptr);
	ASSERT_NE(gate->parent, nullptr);

	const syncnet::Vec3 start = ToVec3(gate->position);
	const int fromMap = gate->parent->id;

	// 데이터를 잠깐 뒤집어 제한 레벨을 올린 그래프를 만든다.
	const int savedLevel = gate->required_level;
	const_cast<gamedata::MapGate*>(gate)->required_level = 50;
	ZoneGraph gated;
	ASSERT_TRUE(gated.Build());
	const_cast<gamedata::MapGate*>(gate)->required_level = savedLevel;

	EXPECT_TRUE(gated.FindRoute(fromMap, start, dest->id, arrival, 50).found)
		<< "레벨이 충분한데 게이트가 막혔습니다.";
	EXPECT_FALSE(gated.AreConnected(fromMap, dest->id, 10))
		<< "제한 레벨 50 인 게이트를 레벨 10 이 통과했습니다.";
	EXPECT_TRUE(gated.AreConnected(fromMap, dest->id, 0))
		<< "level 0 은 제한 없음이어야 합니다.";
}

// Map.json 의 gate_links 에 비용이 적혀 있으면 계산값 대신 그 값을 쓴다.
// navmesh 를 로드하지 않는 맵(인스턴스 등)이나 디자이너가 경로를 유도하고 싶을 때 쓴다.
TEST_F(ZoneGraphTest, RecordedGateLinkOverridesComputedCost)
{
	const gamedata::Map* map = nullptr;
	for (const auto& [id, m] : ResourceLoader::Instance().GetMaps())
	{
		if (m != nullptr && !m->gate_links.empty())
		{
			map = m;
			break;
		}
	}
	if (map == nullptr)
	{
		SUCCEED() << "gate_links 가 기록된 맵이 없습니다.";
		return;
	}

	for (const auto& link : map->gate_links)
	{
		float cost = 0.0f;
		ASSERT_TRUE(graph_.GetWalkCost(link.from_id, link.to_id, cost))
			<< "map " << map->id << " 의 " << link.from_id << " -> " << link.to_id << " 도보 간선이 없습니다.";
		EXPECT_NEAR(cost, static_cast<float>(link.cost), 0.01f)
			<< "기록된 비용이 무시됐습니다.";

		// 반대 방향도 같은 통로다.
		float reverse = 0.0f;
		ASSERT_TRUE(graph_.GetWalkCost(link.to_id, link.from_id, reverse));
		EXPECT_NEAR(reverse, cost, 0.01f);
	}
}

//---------------------------------------------------------------------------------------
// 격자 세계 — 정답을 미리 아는 위상에서의 탐색 정확성.
//
// 실제 Map.json 은 맵이 6개뿐이고 두 맵을 잇는 길이 하나씩밖에 없어서, 탐색이 '고를 수'
// 있는 상황 자체가 없다. 최단 경로를 정말 고르는지 확인하려면 갈래길이 여럿이면서
// 정답을 손으로 계산할 수 있는 위상이 필요하다.
//
// W×H 격자에서는 두 맵 사이의 최소 전환 횟수가 맨해튼 거리와 같다(SyntheticMaps.h).
// 여기서 고정하는 것은:
//   - 전환 횟수가 맨해튼 거리와 정확히 일치한다(돌아가지 않는다)
//   - 구간이 실제로 이어져 있다(각 구간의 게이트가 다음 구간의 맵으로 간다)
//   - 같은 이웃으로 가는 문이 여럿이면 그중 싼 문을 고른다
//   - 길을 끊으면 우회하고, 완전히 끊으면 경로가 없다고 답한다
//   - 순환이 있어도 탐색이 끝난다
//---------------------------------------------------------------------------------------

class ZoneGraphGridTest : public ::testing::Test
{
protected:
	void SetUp() override
	{
		EnsureNetLogger();
		// 합성 맵의 game_mode_id 가 field 인지 확인하려면 GameMode 테이블이 필요하다.
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(GameDataPath::Resolve()));
	}

	// 격자 세계를 그래프로 만든다(도보 비용은 직선 거리 — navmesh 가 없는 합성 맵이다).
	static void BuildGrid(ZoneGraph& graph, const synthetic::GridWorld& world)
	{
		ASSERT_TRUE(graph.Build(world.maps, nullptr));
	}
};

// 최소 전환 횟수는 맨해튼 거리다. 한 번이라도 더 돌아가면 최단 경로가 아니다.
TEST_F(ZoneGraphGridTest, TransitionCountEqualsManhattanDistance)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(5, 4);
	ZoneGraph graph;
	BuildGrid(graph, world);

	for (int y = 0; y < world.height; ++y)
	{
		for (int x = 0; x < world.width; ++x)
		{
			ZoneGraph::Route route = graph.FindRoute(
				world.MapIdAt(0, 0), synthetic::GridWorld::Center(),
				world.MapIdAt(x, y), synthetic::GridWorld::Center());

			ASSERT_TRUE(route.found) << "(0,0) -> (" << x << "," << y << ") 경로가 없습니다.";
			EXPECT_EQ(route.TransitionCount(), synthetic::GridWorld::ManhattanDistance(0, 0, x, y))
				<< "(0,0) -> (" << x << "," << y << ") 가 최단이 아닙니다.";
		}
	}
}

// 구간이 실제로 이어져 있는지 — 각 구간의 게이트를 밟으면 다음 구간의 맵이 나와야 한다.
// 전환 횟수만 맞고 구간이 어긋나면 클라는 엉뚱한 곳으로 걸어간다.
TEST_F(ZoneGraphGridTest, StepsFormAnUnbrokenChain)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(4, 4);
	ZoneGraph graph;
	BuildGrid(graph, world);

	ZoneGraph::Route route = graph.FindRoute(
		world.MapIdAt(0, 0), synthetic::GridWorld::Center(),
		world.MapIdAt(3, 3), synthetic::GridWorld::Center());
	ASSERT_TRUE(route.found);
	ASSERT_EQ(route.steps.size(), 7u);   // 전환 6번 + 마지막 구간.

	EXPECT_EQ(route.steps.front().mapId, world.MapIdAt(0, 0));
	EXPECT_EQ(route.steps.back().mapId, world.MapIdAt(3, 3));
	EXPECT_EQ(route.steps.back().gateId, 0);

	for (size_t i = 0; i + 1 < route.steps.size(); ++i)
	{
		const ZoneGraph::Node* gate = graph.FindNode(route.steps[i].gateId);
		ASSERT_NE(gate, nullptr) << i << "번째 구간의 게이트가 그래프에 없습니다.";
		EXPECT_EQ(gate->mapId, route.steps[i].mapId) << "구간의 게이트가 그 맵의 것이 아닙니다.";

		// 게이트에 서 있는 위치가 곧 구간의 도착점이어야 한다.
		EXPECT_FLOAT_EQ(route.steps[i].to.x(), gate->pos.x());
		EXPECT_FLOAT_EQ(route.steps[i].to.z(), gate->pos.z());
	}
}

// 같은 이웃으로 가는 문이 여럿이면 지금 서 있는 자리에서 가까운 문을 골라야 한다.
TEST_F(ZoneGraphGridTest, PicksTheCheaperOfParallelGates)
{
	// 이웃 한 쌍마다 문 3개. 문은 변을 따라 흩어져 있다.
	synthetic::GridWorld world = synthetic::MakeGridWorld(2, 1, 3);
	ZoneGraph graph;
	BuildGrid(graph, world);

	const int fromMap = world.MapIdAt(0, 0);
	const int toMap = world.MapIdAt(1, 0);

	// 출발 위치를 변을 따라 옮기면, 그때그때 가장 가까운 문이 선택돼야 한다.
	std::vector<int> chosen;
	for (float z : { -15.0f, 0.0f, 15.0f })
	{
		ZoneGraph::Route route = graph.FindRoute(
			fromMap, syncnet::Vec3(0.0f, 0.0f, z), toMap, synthetic::GridWorld::Center());
		ASSERT_TRUE(route.found);
		ASSERT_EQ(route.steps.size(), 2u);

		const ZoneGraph::Node* gate = graph.FindNode(route.steps.front().gateId);
		ASSERT_NE(gate, nullptr);
		// 고른 문이 정말 가장 가까운 문인지 직접 확인한다.
		for (const ZoneGraph::Node& other : graph.GetNodes())
		{
			if (other.mapId != fromMap)
				continue;
			const float pickedDist = std::abs(gate->pos.z() - z);
			const float otherDist = std::abs(other.pos.z() - z);
			EXPECT_LE(pickedDist, otherDist + 0.01f)
				<< "z=" << z << " 에서 더 가까운 문 " << other.markerId << " 를 두고 "
				<< gate->markerId << " 를 골랐습니다.";
		}
		chosen.push_back(gate->markerId);
	}

	// 자리를 옮겼는데 늘 같은 문이면 '가까운 문 고르기'가 동작하지 않는 것이다.
	EXPECT_NE(chosen.front(), chosen.back());
}

// 길을 끊으면 우회한다. 격자에서 한 칸을 들어내도 둘러 갈 길이 남는다.
TEST_F(ZoneGraphGridTest, RoutesAroundARemovedMap)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(3, 3);

	// 가운데 맵(1,1)을 빼고 그래프를 만든다 — 그 맵을 지나던 문들은 target 을 잃는다.
	std::vector<const gamedata::Map*> without;
	for (const gamedata::Map* map : world.maps)
	{
		if (map->id != world.MapIdAt(1, 1))
			without.push_back(map);
	}

	ZoneGraph graph;
	ASSERT_TRUE(graph.Build(without, nullptr));

	ZoneGraph::Route route = graph.FindRoute(
		world.MapIdAt(0, 1), synthetic::GridWorld::Center(),
		world.MapIdAt(2, 1), synthetic::GridWorld::Center());

	ASSERT_TRUE(route.found) << "가운데를 뺐다고 우회로까지 사라지면 안 됩니다.";
	// 직선으로 2번이면 되던 것이 위나 아래로 돌아 4번이 된다.
	EXPECT_EQ(route.TransitionCount(), 4);
}

// 한 줄짜리 세계에서 가운데를 들어내면 정말로 길이 없다 — 없는 길을 지어내지 않는다.
TEST_F(ZoneGraphGridTest, DisconnectedHalvesHaveNoRoute)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(3, 1);

	std::vector<const gamedata::Map*> without;
	for (const gamedata::Map* map : world.maps)
	{
		if (map->id != world.MapIdAt(1, 0))
			without.push_back(map);
	}

	ZoneGraph graph;
	ASSERT_TRUE(graph.Build(without, nullptr));

	EXPECT_FALSE(graph.FindRoute(
		world.MapIdAt(0, 0), synthetic::GridWorld::Center(),
		world.MapIdAt(2, 0), synthetic::GridWorld::Center()).found);
	EXPECT_FALSE(graph.AreConnected(world.MapIdAt(0, 0), world.MapIdAt(2, 0)));
}

// 격자는 순환투성이다(사방으로 돌아 제자리). 탐색이 순환에 갇히지 않아야 한다.
TEST_F(ZoneGraphGridTest, CyclesDoNotTrapTheSearch)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(4, 4);
	ZoneGraph graph;
	BuildGrid(graph, world);

	// 제자리로 돌아오는 경로: 같은 맵이므로 게이트를 거치지 않는다.
	ZoneGraph::Route self = graph.FindRoute(
		world.MapIdAt(2, 2), synthetic::GridWorld::Center(),
		world.MapIdAt(2, 2), synthetic::GridWorld::Center());
	ASSERT_TRUE(self.found);
	EXPECT_EQ(self.TransitionCount(), 0);
	EXPECT_NEAR(self.cost, 0.0f, 0.01f);

	// 모든 칸 쌍이 연결돼 있어야 한다(격자는 완전 연결이다).
	for (int y = 0; y < world.height; ++y)
		for (int x = 0; x < world.width; ++x)
			EXPECT_TRUE(graph.AreConnected(world.MapIdAt(0, 0), world.MapIdAt(x, y)));
}

// 그래프에 없는 맵에서 출발하면 경로가 없다(빈 그래프도 마찬가지).
TEST_F(ZoneGraphGridTest, UnknownMapYieldsNoRoute)
{
	synthetic::GridWorld world = synthetic::MakeGridWorld(2, 2);
	ZoneGraph graph;
	BuildGrid(graph, world);

	EXPECT_FALSE(graph.FindRoute(
		-1, synthetic::GridWorld::Center(),
		world.MapIdAt(1, 1), synthetic::GridWorld::Center()).found);

	ZoneGraph empty;
	EXPECT_FALSE(empty.Build({}, nullptr)) << "정점이 없으면 빌드가 실패로 보고돼야 합니다.";
	EXPECT_EQ(empty.NodeCount(), 0u);
	EXPECT_FALSE(empty.FindRoute(1, synthetic::GridWorld::Center(),
		2, synthetic::GridWorld::Center()).found);
}

// 정점/간선 수가 위상에서 예상되는 값과 맞는지 — 간선이 새거나 빠지면 여기서 걸린다.
TEST_F(ZoneGraphGridTest, GraphSizeMatchesTopology)
{
	constexpr int kWidth = 4, kHeight = 3, kLanes = 2;
	synthetic::GridWorld world = synthetic::MakeGridWorld(kWidth, kHeight, kLanes);
	ZoneGraph graph;
	BuildGrid(graph, world);

	// 정점 = 게이트 수(합성 맵에는 스폰 지점이 없다).
	EXPECT_EQ(graph.NodeCount(), world.GateCount());

	// 간선 = 포탈(게이트마다 1개, 단방향) + 도보(맵마다 게이트 수의 조합 × 2방향).
	size_t expected = world.GateCount();
	for (const gamedata::Map* map : world.maps)
	{
		const size_t n = map->gates.size();
		expected += n * (n - 1);   // 양방향이라 조합 × 2.
	}
	EXPECT_EQ(graph.EdgeCount(), expected);

	// navmesh 를 안 붙였으니 도보 비용은 전부 직선 거리 추정이다.
	EXPECT_EQ(graph.EstimatedEdgeCount(), static_cast<int>((expected - world.GateCount()) / 2));
}

//---------------------------------------------------------------------------------------
// navmesh 를 붙였을 때. 도보 비용이 직선 거리가 아니라 실제 이동 거리가 된다.
//---------------------------------------------------------------------------------------

class ZoneGraphNavMeshTest : public ::testing::Test
{
protected:
	std::unique_ptr<World> world_;

	void SetUp() override
	{
		EnsureNetLogger();

		const std::string& dataPath = GameDataPath::Resolve();
		ASSERT_TRUE(std::filesystem::exists(dataPath + "Map.json"));
		ASSERT_TRUE(ResourceLoader::Instance().LoadResources(dataPath));
		GameMode::InitializeLua(dataPath);

		world_ = std::make_unique<World>();
		world_->Init("waypoint");
	}

	void TearDown() override { world_.reset(); }
};

// navmesh 위의 이동 거리는 직선 거리보다 짧을 수 없다. 짧게 나오면 좌표계 변환(클라 x 반전)
// 이나 경로 길이 합산이 틀린 것이다.
//
// 비교 대상은 반드시 navmesh 위에 있는 것이 확인된 지점이어야 한다 — findNearestPoly 가
// 메시 밖 좌표를 가까운 폴리곤으로 끌어당기므로, 아무 좌표나 넣으면 '스냅되며 짧아진 거리'와
// '스냅 전 직선 거리'를 비교하게 된다. 그래서 마커(게이트/스폰)끼리만 잰다
// (마커가 메시 위에 있는지는 Map::ValidateMapDataOnNavMesh 가 로드 시 검증한다).
TEST_F(ZoneGraphNavMeshTest, MeasuredCostIsNeverShorterThanStraightLine)
{
	const ZoneGraph& graph = world_->GetZoneGraph();
	ASSERT_GT(graph.NodeCount(), 0u);

	// 로드된 맵별 마커 목록.
	std::unordered_map<int, std::vector<const ZoneGraph::Node*>> byMap;
	for (const ZoneGraph::Node& node : graph.GetNodes())
	{
		if (world_->FindMap(node.mapId) != nullptr)
			byMap[node.mapId].push_back(&node);
	}

	int compared = 0;
	for (const auto& [mapId, markers] : byMap)
	{
		const NavMesh* nav = world_->FindMap(mapId)->GetNavMesh();
		ASSERT_NE(nav, nullptr);

		for (size_t i = 0; i < markers.size(); ++i)
		{
			for (size_t j = i + 1; j < markers.size(); ++j)
			{
				const syncnet::Vec3& a = markers[i]->pos;
				const syncnet::Vec3& b = markers[j]->pos;
				const float from[3] = { Vector3::convert_x(a.x()), a.y(), a.z() };
				const float to[3] = { Vector3::convert_x(b.x()), b.y(), b.z() };

				float measured = 0.0f;
				ASSERT_TRUE(nav->PathLength(from, to, measured))
					<< "map " << mapId << " 의 마커 " << markers[i]->markerId
					<< " -> " << markers[j]->markerId << " 사이에 navmesh 경로가 없습니다.";

				// 마커가 메시 표면보다 살짝 뜨거나 잠겨 있을 수 있어 여유를 둔다.
				EXPECT_GE(measured, Straight(a, b) - 0.5f)
					<< "map " << mapId << " 마커 " << markers[i]->markerId
					<< " -> " << markers[j]->markerId << " 의 실측 거리가 직선 거리보다 짧습니다.";
				++compared;
			}
		}
	}
	EXPECT_GT(compared, 0) << "같은 맵에 마커가 둘 이상인 로드된 맵이 없습니다.";
}

// Map.json 의 gate_links 는 실측값을 덮어쓴다. 지형이나 게이트 위치가 바뀌면 그 값이
// 조용히 낡아 라우팅이 어긋나므로, 로드된 맵에 한해 실측값과 맞는지 확인한다.
TEST_F(ZoneGraphNavMeshTest, RecordedGateLinkMatchesMeasuredCost)
{
	int checked = 0;
	for (const auto& [id, map] : ResourceLoader::Instance().GetMaps())
	{
		if (map == nullptr || map->gate_links.empty())
			continue;

		Map* loaded = world_->FindMap(map->id);
		if (loaded == nullptr)
			continue;   // 로드되지 않은 맵은 잴 수가 없다(gate_links 를 적는 이유이기도 하다).

		const ZoneGraph& graph = world_->GetZoneGraph();
		for (const auto& link : map->gate_links)
		{
			const ZoneGraph::Node* from = graph.FindNode(link.from_id);
			const ZoneGraph::Node* to = graph.FindNode(link.to_id);
			ASSERT_NE(from, nullptr);
			ASSERT_NE(to, nullptr);

			const float start[3] = { Vector3::convert_x(from->pos.x()), from->pos.y(), from->pos.z() };
			const float end[3] = { Vector3::convert_x(to->pos.x()), to->pos.y(), to->pos.z() };

			float measured = 0.0f;
			ASSERT_TRUE(loaded->GetNavMesh()->PathLength(start, end, measured));

			EXPECT_NEAR(static_cast<float>(link.cost), measured, measured * 0.1f + 0.5f)
				<< "map " << map->id << " gate_links " << link.from_id << " -> " << link.to_id
				<< " 에 적힌 비용 " << link.cost << " 가 navmesh 실측값 " << measured
				<< " 와 다릅니다. 지형이나 게이트 위치가 바뀌었는지 확인하세요.";
			++checked;
		}
	}
	EXPECT_GT(checked, 0) << "로드된 맵에 gate_links 가 없습니다.";
}

// 실측을 못 한 간선이 남아 있으면 라우팅이 그만큼 부정확하다. 그런 간선이 왜 생겼는지
// (맵 미로드 / 경로 단절) 알 수 있도록 수치를 노출한다.
TEST_F(ZoneGraphNavMeshTest, LoadedMapsHaveMeasuredEdges)
{
	const ZoneGraph& graph = world_->GetZoneGraph();
	graph.LogSummary();

	// 인스턴스 맵은 로드돼 있지 않으므로 그 맵의 간선은 직선 거리로 남는다. 그 외에는 없어야 한다.
	std::unordered_map<int, int> markersInUnloadedMap;
	for (const ZoneGraph::Node& node : graph.GetNodes())
	{
		if (world_->FindMap(node.mapId) == nullptr)
			++markersInUnloadedMap[node.mapId];
	}

	int unloadedMapEdges = 0;
	for (const auto& [mapId, count] : markersInUnloadedMap)
		unloadedMapEdges += count * (count - 1) / 2;

	EXPECT_LE(graph.EstimatedEdgeCount(), unloadedMapEdges)
		<< "로드된 맵인데도 도보 비용을 실측하지 못한 간선이 있습니다(LogSummary 참고).";
}

// 여러 맵을 거치는 실제 경로. 각 구간의 비용이 그 맵 navmesh 로 실측된 값이어야 한다.
TEST_F(ZoneGraphNavMeshTest, MultiMapRouteAccumulatesPerMapWalkCost)
{
	const ZoneGraph& graph = world_->GetZoneGraph();

	const ZoneGraph::Node* start = nullptr;
	const ZoneGraph::Node* goal = nullptr;
	for (const ZoneGraph::Node& a : graph.GetNodes())
	{
		for (const ZoneGraph::Node& b : graph.GetNodes())
		{
			if (a.mapId == b.mapId || !graph.AreConnected(a.mapId, b.mapId))
				continue;
			start = &a;
			goal = &b;
			break;
		}
		if (start != nullptr)
			break;
	}
	ASSERT_NE(start, nullptr);

	ZoneGraph::Route route = graph.FindRoute(start->mapId, start->pos, goal->mapId, goal->pos);
	ASSERT_TRUE(route.found);

	float sum = 0.0f;
	for (const ZoneGraph::Step& step : route.steps)
	{
		EXPECT_GE(step.walkCost, 0.0f);
		sum += step.walkCost;
	}
	// 게이트 통과는 비용이 0 이므로 전체 비용은 구간 도보 비용의 합이다.
	EXPECT_NEAR(route.cost, sum, 0.1f);
}
