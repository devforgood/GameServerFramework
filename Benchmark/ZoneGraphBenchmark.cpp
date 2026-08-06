// 존 그래프(여러 navmesh 를 가로지르는 경로 탐색) 벤치마크.
//
// 재는 것은 두 가지 성격이 다른 비용이다.
//   - 빌드: 서버 기동 시 1회. 맵마다 게이트끼리 도보 간선을 만들며, 비용을 navmesh 로
//           재면 간선마다 Detour 쿼리가 한 번씩 들어간다. O(맵 × 게이트²) 이라
//           게이트가 늘 때 어떻게 휘는지가 핵심이다.
//   - 탐색: 요청마다. 다익스트라 한 번. 여기가 비싸면 플레이어마다 부를 수 없다.
//
// 실제 Map.json 은 맵 6개 / 정점 7개라 측정에 쓸 수 없다. 그래서 W×H 격자 세계를
// 합성해 규모를 바꿔 가며 잰다(UnitTest/SyntheticMaps.h — 단위 테스트가 정확성을
// 검증하는 것과 같은 위상이라, 측정치와 검증이 같은 대상을 가리킨다).
//
// 합성 벤치는 도보 비용을 직선 거리로 계산하므로 Detour 비용이 빠져 있다. 그 몫은
// BM_NavMeshPathLength 가 따로 재고(간선 1개당), 실제 빌드 비용은 둘을 합쳐 어림한다.
// BM_ZoneGraphBuildRealData 는 현재 데이터로 실제 지불하는 값이다.
//
// 의미 있는 수치를 위해 Release/x64 로 빌드/실행할 것.

#include <benchmark/benchmark.h>

#include <array>
#include <memory>
#include <string>
#include <vector>

#include "ZoneGraph.h"
#include "World.h"
#include "Map.h"
#include "NavMesh.h"
#include "Vector3.h"
#include "GameMode.h"
#include "ResourceLoader.h"
#include "LogHelper.h"
#include "syncnet_generated.h"
#include "spdlog/spdlog.h"

// 단위 테스트와 같은 합성 위상을 쓴다. 측정 대상과 검증 대상이 갈라지지 않도록
// 생성기를 복사하지 않고 그대로 포함한다(WorldBenchmark 가 ../BehaviorTree 를 참조하는 것과 같은 방식).
#include "../UnitTest/SyntheticMaps.h"

namespace
{
	// LOG 매크로는 "net" 로거를 역참조한다. 다른 벤치 TU 가 먼저 등록했을 수도 있으므로
	// 없을 때만 만든다. 그래프 빌드가 경고를 찍을 수 있어 반드시 있어야 한다.
	void EnsureLogger()
	{
		if (spdlog::get("net"))
			return;
		InitLog();
		if (auto net = spdlog::get("net"))
			net->set_level(spdlog::level::off);   // 측정 중 로그 출력은 왜곡 요인이다.
	}

	// 합성 맵의 game_mode_id 가 field 인지 확인하려면 GameMode 테이블이 필요하다.
	void EnsureResources()
	{
		EnsureLogger();
		if (ResourceLoader::Instance().GetGameModes().empty())
			ResourceLoader::Instance().LoadResources(GameDataPath::Resolve());
	}

	// 실제 데이터로 띄운 월드. 생성이 navmesh 로드까지 하는 무거운 작업이라
	// 이 TU 의 벤치들이 하나를 나눠 쓴다.
	World& RealWorld()
	{
		static std::unique_ptr<World> world = [] {
			EnsureResources();
			GameMode::InitializeLua(GameDataPath::Resolve());
			auto w = std::make_unique<World>();
			w->Init("waypoint");
			return w;
		}();
		return *world;
	}
}

//---------------------------------------------------------------------------------------
// 빌드 — 서버 기동 시 1회
//---------------------------------------------------------------------------------------

// 격자 한 변(range 0) × 이웃당 문 개수(range 1).
// 맵 수 = 변², 맵당 게이트 = 최대 4×문개수, 도보 간선 = 맵마다 게이트 수의 조합.
static void BM_ZoneGraphBuild(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));
	const int lanes = static_cast<int>(state.range(1));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side, lanes);

	ZoneGraph graph;
	for (auto _ : state)
	{
		graph.Build(world.maps, nullptr);
		benchmark::DoNotOptimize(graph.NodeCount());
	}

	state.counters["maps"] = static_cast<double>(world.maps.size());
	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
	state.counters["edges"] = static_cast<double>(graph.EdgeCount());
}
BENCHMARK(BM_ZoneGraphBuild)
	->Args({ 4, 1 })->Args({ 8, 1 })->Args({ 16, 1 })->Args({ 32, 1 })
	->Args({ 8, 2 })->Args({ 8, 4 })->Args({ 8, 8 })
	->Unit(benchmark::kMicrosecond);

