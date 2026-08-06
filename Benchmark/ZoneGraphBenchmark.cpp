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
// 실제 데이터 + 실제 navmesh 로 재는 BM_ZoneGraphBuildRealData 만 World 를 띄운다.
// 이것이 기동 시 실제로 지불하는 값이고, 합성 벤치는 그 값이 규모에 따라 어떻게
// 늘어날지를 보여 준다.
//
// 의미 있는 수치를 위해 Release/x64 로 빌드/실행할 것.

#include <benchmark/benchmark.h>

#include <memory>
#include <string>
#include <vector>

#include "ZoneGraph.h"
#include "World.h"
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
	EnsureResources();

	// World 생성은 navmesh 로드까지 하는 무거운 작업이라 한 번만 만들고 재사용한다.
	static std::unique_ptr<World> world = [] {
		GameMode::InitializeLua(GameDataPath::Resolve());
		auto w = std::make_unique<World>();
		w->Init("waypoint");
		return w;
	}();

	for (auto _ : state)
	{
		world->RebuildZoneGraph();
		benchmark::DoNotOptimize(world->GetZoneGraph().NodeCount());
	}

	state.counters["nodes"] = static_cast<double>(world->GetZoneGraph().NodeCount());
	state.counters["edges"] = static_cast<double>(world->GetZoneGraph().EdgeCount());
}
BENCHMARK(BM_ZoneGraphBuildRealData)->Unit(benchmark::kMicrosecond);

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

// 경로가 아예 없는 경우. 다익스트라가 도달 가능한 정점을 전부 훑고 끝나므로
// 최악의 탐색 비용이다(실패를 반복 호출하면 이 값을 계속 문다).
static void BM_ZoneGraphFindRouteUnreachable(benchmark::State& state)
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
		// 그래프에 없는 맵 id — 어디로도 닿지 않는다.
		ZoneGraph::Route route = graph.FindRoute(fromMap, center, -1, center);
		benchmark::DoNotOptimize(route.found);
	}
	state.counters["nodes"] = static_cast<double>(graph.NodeCount());
}
BENCHMARK(BM_ZoneGraphFindRouteUnreachable)
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
