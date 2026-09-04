// World/Monster 벤치마크.
//
// 실제 엔진 스택(Recast/Detour 크라우드 + BehaviorTree.CPP + Lua + ECS)을 그대로
// 사용해 다음을 측정한다.
//   - BM_WorldCreate        : 월드 1개 생성 비용(네비메시 로드 + lua 초기화 + 게임모드 부트스트랩)
//   - BM_WorldSpawnMonsters : 몬스터 N마리 스폰 처리량
//   - BM_WorldTick          : N마리가 살아있는 상태에서 update(dt) 1회 소요 시간
//   - BM_LargeMap*          : 가장 넓은 맵의 navmesh 전체에 흩뿌린 상태의 수용량/관심영역
//                             (위 벤치들은 primary 맵의 좁은 좌표 집합에 몬스터를 쌓는다)
//   - BM_MultiWorldTick*    : 월드 여러 개를 스레드에 나눠 돌렸을 때 프로세스 전체 수용량
//                             (운영 구성 그대로 — ServerConfig.world.thread_count)
//
// 자산(*_navmesh.bin, Monster.xml, Map.json, *.lua, *.json)은 GameDataPath::Resolve() 가
// 통합 폴더(Client/Assets/Resources/GameData)에서 찾는다. 리포 밖 배포 실행이면 exe 옆 GameData/ 를 쓴다.
//
// 주의: 스폰 수는 네비메시 크라우드 정원 CrowdNavMovement::MAX_AGENTS 미만이어야 한다(현재 16384).
// 의미 있는 수치를 위해 Release/x64 로 빌드/실행할 것.

#include <benchmark/benchmark.h>

#include <array>
#include <barrier>
#include <chrono>
#include <memory>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

#define WIN32_LEAN_AND_MEAN
// windows.h 의 min/max 매크로는 boost(uuid) 의 std::numeric_limits<T>::max() 를 깨뜨린다.
#define NOMINMAX
#include <windows.h>
#include <psapi.h>
#pragma comment(lib, "Psapi.lib")

#include "World.h"
#include "Map.h"
#include "NavMesh.h"
#include "Actor.h"
#include "Player.h"
#include "INavMovement.h"
#include "DetourCommon.h"
#include <algorithm>
#include <cmath>
#include <cstdint>
#include "ResourceLoader.h"
#include "syncnet_generated.h"
#include "LogHelper.h"
#include "spdlog/spdlog.h"

// BT 프레임워크 비교용. behaviortree_cpp 와 인하우스 BT(../BehaviorTree)는 둘 다
// namespace BT 를 쓰지만 클래스 이름이 겹치지 않아 한 TU 에서 공존한다.
#include "Monster.h"
#include "MonsterBT.h"
#include "MonsterCodeBaseBT.h"
#include "../BehaviorTree/BehaviorTree.h"
#include "behaviortree_cpp/bt_factory.h"

namespace
{
	// 서버 틱 간격(약 30Hz).
	constexpr float kTickDt = 1.0f / 30.0f;

	// 프로그램 시작 시 1회: 로그 초기화 + 리소스 로드.
	// 자산(mob.lua, Monster.xml, navmesh 등)은 GameDataPath::Resolve() 가 찾는
	// 통합 폴더(Client/Assets/Resources/GameData)에서 로드된다. 리졸버가 exe 위치도
	// 탐색 기준으로 쓰므로, 실행 방식(VS 디버거/더블클릭/터미널)에 따라 작업 디렉터리가
	// 달라져도 동작하도록 exe 폴더로 작업 디렉터리를 고정해 둔다.
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
			// 멀티맵 전환 이후 World::Init 은 ResourceLoader 의 맵 데이터(Map.json)로만
			// 맵을 만든다. 리소스를 안 읽으면 mapList_ 가 비어 OnAddAgent 가 크래시하므로
			// 반드시 로드한다. (field 게임모드 lua 는 몬스터를 스폰하지 않아 측정에 영향 없음.
			// primary 맵 = 최소 id 의 field 맵 = "Starting Village")
			ResourceLoader::Instance().LoadResources();
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

	// ── 큰 맵용 스폰 좌표 ──
	//
	// ValidSpawns 의 격자 훑기는 primary 맵(약 50유닛) 크기에 맞춰 x:[-26,26] 로 박혀 있다.
	// 500x500 맵에서는 가운데 1% 만 덮어서, 몬스터를 아무리 늘려도 한 곳에 쌓일 뿐이다.
	// 그리드/관심영역 수치는 '밀도'가 실제 배치와 비슷해야 의미가 있으므로,
	// navmesh 폴리곤을 면적 비례로 표본해 맵 전체에 흩뿌린다(맵툴의 스폰 자동 배치와 같은 방식).
	//
	// 반환 좌표는 클라 좌표계다 — OnAddAgent 가 내부에서 x 를 반전한다.
	std::vector<std::array<float, 3>> SampleNavMeshPoints(const NavMesh* navMesh, int count, uint32_t seed)
	{
		std::vector<std::array<float, 3>> out;
		if (navMesh == nullptr || navMesh->mesh() == nullptr || count <= 0)
			return out;

		// 폴리곤을 부채꼴 분할해 삼각형 + 누적 면적(수평 투영)을 만든다.
		struct Tri { float a[3], b[3], c[3]; };
		std::vector<Tri> tris;
		std::vector<float> cumulative;
		float totalArea = 0.0f;

		const dtNavMesh* mesh = navMesh->mesh();
		for (int t = 0; t < mesh->getMaxTiles(); ++t)
		{
			const dtMeshTile* tile = mesh->getTile(t);
			if (tile == nullptr || tile->header == nullptr)
				continue;

			for (int p = 0; p < tile->header->polyCount; ++p)
			{
				const dtPoly& poly = tile->polys[p];
				// flags 0 은 쿼리 필터에서 제외되는 폴리곤이라 스폰이 실패한다.
				if (poly.getType() == DT_POLYTYPE_OFFMESH_CONNECTION || poly.flags == 0 || poly.vertCount < 3)
					continue;

				const float* v0 = &tile->verts[poly.verts[0] * 3];
				for (int i = 1; i + 1 < poly.vertCount; ++i)
				{
					const float* v1 = &tile->verts[poly.verts[i] * 3];
					const float* v2 = &tile->verts[poly.verts[i + 1] * 3];

					const float area = std::fabs((v1[0] - v0[0]) * (v2[2] - v0[2])
					                           - (v2[0] - v0[0]) * (v1[2] - v0[2])) * 0.5f;
					if (area <= 0.0f)
						continue;

					Tri tri;
					dtVcopy(tri.a, v0);
					dtVcopy(tri.b, v1);
					dtVcopy(tri.c, v2);
					tris.push_back(tri);
					totalArea += area;
					cumulative.push_back(totalArea);
				}
			}
		}

		if (tris.empty())
			return out;

		// 실행마다 같은 배치가 나오도록 시드를 고정한다(측정 재현성).
		// 몬스터와 플레이어는 다른 시드를 써야 한다 — 같으면 플레이어가 몬스터 위에 정확히 겹친다.
		uint32_t rngState = seed;
		auto next01 = [&rngState]()
		{
			rngState = rngState * 1664525u + 1013904223u;
			return static_cast<float>((rngState >> 8) & 0xFFFFFF) / static_cast<float>(0x1000000);
		};

		out.reserve(static_cast<size_t>(count));
		for (int i = 0; i < count; ++i)
		{
			const float target = next01() * totalArea;
			const size_t index = static_cast<size_t>(
				std::lower_bound(cumulative.begin(), cumulative.end(), target) - cumulative.begin());
			const Tri& tri = tris[std::min(index, tris.size() - 1)];

			float u = next01(), v = next01();
			if (u + v > 1.0f) { u = 1.0f - u; v = 1.0f - v; }

			const float x = tri.a[0] + (tri.b[0] - tri.a[0]) * u + (tri.c[0] - tri.a[0]) * v;
			const float y = tri.a[1] + (tri.b[1] - tri.a[1]) * u + (tri.c[1] - tri.a[1]) * v;
			const float z = tri.a[2] + (tri.b[2] - tri.a[2]) * u + (tri.c[2] - tri.a[2]) * v;

			// 서버 좌표계 → 클라 좌표계(OnAddAgent 가 다시 반전한다).
			out.push_back({ -x, y, z });
		}
		return out;
	}

