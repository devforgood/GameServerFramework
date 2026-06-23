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
// 주의: 스폰 수는 네비메시 크라우드 정원 NavMap::MAX_AGENTS 미만이어야 한다(현재 16384).
// 의미 있는 수치를 위해 Release/x64 로 빌드/실행할 것.

#include <benchmark/benchmark.h>

#include <array>
#include <memory>
#include <vector>

#define WIN32_LEAN_AND_MEAN
#include <windows.h>

#include "World.h"
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
	// 좌표는 네비메시에만 의존하므로 한 번만 찾아 캐싱한다.
	const std::vector<std::array<float, 3>>& ValidSpawns()
	{
		static const std::vector<std::array<float, 3>> spawns = []
		{
			std::vector<std::array<float, 3>> out;
			World probe;
			probe.Init();

			// solo_navmesh.bin 의 원점/타일 범위는 약 x:[-27,26], z:[-26.5,25] 이다.
			// 크라우드 addAgent 가 인근 폴리곤으로 스냅하므로 1유닛 격자로 훑는다.
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
			return out;
		}();
		return spawns;
	}

	// 월드에 count 마리의 몬스터를 스폰한다. 유효 좌표를 순환 사용한다.
	// 실제로 스폰된 수를 반환한다(크라우드 정원/실패 시 count 보다 작을 수 있다).
	int SpawnMonsters(World& world, int count)
	{
		const auto& spawns = ValidSpawns();
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
	ValidSpawns(); // 유효 좌표 탐색은 1회성 비용이므로 타이밍 밖에서 미리 준비한다.
	int spawned = 0;
	for (auto _ : state)
	{
		state.PauseTiming();
		World world;
		world.Init();
		state.ResumeTiming();

		spawned = SpawnMonsters(world, count);
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
	const int spawned = SpawnMonsters(world, count);

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
//    (NavMap::MAX_AGENTS 가 10000 이상이어야 한다.)
static void BM_WorldTick10000(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns(); // 1회성 좌표 탐색을 셋업에서 먼저 끝낸다.

	World world;
	world.Init();
	const int spawned = SpawnMonsters(world, count);

	for (auto _ : state)
	{
		world.update(kTickDt);
	}

	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK(BM_WorldTick10000)
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0)            // 반복당 최소 3초 동안 충분히 돌려 평균을 안정화
	->Repetitions(5)          // 5회 반복 → mean/median/stddev 리포트
	->ReportAggregatesOnly(true);