// 실제 데이터 + 실제 navmesh. 기동 시 실제로 지불하는 값이다.
// 도보 간선마다 Detour findPath 가 한 번씩 들어가므로 합성 빌드보다 간선당 훨씬 비싸다.
static void BM_ZoneGraphBuildRealData(benchmark::State& state)
{
	World& world = RealWorld();

	for (auto _ : state)
	{
		world.RebuildZoneGraph();
		benchmark::DoNotOptimize(world.GetZoneGraph().NodeCount());
	}

	state.counters["nodes"] = static_cast<double>(world.GetZoneGraph().NodeCount());
	state.counters["edges"] = static_cast<double>(world.GetZoneGraph().EdgeCount());
	// navmesh 로 실측한 간선 = 전체 도보 간선 - 직선 거리로 대체한 것.
	state.counters["estimated_edges"] = static_cast<double>(world.GetZoneGraph().EstimatedEdgeCount());
}
BENCHMARK(BM_ZoneGraphBuildRealData)->Unit(benchmark::kMicrosecond);

// 도보 간선 하나의 비용을 navmesh 로 재는 값(Detour findPath + findStraightPath).
// 위 두 벤치는 직선 거리로만 재기 때문에 이 비용이 빠져 있다 — 실제 빌드 비용은
// '합성 빌드 시간 + 실측 간선 수 × 이 값' 으로 어림해야 한다.
//
// 지금 Map.json 에는 navmesh 로 재는 간선이 하나도 없다(맵 안에 게이트가 둘 이상인 곳이
// 맵 2 뿐이고 그건 gate_links 에 적혀 있다). 그래서 이 값이 실제로 물리는 순간은
// '맵 하나에 문이 여러 개 생길 때' 이고, 그때 얼마를 물게 되는지를 미리 재 둔다.
static void BM_NavMeshPathLength(benchmark::State& state, int mapId)
{
	Map* map = RealWorld().FindMap(mapId);
	if (map == nullptr || map->GetNavMesh() == nullptr)
	{
		state.SkipWithError("맵이 로드되지 않았습니다");
		return;
	}

	// navmesh 위에 있는 것이 검증된 지점들(스폰 마커)을 표본으로 쓴다.
	const gamedata::Map* data = ResourceLoader::Instance().GetMap(mapId);
	std::vector<std::array<float, 3>> points;
	for (const auto& spawn : data->spawn_points.monster_spawn)
	{
		points.push_back({ Vector3::convert_x(static_cast<float>(spawn.position.x)),
			static_cast<float>(spawn.position.y), static_cast<float>(spawn.position.z) });
	}
	if (points.size() < 2)
	{
		state.SkipWithError("표본 지점이 부족합니다");
		return;
	}

	// 가장 멀리 떨어진 쌍(첫 점 ↔ 마지막 점)에서 시작해 표본을 한 칸씩 돌린다.
	// 같은 쌍만 반복하면 Detour 내부 캐시 효과로 실제보다 싸게 나온다.
	size_t index = 0;
	float length = 0.0f;
	for (auto _ : state)
	{
		const auto& a = points[index % points.size()];
		const auto& b = points[(index + points.size() / 2) % points.size()];
		++index;
		map->GetNavMesh()->PathLength(a.data(), b.data(), length);
		benchmark::DoNotOptimize(length);
	}
	state.counters["samples"] = static_cast<double>(points.size());
}
// 맵 1 = Starting Village(작은 맵), 맵 15 = Arcadia Plains(500x500).
BENCHMARK_CAPTURE(BM_NavMeshPathLength, small_map, 1)->Unit(benchmark::kMicrosecond);
BENCHMARK_CAPTURE(BM_NavMeshPathLength, large_map, 15)->Unit(benchmark::kMicrosecond);

//---------------------------------------------------------------------------------------
// 탐색 — 요청마다
//---------------------------------------------------------------------------------------

// 격자 대각선 끝에서 끝까지. 전환 횟수가 2×(변-1) 이라 경로가 가장 긴 경우다.
static void BM_ZoneGraphFindRouteDiagonal(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side);
	ZoneGraph graph;
	graph.Build(world.maps, nullptr);

	const int fromMap = world.MapIdAt(0, 0);
	const int toMap = world.MapIdAt(side - 1, side - 1);
	const syncnet::Vec3 center = synthetic::GridWorld::Center();

	int transitions = 0;
	for (auto _ : state)
	{
		ZoneGraph::Route route = graph.FindRoute(fromMap, center, toMap, center);
		transitions = route.TransitionCount();
		benchmark::DoNotOptimize(route.cost);
	}

	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
	state.counters["transitions"] = static_cast<double>(transitions);
}
BENCHMARK(BM_ZoneGraphFindRouteDiagonal)
	->Arg(4)->Arg(8)->Arg(16)->Arg(32)
	->Unit(benchmark::kMicrosecond);