	// navmesh 범위가 가장 넓은 맵. 대규모 측정은 여기서 해야 한다(primary 는 약 50유닛).
	Map* LargestMap(World& world)
	{
		Map* best = nullptr;
		float bestArea = -1.0f;

		for (Map* map : world.GetMaps())
		{
			float bmin[3], bmax[3];
			if (map == nullptr || map->GetNavMesh() == nullptr || !map->GetNavMesh()->Bounds(bmin, bmax))
				continue;

			const float area = (bmax[0] - bmin[0]) * (bmax[2] - bmin[2]);
			if (area > bestArea)
			{
				bestArea = area;
				best = map;
			}
		}
		return best != nullptr ? best : world.GetPrimaryMap();
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

	// 수용량 계열 공통: world.update 를 반복하며 틱 비용과 예산 비율을 카운터에 채운다.
	// players 가 음수면 플레이어 카운터를 내보내지 않는다(몬스터 전용 벤치).
	void MeasureTickBudget(benchmark::State& state, World& world, int monsters, int players)
	{
		using clock = std::chrono::steady_clock;

		// 워밍업. 스폰 직후 첫 틱은 몬스터 전원이 같은 틱에 배회 목적지를 잡고 경로를 계산해
		// 정상 틱보다 수십 배 비싸다(넓은 맵 2만 마리에서 약 1초). 이걸 그대로 재면
		// google benchmark 가 min_time 을 그 한 틱으로 채워 버려서 반복이 1회에 그치고,
		// '정상 상태 틱 비용'이 아니라 콜드 스타트를 보고하게 된다.
		// 콜드 스타트도 실제 위험(서버 기동 시 스톨)이라 따로 기록하고, 측정은 안정된 뒤에 한다.
		const auto coldStart = clock::now();
		world.update(kTickDt);
		const double firstTickMs = std::chrono::duration<double, std::milli>(clock::now() - coldStart).count();

		constexpr int kWarmupTicks = 10;
		for (int i = 0; i < kWarmupTicks; ++i)
			world.update(kTickDt);

		double totalNs = 0.0;
		int64_t iters = 0;

		for (auto _ : state)
		{
			const auto t0 = clock::now();
			world.update(kTickDt);
			const auto t1 = clock::now();
			totalNs += std::chrono::duration<double, std::nano>(t1 - t0).count();
			++iters;
		}

		constexpr double kTickBudgetMs = 100.0; // 10Hz(SIM_RATE) 한 틱 예산
		const double tickMs = (iters > 0) ? (totalNs / static_cast<double>(iters) / 1e6) : 0.0;

		state.counters["monsters"] = monsters;
		if (players >= 0)
			state.counters["players"] = players;
		state.counters["first_tick_ms"] = firstTickMs; // 스폰 직후 경로 계산이 몰리는 콜드 스타트
		state.counters["tick_ms"] = tickMs;
		state.counters["budget_pct"] = tickMs / kTickBudgetMs * 100.0; // 100 을 넘으면 수용 한계 초과
		state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(monsters));
	}

