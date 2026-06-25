// World/Monster 벤치마크.
//
// 실제 엔진 스택(Recast/Detour 크라우드 + BehaviorTree.CPP + Lua + ECS)을 그대로
// 사용해 다음을 측정한다.
//   - BM_WorldCreate        : 월드 1개 생성 비용(네비메시 로드 + lua 초기화 + 게임모드 부트스트랩)
//   - BM_WorldSpawnMonsters : 몬스터 N마리 스폰 처리량
//   - BM_WorldTick          : N마리가 살아있는 상태에서 update(dt) 1회 소요 시간
//
// 실행 시 작업 디렉터리(또는 exe 디렉터리)에 GameData/ 자산이 있어야 한다
// (solo_navmesh.bin, Monster.xml, *.lua, *.json). PostBuildEvent 가 exe 옆으로 복사한다.
//
// 주의: 스폰 수는 네비메시 크라우드 정원 CrowdNavMovement::MAX_AGENTS 미만이어야 한다(현재 16384).
// 의미 있는 수치를 위해 Release/x64 로 빌드/실행할 것.

#include <benchmark/benchmark.h>

#include <array>
#include <chrono>
#include <memory>
#include <string>
#include <unordered_map>
#include <vector>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "World.h"
#include "Map.h"
#include "Actor.h"
#include "syncnet_generated.h"
#include "LogHelper.h"
#include "spdlog/spdlog.h"

namespace
{
	// 서버 틱 간격(약 30Hz).
	constexpr float kTickDt = 1.0f / 30.0f;

	// 프로그램 시작 시 1회: 로그 초기화 + 리소스 로드.
	// 자산("mob.lua", "GameData/...")은 실행 시 작업 디렉터리 기준 상대 경로로 로드된다.
	// 실행 방식(VS 디버거/더블클릭/터미널)에 따라 작업 디렉터리가 달라지므로, exe 가 있는
	// 폴더(=PostBuild 가 자산을 복사한 출력 폴더)로 작업 디렉터리를 고정한다.
	void ChdirToExeDir()
	{
		wchar_t path[MAX_PATH];
		DWORD len = GetModuleFileNameW(nullptr, path, MAX_PATH);
		if (len == 0 || len == MAX_PATH)
			return;
		// 마지막 경로 구분자에서 잘라 디렉터리만 남긴다.
		for (DWORD i = len; i > 0; --i)
		{
			if (path[i - 1] == L'\\' || path[i - 1] == L'/')
			{
				path[i - 1] = L'\0';
				SetCurrentDirectoryW(path);
				return;
			}
		}
	}

	struct GlobalSetup
	{
		GlobalSetup()
		{
			ChdirToExeDir();
			// LOG 매크로는 "net" 로거를 역참조하므로 반드시 먼저 등록해야 한다.
			InitLog();
			// 스폰/틱마다 LOG 출력이 쏟아져 측정을 왜곡하므로 끈다.
			if (auto net = spdlog::get("net"))
				net->set_level(spdlog::level::off);
			// 주: 리소스(ResourceLoader)는 일부러 로드하지 않는다. 그러면 GameModeFactory 가
			// 게임모드를 만들지 않아(=null) 벤치마크가 순수하게 월드/몬스터에 집중되고
			// 게임모드 lua 의 print 출력도 사라진다. 몬스터 스폰/틱은 리소스가 없어도 동작한다.
		}
	};
	GlobalSetup g_setup;