// 같은 그래프에서 바로 옆 맵으로 가는 짧은 경로. 다익스트라는 목적지를 확정하면 멈추므로
// 대각선보다 훨씬 싸야 한다 — 비슷하게 나오면 조기 종료가 동작하지 않는 것이다.
static void BM_ZoneGraphFindRouteNeighbor(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side);
	ZoneGraph graph;
	graph.Build(world.maps, nullptr);

	const int fromMap = world.MapIdAt(0, 0);
	const int toMap = world.MapIdAt(1, 0);
	const syncnet::Vec3 center = synthetic::GridWorld::Center();

	for (auto _ : state)
	{
		ZoneGraph::Route route = graph.FindRoute(fromMap, center, toMap, center);
		benchmark::DoNotOptimize(route.cost);
	}
	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
}
BENCHMARK(BM_ZoneGraphFindRouteNeighbor)
	->Arg(4)->Arg(8)->Arg(16)->Arg(32)
	->Unit(benchmark::kMicrosecond);

// 목적지 맵이 그래프에 아예 없는 경우. 탐색을 시작하기 전에 걸러지므로 상수 시간이어야 한다
// — 클라가 보낸 맵 id 로 조회하는 자리라면 여기가 막혀 있어야 부하가 안 된다.
static void BM_ZoneGraphFindRouteUnknownMap(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side);
	ZoneGraph graph;
	graph.Build(world.maps, nullptr);

	const syncnet::Vec3 center = synthetic::GridWorld::Center();
	const int fromMap = world.MapIdAt(0, 0);

	for (auto _ : state)
	{
		ZoneGraph::Route route = graph.FindRoute(fromMap, center, -1, center);
		benchmark::DoNotOptimize(route.found);
	}
	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
}
BENCHMARK(BM_ZoneGraphFindRouteUnknownMap)
	->Arg(8)->Arg(16)->Arg(32)
	->Unit(benchmark::kMicrosecond);

// 맵은 있는데 길이 끊긴 경우. 이건 걸러낼 방법이 없다 — 도달 가능한 정점을 전부 훑고
// 나서야 '없다' 고 답할 수 있으므로 탐색의 최악 비용이다.
// 세계를 가운데 열에서 반으로 갈라 왼쪽에서 오른쪽으로 물어본다.
static void BM_ZoneGraphFindRouteDisconnected(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side);

	// 가운데 열을 통째로 들어내면 좌우가 완전히 끊긴다.
	const int cut = side / 2;
	std::vector<const gamedata::Map*> split;
	for (int y = 0; y < side; ++y)
		for (int x = 0; x < side; ++x)
			if (x != cut)
				split.push_back(world.maps[y * side + x]);

	ZoneGraph graph;
	graph.Build(split, nullptr);

	const syncnet::Vec3 center = synthetic::GridWorld::Center();
	const int fromMap = world.MapIdAt(0, 0);
	const int toMap = world.MapIdAt(side - 1, side - 1);

	for (auto _ : state)
	{
		ZoneGraph::Route route = graph.FindRoute(fromMap, center, toMap, center);
		benchmark::DoNotOptimize(route.found);
	}
	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
}
BENCHMARK(BM_ZoneGraphFindRouteDisconnected)
	->Arg(8)->Arg(16)->Arg(32)
	->Unit(benchmark::kMicrosecond);

// 연결 여부만 묻는 BFS. 비용을 보지 않으므로 FindRoute 보다 싸야 한다
// (도달 가능 여부만 알면 되는 곳에서 FindRoute 를 부를 이유가 없다는 근거).
static void BM_ZoneGraphAreConnected(benchmark::State& state)
{
	EnsureResources();
	const int side = static_cast<int>(state.range(0));

	synthetic::GridWorld world = synthetic::MakeGridWorld(side, side);
	ZoneGraph graph;
	graph.Build(world.maps, nullptr);

	const int fromMap = world.MapIdAt(0, 0);
	const int toMap = world.MapIdAt(side - 1, side - 1);

	for (auto _ : state)
		benchmark::DoNotOptimize(graph.AreConnected(fromMap, toMap));

	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
}
BENCHMARK(BM_ZoneGraphAreConnected)
	->Arg(8)->Arg(16)->Arg(32)
	->Unit(benchmark::kMicrosecond);