	// 큰 맵 벤치의 몬스터/플레이어 표본 시드(서로 겹치지 않게 다른 값을 쓴다).
	constexpr uint32_t kMonsterSeed = 0x9E3779B9u;
	constexpr uint32_t kPlayerSeed = 0x85EBCA6Bu;
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
	ValidSpawns("waypoint"); // 유효 좌표 탐색은 1회성 비용이므로 타이밍 밖에서 미리 준비한다.
	int spawned = 0;
	for (auto _ : state)
	{
		state.PauseTiming();
		World world;
		// 프로덕션 기본 전략(waypoint)으로 고정한다. ValidSpawns 좌표는 전략별 스냅 조건으로
		// 탐색된 것이라 전략과 좌표 집합이 어긋나면 스폰이 실패한다.
		world.Init("waypoint");
		state.ResumeTiming();

		spawned = SpawnMonsters(world, count, "waypoint");
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
	world.Init("waypoint"); // 프로덕션 기본 전략. ValidSpawns 좌표와 이동 전략을 일치시킨다.
	const int spawned = SpawnMonsters(world, count, "waypoint");

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

// 수용량 탐색: "틱당 예산 안에 몇 마리까지 들어가는가".
//  서버 시뮬레이션은 10Hz(GameServer::SIM_RATE)이므로 한 틱 예산은 100ms 다.
//  budget_pct = 한 틱 소요 / 100ms. 이 값이 100% 를 넘는 지점이 수용 한계다.
//  BM_WorldTick 과 동일하게 World::update(모든 맵 + 게임모드)를 잰다.
static void BM_WorldTickCapacity(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns("waypoint");

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");

	MeasureTickBudget(state, world, spawned, -1);
}
BENCHMARK(BM_WorldTickCapacity)
	->Arg(10000)->Arg(16000)->Arg(20000)->Arg(24000)->Arg(28000)->Arg(32000)->Arg(40000)->Arg(60000)
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// 수용량(교전 포함): 플레이어가 있는 상태의 틱 비용.
//  몬스터만 있는 BM_WorldTickCapacity 는 전원이 배회 상태라 탐지가 즉시 실패하고 경로 재계산도 없다.
//  실제로는 플레이어 근처 몬스터가 교전에 들어가면서 매 틱 탐지 성공 + ActionChase 의
//  SetMoveTarget(경로 재계산) + 사거리 판정 raycast 를 돈다. 그 차이를 보기 위한 벤치다.
//  플레이어는 스폰 좌표를 넓게 훑어 배치해 여러 지점에서 동시에 교전이 일어나게 한다.
static void BM_WorldTickCapacityEngaged(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	const int playerCount = static_cast<int>(state.range(1));
	// 3번째 인자는 관심영역 반경(0 이면 맵 데이터 값). 반경이 맵 크기에 가까우면 모두가 모두를 보게 되어
	// 필터 효과가 사라지고 플레이어별 직렬화 중복만 남는다 — 그 차이를 재기 위한 인자다.
	const float aoiRadius = static_cast<float>(state.range(2));
	const auto& spawns = ValidSpawns("waypoint");

	World world;
	world.Init("waypoint");
	if (aoiRadius > 0.0f)
		world.GetPrimaryMap()->SetAoIRadius(aoiRadius);
	const int spawned = SpawnMonsters(world, count, "waypoint");

	// 캐릭터를 스폰 좌표에 고르게 흩뿌린다(몬스터와 같은 좌표 집합).
	std::vector<std::shared_ptr<Player>> players;
	int placed = 0;
	if (!spawns.empty() && playerCount > 0)
	{
		const size_t stride = std::max<size_t>(1, spawns.size() / static_cast<size_t>(playerCount));
		for (int i = 0; i < playerCount; ++i)
		{
			const auto& c = spawns[(i * stride) % spawns.size()];
			syncnet::Vec3 v(c[0], c[1], c[2]);
			auto player = std::make_shared<Player>();
			if (world.OnAddAgent(player, syncnet::GameObjectType_Character, &v))
			{
				players.push_back(player);
				++placed;
			}
		}
	}

	MeasureTickBudget(state, world, spawned, placed);
}
// 인자: (몬스터 수, 플레이어 수, 관심영역 반경 — 0 이면 맵 데이터 값)
BENCHMARK(BM_WorldTickCapacityEngaged)
	->Args({ 10000, 50, 0 })->Args({ 20000, 50, 0 })->Args({ 28000, 50, 0 })
	->Args({ 30000, 50, 0 })->Args({ 32000, 50, 0 })->Args({ 40000, 50, 0 })
	// 반경별 비교(맵이 약 50유닛이라 반경 40 이면 사실상 전 맵이 시야다).
	->Args({ 10000, 50, 20 })->Args({ 10000, 50, 10 })->Args({ 10000, 50, 5 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// ── 큰 맵: navmesh 전체에 분산 배치한 수용량 ──
//
// 위의 두 벤치는 primary 맵(약 50유닛)의 좁은 격자에 몬스터를 쌓는다. 셀당 밀도가
// 운영과 동떨어지고, 관심영역은 반경이 맵보다 커서 효과가 드러나지 않는다.
// 여기서는 가장 넓은 맵(navmesh 범위 기준)에 면적 비례로 흩뿌려 잰다.
static void BM_LargeMapTickCapacity(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));

	World world;
	world.Init("waypoint");

	Map* map = LargestMap(world);
	const auto spawns = SampleNavMeshPoints(map->GetNavMesh(), count, kMonsterSeed);

	int spawned = 0;
	for (const auto& c : spawns)
	{
		syncnet::Vec3 v(c[0], c[1], c[2]);
		if (map->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v))
			++spawned;
	}

	state.counters["map_id"] = map->GetMapId();
	MeasureTickBudget(state, world, spawned, -1);
}
BENCHMARK(BM_LargeMapTickCapacity)
	->Arg(10000)->Arg(20000)->Arg(40000)->Arg(60000)
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// 큰 맵 + 교전 + 관심영역 반경 비교.
// 인자: (몬스터 수, 플레이어 수, 관심영역 반경 — 0 이면 맵 데이터 값)
static void BM_LargeMapTickCapacityEngaged(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	const int playerCount = static_cast<int>(state.range(1));
	const float aoiRadius = static_cast<float>(state.range(2));

	World world;
	world.Init("waypoint");

	Map* map = LargestMap(world);
	if (aoiRadius > 0.0f)
		map->SetAoIRadius(aoiRadius);

	const auto spawns = SampleNavMeshPoints(map->GetNavMesh(), count, kMonsterSeed);
	int spawned = 0;
	for (const auto& c : spawns)
	{
		syncnet::Vec3 v(c[0], c[1], c[2]);
		if (map->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v))
			++spawned;
	}

	// 플레이어도 맵 전체에 흩뿌린다(몬스터와 다른 시드 = 다른 자리).
	//
	// World::OnAddAgent 가 아니라 맵에 직접 넣는다 — 스폰 맵이 primary 와 다르면
	// World 가 SendStateTo 까지 호출하는데, 세션 없는 벤치용 Player 에서는 그 경로가 죽는다.
	// 그리고 Enter 로 players_ 에 넣어야 SendWorldState 가 실제로 돈다. 비어 있으면
	// 그 단계가 통째로 조기 반환해서 관심영역 비교가 아무것도 재지 않는다.
	const auto playerSpots = SampleNavMeshPoints(map->GetNavMesh(), playerCount, kPlayerSeed);
	std::vector<std::shared_ptr<Player>> players;
	int placed = 0;
	for (const auto& c : playerSpots)
	{
		syncnet::Vec3 v(c[0], c[1], c[2]);
		auto player = std::make_shared<Player>();
		player->SetSpawnLocation(map->GetMapId(), v);
		map->Enter(player);
		if (map->OnAddAgent(player, syncnet::GameObjectType_Character, &v))
		{
			players.push_back(player);
			++placed;
		}
	}

