// GridManager 격리 벤치마크.
//
// 월드 틱의 actors/systems 단계는 그리드를 두 가지로 쓴다:
//   - 적 탐지: Map::DetectEnemy → getCharactersInViewRange(range 10)
//   - 위치 갱신: Map::SyncActorState → GridManager::move(셀 이동 시 재등록)
// 여기서는 Map/World 없이 그리드만 떼어내 밀도별 비용을 잰다.
// 그리드 설정은 프로덕션과 동일하다(Map::Init 의 GridManager(100, 100, 2)).

#include <benchmark/benchmark.h>

#include <cstdlib>
#include <memory>
#include <vector>

#include "GridManager.h"

namespace
{
	constexpr int kGridWidth = 100;
	constexpr int kGridHeight = 100;
	constexpr int kCellSize = 2;
	constexpr float kDetectRange = 10.0f; // Map.cpp 의 g_fDistance
	constexpr float kWorldHalf = kGridWidth * kCellSize / 2.0f - 1.0f;

	class BenchActor : public IGridActor
	{
	public:
		BenchActor(int id, float x, float y, bool isCharacter)
			: id_(id), x_(x), y_(y), isCharacter_(isCharacter) {}

		bool IsCharacter() const override { return isCharacter_; }
		void SetGridX(int gx) override { gridX_ = gx; }
		void SetGridY(int gy) override { gridY_ = gy; }
		int GetGridX() const override { return gridX_; }
		int GetGridY() const override { return gridY_; }
		float GetVector2X() const override { return x_; }
		float GetVector2Y() const override { return y_; }
		int GetActorId() const override { return id_; }
		void DecrementHealth(int) override {}
		void SetLastAttacker(int) override {}

		void SetPos(float x, float y) { x_ = x; y_ = y; }

	private:
		int id_;
		float x_, y_;
		bool isCharacter_;
		int gridX_ = -1, gridY_ = -1;
	};

	// 결정적 의사난수(런마다 같은 배치를 쓰기 위해 rand 대신 직접 굴린다).
	struct Rng
	{
		uint32_t s = 12345;
		float Next01()
		{
			s = s * 1664525u + 1013904223u;
			return static_cast<float>((s >> 8) & 0xFFFFFF) / static_cast<float>(0x1000000);
		}
		float Range(float lo, float hi) { return lo + Next01() * (hi - lo); }
	};

	// 액터를 월드 전역에 흩뿌려 그리드에 등록한다.
	std::vector<std::unique_ptr<BenchActor>> Populate(GridManager& grid, int monsters, int characters)
	{
		std::vector<std::unique_ptr<BenchActor>> actors;
		actors.reserve(static_cast<size_t>(monsters + characters));

		Rng rng;
		for (int i = 0; i < monsters + characters; ++i)
		{
			const bool isCharacter = (i >= monsters);
			auto actor = std::make_unique<BenchActor>(
				i, rng.Range(-kWorldHalf, kWorldHalf), rng.Range(-kWorldHalf, kWorldHalf), isCharacter);
			grid.add(actor.get());
			actors.push_back(std::move(actor));
		}
		return actors;
	}
} // namespace

// 적 탐지 스캔: 몬스터 전원이 1회씩 getCharactersInViewRange 를 돈다(= 스태거링 없는 한 틱 분량).
// range 10 / cellSize 2 → 한 번에 (2*5+1)^2 = 121 셀을 훑는다.
// 인자: (몬스터 수, 캐릭터 수)
static void BM_GridDetectScan(benchmark::State& state)
{
	const int monsters = static_cast<int>(state.range(0));
	const int characters = static_cast<int>(state.range(1));

	GridManager grid(kGridWidth, kGridHeight, kCellSize);
	auto actors = Populate(grid, monsters, characters);

	std::vector<IGridActor*> scratch;
	size_t found = 0;

	for (auto _ : state)
	{
		for (int i = 0; i < monsters; ++i)
		{
			grid.getCharactersInViewRange(actors[i].get(), kDetectRange, scratch);
			found += scratch.size();
		}
		benchmark::DoNotOptimize(found);
	}

	state.counters["monsters"] = monsters;
	state.counters["characters"] = characters;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(monsters));
}
BENCHMARK(BM_GridDetectScan)
	->Args({ 10000, 0 })->Args({ 10000, 50 })->Args({ 10000, 500 })
	->Args({ 40000, 0 })->Args({ 40000, 50 })
	->Unit(benchmark::kMicrosecond);

// 위치 갱신: 셀 경계를 넘는 이동(재등록 발생) — leaveCell/enterCell 이 매번 실행된다.
static void BM_GridMoveCrossCell(benchmark::State& state)
{
	const int monsters = static_cast<int>(state.range(0));

	GridManager grid(kGridWidth, kGridHeight, kCellSize);
	auto actors = Populate(grid, monsters, 0);

	float offset = 0.0f;
	for (auto _ : state)
	{
		// 매 반복 셀 크기만큼 이동시켜 항상 셀이 바뀌게 한다(왕복).
		offset = (offset == 0.0f) ? static_cast<float>(kCellSize) : 0.0f;
		for (int i = 0; i < monsters; ++i)
		{
			BenchActor* a = actors[i].get();
			const float nx = a->GetVector2X() + (offset > 0 ? kCellSize : -kCellSize);
			a->SetPos(nx, a->GetVector2Y());
			grid.move(a, nx, a->GetVector2Y());
		}
	}

	state.counters["monsters"] = monsters;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(monsters));
}
BENCHMARK(BM_GridMoveCrossCell)
	->Arg(10000)->Arg(40000)
	->Unit(benchmark::kMicrosecond);

// 위치 갱신: 같은 셀 안에서의 이동(재등록 없음) — 좌표 계산 + 비교만 한다.
static void BM_GridMoveSameCell(benchmark::State& state)
{
	const int monsters = static_cast<int>(state.range(0));

	GridManager grid(kGridWidth, kGridHeight, kCellSize);
	auto actors = Populate(grid, monsters, 0);

	for (auto _ : state)
	{
		for (int i = 0; i < monsters; ++i)
		{
			BenchActor* a = actors[i].get();
			grid.move(a, a->GetVector2X(), a->GetVector2Y());
		}
	}

	state.counters["monsters"] = monsters;
	state.SetItemsProcessed(state.iterations() * static_cast<int64_t>(monsters));
}
BENCHMARK(BM_GridMoveSameCell)
	->Arg(10000)->Arg(40000)
	->Unit(benchmark::kMicrosecond);

// 스킬 광역 판정: getEntitiesInAoEMask(원형/부채꼴) 1회 비용.
// 인자: (월드 내 몬스터 수, 반경)
static void BM_GridAoEQuery(benchmark::State& state)
{
	const int monsters = static_cast<int>(state.range(0));
	const float radius = static_cast<float>(state.range(1));

	GridManager grid(kGridWidth, kGridHeight, kCellSize);
	auto actors = Populate(grid, monsters, 0);

	Rng rng;
	size_t hit = 0;
	for (auto _ : state)
	{
		const float x = rng.Range(-kWorldHalf, kWorldHalf);
		const float y = rng.Range(-kWorldHalf, kWorldHalf);
		auto result = grid.getEntitiesInAoEMask(x, y, radius, 0.0f);
		hit += result.size();
		benchmark::DoNotOptimize(hit);
	}

	state.counters["monsters"] = monsters;
	state.counters["radius"] = radius;
}
BENCHMARK(BM_GridAoEQuery)
	->Args({ 10000, 5 })->Args({ 10000, 10 })->Args({ 40000, 5 })
	->Unit(benchmark::kMicrosecond);