	// 네비메시 위의 유효한 스폰 좌표(클라이언트 좌표계, x는 서버에서 반전됨)를 탐색한다.
	// 좌표는 네비메시 + 이동 전략의 스냅 조건에 의존한다. crowd 와 waypoint 는 addAgent
	// 의 스냅(쿼리 반경/조건)이 달라 받아들이는 좌표 집합이 다르므로, 전략별로 따로 탐색해
	// 캐싱한다. 이렇게 해야 두 전략이 같은 개체수(=같은 밀도)로 스폰되어 공정하게 비교된다.
	const std::vector<std::array<float, 3>>& ValidSpawns(const char* movement)
	{
		static std::unordered_map<std::string, std::vector<std::array<float, 3>>> cache;
		auto it = cache.find(movement);
		if (it != cache.end())
			return it->second;

		std::vector<std::array<float, 3>> out;
		World probe;
		probe.Init(movement);

		// solo_navmesh.bin 의 원점/타일 범위는 약 x:[-27,26], z:[-26.5,25] 이다.
		// addAgent 가 인근 폴리곤으로 스냅하므로 1유닛 격자로 훑는다.
		for (float x = -26.0f; x <= 26.0f; x += 1.0f)
			for (float z = -26.0f; z <= 25.0f; z += 1.0f)
				for (float y : { -1.0f, 2.0f, 6.0f })
				{
					syncnet::Vec3 v(x, y, z);
					auto actor = probe.OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v);
					if (actor)
					{
						out.push_back({ x, y, z });
						probe.OnRemoveAgent(actor->GetActorId());
					}
				}
		return cache.emplace(movement, std::move(out)).first->second;
	}

	// 월드에 count 마리의 몬스터를 스폰한다. 유효 좌표를 순환 사용한다.
	// 실제로 스폰된 수를 반환한다(정원/실패 시 count 보다 작을 수 있다).
	int SpawnMonsters(World& world, int count, const char* movement)
	{
		const auto& spawns = ValidSpawns(movement);
		if (spawns.empty())
			return 0;

		int spawned = 0;
		for (int i = 0; i < count; ++i)
		{
			const auto& c = spawns[i % spawns.size()];
			syncnet::Vec3 v(c[0], c[1], c[2]);
			if (world.OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v))
				++spawned;
		}
		return spawned;
	}
} // namespace

// 월드 생성 비용.
static void BM_WorldCreate(benchmark::State& state)
{
	for (auto _ : state)
	{
		World world;
		world.Init();
		benchmark::DoNotOptimize(&world);
	}
}
BENCHMARK(BM_WorldCreate)->Unit(benchmark::kMillisecond)->Iterations(30);

// 몬스터 스폰 처리량. 월드 생성은 타이밍에서 제외한다.
static void BM_WorldSpawnMonsters(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns("crowd"); // 유효 좌표 탐색은 1회성 비용이므로 타이밍 밖에서 미리 준비한다.
	int spawned = 0;
	for (auto _ : state)
	{
		state.PauseTiming();
		World world;
		world.Init();
		state.ResumeTiming();

		spawned = SpawnMonsters(world, count, "crowd");
		benchmark::DoNotOptimize(spawned);
	}
	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK(BM_WorldSpawnMonsters)
	->Arg(64)->Arg(128)->Arg(256)->Arg(512)->Arg(1000)
	->Unit(benchmark::kMicrosecond);

// N마리 몬스터가 살아있는 월드의 update(dt) 1회 소요 시간.
static void BM_WorldTick(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));

	World world;
	world.Init();
	const int spawned = SpawnMonsters(world, count, "crowd");

	for (auto _ : state)
	{
		world.update(kTickDt);
	}

	state.counters["monsters"] = spawned;
	// monsters * ticks / sec 환산용.
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK(BM_WorldTick)
	->Arg(1)->Arg(64)->Arg(128)->Arg(256)->Arg(512)->Arg(1000)
	->Unit(benchmark::kMicrosecond);

// 대규모: 몬스터 10000마리가 살아있는 월드의 update(dt) 평균 소요 시간.
//  - 스폰(셋업)은 타이밍에서 제외된다(반복마다 1회).
//  - MinTime 으로 충분한 반복 횟수를, Repetitions 로 평균/중앙값/표준편차를 산출한다.
//    (CrowdNavMovement::MAX_AGENTS / WaypointNavMovement 정원이 10000 이상이어야 한다.)
//  - movement 인자("crowd"/"waypoint")로 이동 전략을 강제 주입해 두 방식을 동일 조건에서 비교한다.
//    벤치는 ResourceLoader 를 안 띄워 게임모드가 null 이므로, 이 오버라이드가 유일한 전략 선택 경로다.
static void BM_WorldTick10000(benchmark::State& state, const char* movement)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns(movement); // 1회성 좌표 탐색을 셋업에서 먼저 끝낸다(전략별).

	World world;
	world.Init(movement);
	const int spawned = SpawnMonsters(world, count, movement);

	for (auto _ : state)
	{
		world.update(kTickDt);
	}

	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK_CAPTURE(BM_WorldTick10000, crowd, "crowd")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0)            // 반복당 최소 3초 동안 충분히 돌려 평균을 안정화
	->Repetitions(5)          // 5회 반복 → mean/median/stddev 리포트
	->ReportAggregatesOnly(true);