	state.counters["map_id"] = map->GetMapId();
	state.counters["aoi"] = aoiRadius;
	MeasureTickBudget(state, world, spawned, placed);
}
BENCHMARK(BM_LargeMapTickCapacityEngaged)
	->Args({ 10000, 50, 0 })->Args({ 20000, 50, 0 })->Args({ 40000, 50, 0 })
	// 반경별 비교. 맵이 약 500유닛이라 이제 반경이 실제로 시야를 좁힌다(0 = 맵 전체 브로드캐스트).
	->Args({ 10000, 50, 100 })->Args({ 10000, 50, 50 })->Args({ 10000, 50, 25 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// ============================================================================
// 멀티스레드 멀티 월드: "한 프로세스에 몬스터를 몇 마리까지 담을 수 있나"
//
// 위의 벤치들은 전부 월드 하나를 한 스레드에서 돌린 값이다. 운영 서버는
// ServerConfig.world.thread_count 개의 스레드에 월드(=포트 하나당 GameServer 하나)를
// 라운드로빈으로 배정하고, 한 월드는 항상 같은 스레드에서만 갱신한다(ServerManager::IoWorker).
// 여기서는 그 구성을 그대로 재현해서 프로세스 전체 수용량을 잰다.
//
// 측정 기준은 "틱이 밀리지 않는가" 하나다. 서버 시뮬레이션은 10Hz(GameServer::SIM_RATE)라
// 한 틱 예산이 100ms 이고, 여러 스레드가 병렬로 도는 상황에서 틱을 못 지키는지는
// **가장 느린 스레드**가 결정한다. 그래서 매 틱 워커별 소요 시간의 최댓값을 모으고,
//   budget_pct = 평균(가장 느린 스레드 시간) / 100ms
//   worst_pct  = 최악(가장 느린 스레드 시간) / 100ms
// 로 보고한다. budget_pct 가 100 을 넘기 직전의 monsters 값이 그 구성의 수용 한계다.
//
// 이 벤치는 스레드 수를 늘렸을 때 실제로 선형에 가깝게 늘어나는지도 같이 보여준다.
// 월드끼리는 게임 상태를 전혀 공유하지 않지만, navmesh 질의/BT 틱/직렬화가 전부
// 메모리 대역폭과 캐시를 두고 경쟁하므로 코어 수만큼 그대로 곱해지지는 않는다.
//
// 주의: 월드 하나가 field 맵 전부(navmesh + 그리드)를 들고 있어서 월드를 늘리면
// 몬스터와 무관한 고정 메모리도 함께 늘어난다. rss_delta_mb / kb_per_monster 로 그 몫을 본다.
// ============================================================================

namespace multiworld
{
	// 현재 프로세스의 워킹셋(MB). 수용 한계는 틱뿐 아니라 메모리로도 결정되므로 같이 본다.
	double WorkingSetMb()
	{
		PROCESS_MEMORY_COUNTERS pmc{};
		if (!GetProcessMemoryInfo(GetCurrentProcess(), &pmc, sizeof(pmc)))
			return 0.0;
		return static_cast<double>(pmc.WorkingSetSize) / (1024.0 * 1024.0);
	}

	// 워커 스레드 풀. 스레드 하나가 배정받은 월드들을 순서대로 update 한다 —
	// ServerManager::IoWorker::UpdateGameLogic 과 같은 배치다(한 월드는 늘 같은 스레드).
	//
	// 틱은 배리어 두 개로 동기화한다. 그래야 "모든 워커가 같은 틱을 동시에 돌 때"의
	// 벽시계 시간을 재게 되고, 워커별 시간의 최댓값이 곧 그 틱의 실제 소요가 된다.
	// (동기화 없이 각자 돌게 두면 빠른 스레드가 앞서 나가 경쟁 상황이 실제보다 순해진다.)
	class Runner
	{
	public:
		explicit Runner(std::vector<std::vector<World*>> assignment)
			: assignment_(std::move(assignment))
			, tickMs_(assignment_.size(), 0.0)
			, start_(static_cast<std::ptrdiff_t>(assignment_.size()) + 1)
			, done_(static_cast<std::ptrdiff_t>(assignment_.size()) + 1)
		{
			threads_.reserve(assignment_.size());
			for (size_t i = 0; i < assignment_.size(); ++i)
				threads_.emplace_back([this, i] { WorkerLoop(i); });
		}

		~Runner()
		{
			stop_ = true;
			start_.arrive_and_wait(); // 대기 중인 워커를 깨워 종료시킨다(done_ 은 통과하지 않는다).
			for (auto& t : threads_)
				t.join();
		}

		// 모든 워커가 한 틱을 끝낼 때까지 기다리고, 그중 가장 오래 걸린 시간(ms)을 돌려준다.
		double Tick(float dt)
		{
			dt_ = dt;
			start_.arrive_and_wait();
			done_.arrive_and_wait();
			return *std::max_element(tickMs_.begin(), tickMs_.end());
		}

	private:
		void WorkerLoop(size_t index)
		{
			for (;;)
			{
				start_.arrive_and_wait();
				if (stop_)
					return;

				const auto t0 = std::chrono::steady_clock::now();
				for (World* world : assignment_[index])
					world->update(dt_);
				const auto t1 = std::chrono::steady_clock::now();

				// 워커마다 다른 원소만 쓴다(벡터는 재할당되지 않는다). 배리어가
				// 이 쓰기와 메인 스레드의 읽기 사이에 순서를 만들어 준다.
				tickMs_[index] = std::chrono::duration<double, std::milli>(t1 - t0).count();

				done_.arrive_and_wait();
			}
		}

		std::vector<std::vector<World*>> assignment_;
		std::vector<std::thread> threads_;
		std::vector<double> tickMs_;
		std::barrier<> start_;
		std::barrier<> done_;
		float dt_ = 0.0f;
		bool stop_ = false;   // start_ 배리어 이전에만 쓰고 이후에만 읽는다.
	};
} // namespace multiworld

// 인자: (스레드 수, 스레드당 월드 수, 월드당 몬스터 수)
//
// 월드 생성/스폰은 메인 스레드에서 순차로 끝낸다 — World::Init 은 공유 lua 상태를
// 초기화하고 ResourceLoader 를 읽으므로 병렬로 만들면 안 된다. 측정 대상은 틱뿐이다.
static void BM_MultiWorldTickCapacity(benchmark::State& state)
{
	const int threadCount = static_cast<int>(state.range(0));
	const int worldsPerThread = static_cast<int>(state.range(1));
	const int monstersPerWorld = static_cast<int>(state.range(2));

	const double rssBefore = multiworld::WorkingSetMb();

	std::vector<std::unique_ptr<World>> worlds;
	std::vector<std::vector<World*>> assignment(static_cast<size_t>(threadCount));
	int totalMonsters = 0;

	for (int t = 0; t < threadCount; ++t)
	{
		for (int w = 0; w < worldsPerThread; ++w)
		{
			auto world = std::make_unique<World>();
			world->Init("waypoint"); // 프로덕션 기본 이동 전략.

			Map* map = LargestMap(*world);
			if (map == nullptr || map->GetNavMesh() == nullptr)
			{
				state.SkipWithError("no navmesh map available");
				return;
			}

			// 월드마다 시드를 달리해 모든 월드가 똑같은 자리에 겹쳐 서지 않게 한다.
			const uint32_t seed = kMonsterSeed + static_cast<uint32_t>(worlds.size()) * 0x9E3779B9u;
			for (const auto& c : SampleNavMeshPoints(map->GetNavMesh(), monstersPerWorld, seed))
			{
				syncnet::Vec3 v(c[0], c[1], c[2]);
				if (map->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v))
					++totalMonsters;
			}

			assignment[static_cast<size_t>(t)].push_back(world.get());
			worlds.push_back(std::move(world));
		}
	}

	const double rssAfter = multiworld::WorkingSetMb();

	multiworld::Runner runner(std::move(assignment));

	// 콜드 스타트: 스폰 직후 첫 틱은 전원이 같은 틱에 배회 목적지를 잡고 경로를 계산해
	// 정상 틱보다 수십 배 비싸다. 실제 위험(기동 직후 스톨)이라 따로 기록하되,
	// 정상 상태 수용량을 재야 하므로 측정은 안정된 뒤에 시작한다.
	const double firstTickMs = runner.Tick(kTickDt);
	constexpr int kWarmupTicks = 10;
	for (int i = 0; i < kWarmupTicks; ++i)
		runner.Tick(kTickDt);

	double worstMs = 0.0;
	double sumMs = 0.0;
	int64_t iters = 0;

	for (auto _ : state)
	{
		const double ms = runner.Tick(kTickDt);
		worstMs = std::max(worstMs, ms);
		sumMs += ms;
		++iters;
	}

	constexpr double kTickBudgetMs = 100.0; // 10Hz(SIM_RATE) 한 틱 예산
	const double meanMs = (iters > 0) ? (sumMs / static_cast<double>(iters)) : 0.0;

	state.counters["threads"] = threadCount;
	state.counters["worlds"] = threadCount * worldsPerThread;
	state.counters["monsters"] = totalMonsters;            // 프로세스 전체
	state.counters["per_world"] = monstersPerWorld;
	state.counters["first_tick_ms"] = firstTickMs;
	state.counters["tick_ms"] = meanMs;                    // 가장 느린 스레드 기준 평균
	state.counters["worst_tick_ms"] = worstMs;
	state.counters["budget_pct"] = meanMs / kTickBudgetMs * 100.0; // 100 초과 = 수용 한계 초과
	state.counters["worst_pct"] = worstMs / kTickBudgetMs * 100.0;
	state.counters["rss_mb"] = rssAfter;
	// 이 구성이 새로 잡은 메모리. 여러 구성을 한 번에 돌리면 앞 구성이 반납한 블록을
	// 할당자가 재사용해서 실제보다 작게 나온다 — 메모리를 볼 때는 구성 하나만 필터해서 돌릴 것.
	state.counters["rss_delta_mb"] = rssAfter - rssBefore;
	state.counters["kb_per_monster"] =
		(totalMonsters > 0) ? ((rssAfter - rssBefore) * 1024.0 / totalMonsters) : 0.0;

	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(totalMonsters));
}

// 스레드 확장성: 월드당 몬스터를 고정하고 스레드만 늘린다.
// 이상적이면 monsters 는 스레드 수에 비례해 늘고 tick_ms 는 그대로여야 한다.
// 실제로는 메모리 대역폭 경쟁 때문에 tick_ms 가 서서히 올라간다 — 그 기울기가 이 벤치의 핵심이다.
BENCHMARK(BM_MultiWorldTickCapacity)
	->Args({ 1, 1, 20000 })
	->Args({ 2, 1, 20000 })
	->Args({ 4, 1, 20000 })
	->Args({ 8, 1, 20000 })
	->Args({ 16, 1, 20000 })
	// 스레드 수를 고정하고 월드당 몬스터를 늘려 한계 지점을 찾는다.
	->Args({ 8, 1, 10000 })
	->Args({ 8, 1, 30000 })
	->Args({ 8, 1, 40000 })
	// 코어를 다 쓰는 구성에서 월드당 밀도를 올려 100% 에 닿는 지점을 찾는다.
	->Args({ 16, 1, 30000 })
	->Args({ 16, 1, 40000 })
	// 한 스레드가 월드 여러 개를 맡는 경우(포트 수 > thread_count). 같은 총량을 월드 하나로
	// 몰았을 때와 비교하면, 비용이 몬스터 수에 붙는지 월드 수에 붙는지가 갈린다.
	->Args({ 8, 2, 10000 })
	->Args({ 16, 2, 20000 })
	->Unit(benchmark::kMillisecond)
	->UseRealTime()   // 워커가 병렬로 돌아 CPU 시간은 스레드 수만큼 부풀려진다. 벽시계로 본다.
	->MinTime(1.0);

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
// Arg(1) 은 몬스터 수와 무관한 '고정비'를 드러낸다(이동 시뮬/ECS 순회/스냅샷 생성).
// BM_WorldTick/1 은 World::update 라 모든 맵을 돌지만 여기는 primary 맵 하나만 돈다.
// 두 값의 차이가 곧 '살아있지만 비어 있는 나머지 맵들'의 비용이다.
// waypoint 가 프로덕션 기본 전략이므로 스케일 전체를 재고, crowd 는 비교용으로 10000만 잰다.
BENCHMARK_CAPTURE(BM_WorldTickPhases, waypoint, "waypoint")
	->Arg(1)
	->Arg(1000)
	->Arg(10000)
	->Arg(24000) // 10Hz 수용 한계 부근 — 어느 단계가 먼저 무너지는지 본다
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);
BENCHMARK_CAPTURE(BM_WorldTickPhases, crowd, "crowd")
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
// 이동 명령 1회 = 경로 산출 비용(dtNavMeshQuery findPath + findStraightPath).
//  ActionPatrol 은 목적지 도달/휴식 종료 때마다, ActionChase 는 교전 중 매 틱 이 비용을 낸다.
//  mode: 0 = Patrol(임의 지점 탐색 + 경로), 1 = SetMoveTarget(지정 지점 경로), 2 = Raycast(시야 판정).
static void BM_NavPathIssue(benchmark::State& state, int mode)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns("waypoint");

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");
	Map* map = world.GetPrimaryMap();
	INavMovement* nav = map->GetNavMap();

	// 목적지로 쓸 좌표 하나(첫 에이전트 위치).
	float target[3] = { 0, 0, 0 };
	if (const float* p = nav->GetPos(0))
		dtVcopy(target, p);

	int id = 0;
	for (auto _ : state)
	{
		const float* origin = nav->GetPos(id);
		if (origin != nullptr)
		{
			if (mode == 0)
			{
				float dest[3];
				nav->Patrol(id, origin, 40.0f, dest);
				benchmark::DoNotOptimize(dest);
			}
			else if (mode == 1)
			{
				nav->SetMoveTarget(id, target, false);
			}
			else
			{
				// 시야 판정: DetectEnemy 가 후보 캐릭터마다, AttackRange 가 사거리 안에서 부른다.
				float hit[3];
				benchmark::DoNotOptimize(nav->Raycast(id, target, hit));
			}
		}
		id = (id + 1) % (spawned > 0 ? spawned : 1);
	}
	state.counters["monsters"] = spawned;
}
BENCHMARK_CAPTURE(BM_NavPathIssue, patrol, 0)->Arg(10000)->Unit(benchmark::kMicrosecond);
BENCHMARK_CAPTURE(BM_NavPathIssue, move_target, 1)->Arg(10000)->Unit(benchmark::kMicrosecond);
BENCHMARK_CAPTURE(BM_NavPathIssue, raycast, 2)->Arg(10000)->Unit(benchmark::kMicrosecond);