BENCHMARK_CAPTURE(BM_WorldTick10000, waypoint, "waypoint")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0)
	->Repetitions(5)
	->ReportAggregatesOnly(true);

// update(dt) 의 어느 구간이 병목인지 찾기 위한 단계별 프로파일.
//  Map::update 를 구성하는 4단계(actors → movement → systems → send)를 직접 호출하면서
//  각 단계의 소요 시간을 누적하고, 평균(반복당, 단위 us)을 counters 로 출력한다.
//  headline 시간(반복 전체)은 한 틱의 총 update 시간과 거의 같고, 그 안에서
//  actors_us / movement_us / systems_us / send_us 의 합으로 분해돼 병목 구간이 드러난다.
//
//  출력 예) ... actors_us=12345 movement_us=6789 systems_us=234 send_us=567 monsters=10000
//  → 가장 큰 *_us 값이 병목. crowd/waypoint 전략별로 따로 비교한다.
static void BM_WorldTickPhases(benchmark::State& state, const char* movement)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns(movement); // 1회성 좌표 탐색을 셋업에서 먼저 끝낸다.

	World world;
	world.Init(movement);
	const int spawned = SpawnMonsters(world, count, movement);
	Map* map = world.GetPrimaryMap();

	using clock = std::chrono::steady_clock;
	double actors_ns = 0.0, move_ns = 0.0, sys_ns = 0.0, send_ns = 0.0;
	int64_t iters = 0;

	for (auto _ : state)
	{
		// world.update() 가 호출하는 것과 동일한 순서로 직접 단계를 돌린다.
		const auto t0 = clock::now();
		map->UpdateActors(kTickDt);
		const auto t1 = clock::now();
		map->UpdateMovement(kTickDt);
		const auto t2 = clock::now();
		map->UpdateSystems(kTickDt);
		const auto t3 = clock::now();
		map->SendWorldState();
		const auto t4 = clock::now();

		actors_ns += std::chrono::duration<double, std::nano>(t1 - t0).count();
		move_ns   += std::chrono::duration<double, std::nano>(t2 - t1).count();
		sys_ns    += std::chrono::duration<double, std::nano>(t3 - t2).count();
		send_ns   += std::chrono::duration<double, std::nano>(t4 - t3).count();
		++iters;
	}

	const double inv = (iters > 0) ? (1.0 / static_cast<double>(iters) / 1000.0) : 0.0; // ns→us 평균
	state.counters["actors_us"]   = actors_ns * inv;
	state.counters["movement_us"] = move_ns * inv;
	state.counters["systems_us"]  = sys_ns * inv;
	state.counters["send_us"]     = send_ns * inv;
	state.counters["monsters"]    = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK_CAPTURE(BM_WorldTickPhases, crowd, "crowd")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);
BENCHMARK_CAPTURE(BM_WorldTickPhases, waypoint, "waypoint")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);

// actors 단계 안에서 적 탐지(ConditionDetectEnemy → DetectEnemy → 그리드 쿼리)만 격리 측정.
//  Time 열 = 10000마리에 대해 DetectEnemy 1회씩 = 한 틱 분량의 적 탐지 비용.
//  이 값이 BM_WorldTickPhases 의 actors_us 대부분을 차지하면 그리드 쿼리가 병목이다.
static void BM_DetectEnemyOnly(benchmark::State& state, const char* movement)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns(movement);

	World world;
	world.Init(movement);
	const int spawned = SpawnMonsters(world, count, movement);
	Map* map = world.GetPrimaryMap();

	long long found = 0;
	for (auto _ : state)
	{
		found += map->ProfileDetectEnemyAll();
		benchmark::DoNotOptimize(found);
	}
	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK_CAPTURE(BM_DetectEnemyOnly, crowd, "crowd")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);