// 적 탐지 비용을 '캐릭터가 있을 때' 로 다시 잰다.
//  BM_DetectEnemyOnly 는 캐릭터가 0명이라 그리드 스캔만 돌고 끝난다. 실제로는 후보 캐릭터마다
//  navmesh Raycast(시야 판정)가 붙으므로, 플레이어가 있으면 탐지 단가 자체가 달라진다.
static void BM_DetectEnemyWithPlayers(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	const int playerCount = static_cast<int>(state.range(1));
	const auto& spawns = ValidSpawns("waypoint");

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");
	Map* map = world.GetPrimaryMap();

	std::vector<std::shared_ptr<Player>> players;
	int placed = 0;
	if (!spawns.empty() && playerCount > 0)
	{
		const size_t stride = std::max<size_t>(1, spawns.size() / static_cast<size_t>(playerCount));
		for (int i = 0; i < playerCount; ++i)
		{
			const auto& c = spawns[(i * stride) % spawns.size()];
			syncnet::Vec3 v(c[0], c[1], c[2]);
			auto player = std::make_shared<Player>();
			if (world.OnAddAgent(player, syncnet::GameObjectType_Character, &v))
			{
				players.push_back(player);
				++placed;
			}
		}
	}

	long long found = 0;
	for (auto _ : state)
	{
		found += map->ProfileDetectEnemyAll();
		benchmark::DoNotOptimize(found);
	}
	state.counters["monsters"] = spawned;
	state.counters["players"] = placed;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK(BM_DetectEnemyWithPlayers)
	->Args({ 10000, 0 })->Args({ 10000, 50 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// systems 단계 안에서 ActorInfo 직렬화(flatbuffer)만 격리 측정.
//  Time 열 = 10000마리 전원을 1회 직렬화하는 비용 = 한 틱 분량(모두 움직였다고 가정).
//  이 값이 BM_WorldTickPhases 의 systems_us 에서 큰 비중이면, 관전자가 없을 때
//  직렬화를 건너뛰는 것만으로 그만큼이 사라진다.
static void BM_SerializeActorsOnly(benchmark::State& state)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns("waypoint");

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");
	Map* map = world.GetPrimaryMap();

	long long serialized = 0;
	for (auto _ : state)
	{
		serialized += map->ProfileSerializeAll();
		benchmark::DoNotOptimize(serialized);
	}
	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK(BM_SerializeActorsOnly)
	->Arg(10000)->Arg(40000)
	->Unit(benchmark::kMicrosecond)
	->MinTime(1.0);

BENCHMARK_CAPTURE(BM_DetectEnemyOnly, waypoint, "waypoint")
	->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);

// ============================================================================
// BT 프레임워크 비교: behaviortree_cpp vs ../BehaviorTree(인하우스)
//
// 두 백엔드는 노드 로직을 아예 공유하고(Engine/AI/MonsterBTNodes.h) 트리 구조도 1:1 로
// 같으므로, 게임 로직 비용은 상쇄되고 프레임워크 자체의 오버헤드 차이가 드러난다.
//   - BM_BTFrameworkTick_* : 스텁 노드로 만든 동일 토폴로지 트리 1회 틱(순수 오버헤드)
//   - BM_BTCreate/*        : 몬스터 1마리분 트리 생성 비용(프로덕션 경로 그대로)
//   - BM_BTWorldTickActors/*: 몬스터 N마리 UpdateActors(BT 틱 포함) 1회 비용
// ============================================================================

namespace btbench
{
	// 시나리오: 0=배회(탐지 실패), 1=추격(탐지 성공/사거리 실패), 2=공격(둘 다 성공).
	// 두 프레임워크의 스텁 트리가 같은 값을 읽어 같은 실행 경로를 밟는다.
	int g_scenario = 0;
	long long g_work = 0; // 액션 실행 횟수(최적화 방지 겸 경로 검증용).

	// Monster.xml 과 동일한 토폴로지의 스텁 트리(behaviortree_cpp 용).
	constexpr const char* kStubTreeXml = R"(
<root BTCPP_format="4">
  <BehaviorTree ID="BenchTree">
    <Fallback>
      <Sequence>
        <StubCheckHealth/>
        <Fallback>
          <Sequence>
            <StubDetectEnemy/>
            <Fallback>
              <Sequence>
                <StubAttackRange/>
                <StubAttack/>
              </Sequence>
              <StubChase/>
            </Fallback>
          </Sequence>
          <StubPatrol/>
        </Fallback>
      </Sequence>
      <Sequence>
        <StubDead/>
        <Delay delay_msec="2000">
          <StubDestroyed/>
        </Delay>
      </Sequence>
    </Fallback>
  </BehaviorTree>
</root>
)";

	// --- behaviortree_cpp 스텁 노드 ---
	class StubCondition : public BT::ConditionNode
	{
	private:
		int threshold_; // g_scenario >= threshold_ 이면 SUCCESS.
	public:
		StubCondition(const std::string& name, const BT::NodeConfig& config, int threshold)
			: BT::ConditionNode(name, config), threshold_(threshold) {}
		BT::NodeStatus tick() override
		{
			return g_scenario >= threshold_ ? BT::NodeStatus::SUCCESS : BT::NodeStatus::FAILURE;
		}
	};

	class StubAction : public BT::SyncActionNode
	{
	public:
		StubAction(const std::string& name, const BT::NodeConfig& config)
			: BT::SyncActionNode(name, config) {}
		BT::NodeStatus tick() override
		{
			++g_work;
			return BT::NodeStatus::SUCCESS;
		}
	};

	// --- 인하우스 BT 스텁 노드 ---
	class StubConditionIB : public BT::Condition
	{
	private:
		int threshold_;
	public:
		static BT::Behavior* Create(int threshold) { return new StubConditionIB(threshold); }
		virtual std::string Name() override { return "StubCondition"; }
	protected:
		StubConditionIB(int threshold) : Condition(false), threshold_(threshold) {}
		virtual ~StubConditionIB() {}
		virtual BT::EStatus Update() override
		{
			return g_scenario >= threshold_ ? BT::EStatus::Success : BT::EStatus::Failure;
		}
	};

	class StubActionIB : public BT::Action
	{
	public:
		static BT::Behavior* Create() { return new StubActionIB(); }
		virtual std::string Name() override { return "StubAction"; }
	protected:
		StubActionIB() {}
		virtual ~StubActionIB() {}
		virtual BT::EStatus Update() override
		{
			++g_work;
			return BT::EStatus::Success;
		}
	};

	// behaviortree_cpp 의 <Delay> 에 해당(사망 분기 토폴로지 맞춤용, 벤치 경로에선 실행 안 됨).
	class StubDelayIB : public BT::Decorator
	{
	private:
		std::chrono::milliseconds delay_;
		std::chrono::steady_clock::time_point until_;
	public:
		static BT::Behavior* Create(int delayMs) { return new StubDelayIB(delayMs); }
		virtual std::string Name() override { return "Delay"; }
		virtual void Release() override
		{
			if (Child != nullptr)
				Child->Release();
			delete this;
		}
	protected:
		StubDelayIB(int delayMs) : delay_(delayMs) {}
		virtual ~StubDelayIB() {}
		virtual void OnInitialize() override { until_ = std::chrono::steady_clock::now() + delay_; }
		virtual BT::EStatus Update() override
		{
			if (std::chrono::steady_clock::now() < until_)
				return BT::EStatus::Running;
			return Child->Tick();
		}
	};

	// Monster.xml 과 동일한 토폴로지의 스텁 트리(인하우스 BT 용). 호출자가 Release+delete.
	BT::BehaviorTree* CreateStubTreeCodeBase()
	{
		BT::BehaviorTreeBuilder builder;
		return builder
			.Selector()                                                  // Fallback (root)
				->Sequence()                                             // 생존 분기
					->Condition(StubConditionIB::Create(0))->Back()      // CheckHealth: 항상 성공
					->Selector()
						->Sequence()
							->Condition(StubConditionIB::Create(1))->Back() // DetectEnemy
							->Selector()
								->Sequence()
									->Condition(StubConditionIB::Create(2))->Back() // AttackRange
									->Action(StubActionIB::Create())->Back()        // Attack
								->Back()
								->Action(StubActionIB::Create())->Back()            // Chase
							->Back()
						->Back()
						->Action(StubActionIB::Create())->Back()                    // Patrol
					->Back()
				->Back()
				->Sequence()                                             // 사망 분기(실행 안 됨)
					->Action(StubActionIB::Create())->Back()             // Dead
					->Action(StubDelayIB::Create(2000))
						->Action(StubActionIB::Create())->Back()         // Destroyed
					->Back()
				->Back()
			->End();
	}
} // namespace btbench

// 스텁 트리 1회 틱: behaviortree_cpp. 시나리오를 순환시켜 세 실행 경로를 고르게 밟는다.
static void BM_BTFrameworkTick_BTCpp(benchmark::State& state)
{
	BT::BehaviorTreeFactory factory;
	factory.registerBuilder<btbench::StubCondition>("StubCheckHealth",
		[](const std::string& name, const BT::NodeConfig& config) {
			return std::make_unique<btbench::StubCondition>(name, config, 0);
		});
	factory.registerBuilder<btbench::StubCondition>("StubDetectEnemy",
		[](const std::string& name, const BT::NodeConfig& config) {
			return std::make_unique<btbench::StubCondition>(name, config, 1);
		});
	factory.registerBuilder<btbench::StubCondition>("StubAttackRange",
		[](const std::string& name, const BT::NodeConfig& config) {
			return std::make_unique<btbench::StubCondition>(name, config, 2);
		});
	for (const char* id : { "StubAttack", "StubChase", "StubPatrol", "StubDead", "StubDestroyed" })
	{
		factory.registerBuilder<btbench::StubAction>(id,
			[](const std::string& name, const BT::NodeConfig& config) {
				return std::make_unique<btbench::StubAction>(name, config);
			});
	}

	auto tree = factory.createTreeFromText(btbench::kStubTreeXml);

	int i = 0;
	for (auto _ : state)
	{
		btbench::g_scenario = i++ % 3;
		tree.tickOnce();
	}
	benchmark::DoNotOptimize(btbench::g_work);
}
BENCHMARK(BM_BTFrameworkTick_BTCpp);

// 스텁 트리 1회 틱: 인하우스 BT. 위와 동일 토폴로지/시나리오.
static void BM_BTFrameworkTick_CodeBase(benchmark::State& state)
{
	BT::BehaviorTree* tree = btbench::CreateStubTreeCodeBase();

	int i = 0;
	for (auto _ : state)
	{
		btbench::g_scenario = i++ % 3;
		tree->Tick();
	}
	benchmark::DoNotOptimize(btbench::g_work);

	tree->Release();
	delete tree;
}
BENCHMARK(BM_BTFrameworkTick_CodeBase);

// 몬스터 1마리분 트리 생성+해제 비용(프로덕션 경로 그대로).
// behaviortree_cpp 는 매 호출 팩토리 등록 + GameData/Monster.xml 파일 로드/파싱을 포함한다
// (Monster::Init 이 실제로 지불하는 비용). 인하우스는 빌더로 노드를 직접 생성한다.
static void BM_BTCreate(benchmark::State& state, Monster::BTBackend backend)
{
	const auto& spawns = ValidSpawns("waypoint");
	if (spawns.empty())
	{
		state.SkipWithError("no valid spawn position");
		return;
	}

	World world;
	world.Init("waypoint");
	syncnet::Vec3 v(spawns[0][0], spawns[0][1], spawns[0][2]);
	auto actor = world.OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v);
	Monster* monster = static_cast<Monster*>(actor.get());

	if (backend == Monster::BTBackend::BTCpp)
	{
		for (auto _ : state)
		{
			BT::Tree* tree = MonsterBT::createTree(monster);
			benchmark::DoNotOptimize(tree);
			delete tree;
		}
	}
	else
	{
		for (auto _ : state)
		{
			BT::BehaviorTree* tree = MonsterCodeBaseBT::createTree(monster);
			benchmark::DoNotOptimize(tree);
			tree->Release();
			delete tree;
		}
	}
}
BENCHMARK_CAPTURE(BM_BTCreate, btcpp, Monster::BTBackend::BTCpp)
	->Unit(benchmark::kMicrosecond);
BENCHMARK_CAPTURE(BM_BTCreate, codebase, Monster::BTBackend::CodeBase)
	->Unit(benchmark::kMicrosecond);

// 몬스터 N마리 UpdateActors 1회(=한 틱 분량의 BT 실행 포함) 비용을 백엔드별로 비교.
// 두 트리는 동일한 게임 로직(탐지/배회/추격)을 수행하므로 차이 = 프레임워크 오버헤드.
// movement 는 갱신하지 않아(UpdateActors 만 호출) 이동 비용이 섞이지 않는다.
static void BM_BTWorldTickActors(benchmark::State& state, Monster::BTBackend backend)
{
	const int count = static_cast<int>(state.range(0));
	ValidSpawns("waypoint"); // 1회성 좌표 탐색을 셋업에서 먼저 끝낸다.

	// 백엔드는 스폰 시점에 트리를 결정하므로 반드시 스폰 전에 설정한다.
	const Monster::BTBackend previous = Monster::btBackend_;
	Monster::btBackend_ = backend;

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");
	Map* map = world.GetPrimaryMap();

	for (auto _ : state)
	{
		map->UpdateActors(kTickDt);
	}
	Monster::btBackend_ = previous; // 기본값 복원.

	state.counters["monsters"] = spawned;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(spawned));
}
BENCHMARK_CAPTURE(BM_BTWorldTickActors, btcpp, Monster::BTBackend::BTCpp)
	->Arg(1000)->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);
BENCHMARK_CAPTURE(BM_BTWorldTickActors, codebase, Monster::BTBackend::CodeBase)
	->Arg(1000)->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);
BENCHMARK_CAPTURE(BM_BTWorldTickActors, ecs, Monster::BTBackend::Ecs)
	->Arg(1000)->Arg(10000)
	->Unit(benchmark::kMillisecond)
	->MinTime(3.0);

// 백엔드별 수용량(교전 포함). BM_WorldTickCapacityEngaged 와 같은 구성이지만 백엔드를
// 스폰 전에 고정해 두 구현을 한 번의 실행으로 비교한다. 인자는 (몬스터 수, 플레이어 수).
//
// actors 단계만 재는 BM_BTWorldTickActors 와 달리 월드 전체(update)를 재므로,
// 백엔드 교체가 이동/ECS/전송 단계에 미치는 영향(캐시 압력, 상태 변경 빈도)까지 들어온다.
static void BM_BTCapacityEngaged(benchmark::State& state, Monster::BTBackend backend)
{
	const int count = static_cast<int>(state.range(0));
	const int playerCount = static_cast<int>(state.range(1));
	const auto& spawns = ValidSpawns("waypoint");

	const Monster::BTBackend previous = Monster::btBackend_;
	Monster::btBackend_ = backend; // 트리는 스폰 시점에 정해진다.

	World world;
	world.Init("waypoint");
	const int spawned = SpawnMonsters(world, count, "waypoint");

	std::vector<std::shared_ptr<Player>> players;
	int placed = 0;
	if (!spawns.empty() && playerCount > 0)
	{
		const size_t stride = std::max<size_t>(1, spawns.size() / static_cast<size_t>(playerCount));
		for (int i = 0; i < playerCount; ++i)
		{
			const auto& c = spawns[(i * stride) % spawns.size()];
			syncnet::Vec3 v(c[0], c[1], c[2]);
			auto player = std::make_shared<Player>();
			if (world.OnAddAgent(player, syncnet::GameObjectType_Character, &v))
			{
				players.push_back(player);
				++placed;
			}
		}
	}

	MeasureTickBudget(state, world, spawned, placed);
	Monster::btBackend_ = previous;
}
BENCHMARK_CAPTURE(BM_BTCapacityEngaged, codebase, Monster::BTBackend::CodeBase)
	->Args({ 10000, 50 })->Args({ 20000, 50 })->Args({ 40000, 50 })
	->Args({ 10000, 0 })->Args({ 40000, 0 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);
BENCHMARK_CAPTURE(BM_BTCapacityEngaged, ecs, Monster::BTBackend::Ecs)
	->Args({ 10000, 50 })->Args({ 20000, 50 })->Args({ 40000, 50 })
	->Args({ 10000, 0 })->Args({ 40000, 0 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);

// 백엔드별 수용량(큰 맵). BM_LargeMapTickCapacityEngaged 와 같은 구성 —
// navmesh 전체에 면적 비례로 흩뿌리고 플레이어는 Map::Enter 까지 거쳐 실제 전송 단계를 태운다.
// 좁은 primary 맵에 몬스터를 쌓는 BM_BTCapacityEngaged 보다 운영 분포에 가깝다.
static void BM_LargeMapBTBackend(benchmark::State& state, Monster::BTBackend backend)
{
	const int count = static_cast<int>(state.range(0));
	const int playerCount = static_cast<int>(state.range(1));

	const Monster::BTBackend previous = Monster::btBackend_;
	Monster::btBackend_ = backend; // 트리는 스폰 시점에 정해진다.

	World world;
	world.Init("waypoint");
	Map* map = LargestMap(world);

	const auto spawns = SampleNavMeshPoints(map->GetNavMesh(), count, kMonsterSeed);
	int spawned = 0;
	for (const auto& c : spawns)
	{
		syncnet::Vec3 v(c[0], c[1], c[2]);
		if (map->OnAddAgent(nullptr, syncnet::GameObjectType_Monster, &v))
			++spawned;
	}

	const auto playerSpots = SampleNavMeshPoints(map->GetNavMesh(), playerCount, kPlayerSeed);
	std::vector<std::shared_ptr<Player>> players;
	int placed = 0;
	for (const auto& c : playerSpots)
	{
		syncnet::Vec3 v(c[0], c[1], c[2]);
		auto player = std::make_shared<Player>();
		player->SetSpawnLocation(map->GetMapId(), v);
		map->Enter(player);
		if (map->OnAddAgent(player, syncnet::GameObjectType_Character, &v))
		{
			players.push_back(player);
			++placed;
		}
	}

	state.counters["map_id"] = map->GetMapId();
	MeasureTickBudget(state, world, spawned, placed);
	Monster::btBackend_ = previous;
}
BENCHMARK_CAPTURE(BM_LargeMapBTBackend, codebase, Monster::BTBackend::CodeBase)
	->Args({ 10000, 50 })->Args({ 20000, 50 })->Args({ 40000, 50 })->Args({ 60000, 50 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);
// ECS 는 40,000마리에서도 예산에 여유가 있어 한계를 보려면 더 큰 수가 필요하다.
BENCHMARK_CAPTURE(BM_LargeMapBTBackend, ecs, Monster::BTBackend::Ecs)
	->Args({ 10000, 50 })->Args({ 20000, 50 })->Args({ 40000, 50 })->Args({ 60000, 50 })
	->Args({ 100000, 50 })->Args({ 140000, 50 })
	->Unit(benchmark::kMillisecond)
	->MinTime(1.0);
