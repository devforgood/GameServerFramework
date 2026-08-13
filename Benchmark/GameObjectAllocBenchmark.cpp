// GameObject spawn/despawn allocation cost: default CRT allocator vs mimalloc.
//
// Both paths coexist in one binary by selecting the allocator explicitly
// (std::allocator vs mi_stl_allocator) instead of overriding global new/delete.
// This models the current pattern: 1 object alloc (allocate_shared) + the
// components_ vector buffer + one alloc per Component, all routed through the
// chosen allocator.
//
// NOTE: build & run in Release/x64 for meaningful numbers. mimalloc must be
// compiled with NDEBUG / MI_DEBUG=0 (see Benchmark.vcxproj), otherwise it runs
// in debug mode (guard pages, fill patterns) and is far slower.

#include <benchmark/benchmark.h>
#include <mimalloc.h>

#include <memory>
#include <vector>
#include <string>
#include <random>
#include <cstdint>

namespace {

// ---- Component model (mirrors Engine/GameObject/Component.h) ----
struct Component {
    void* game_object = nullptr;
    bool dirty_ = false;
    virtual ~Component() = default;
    virtual void Update(float) {}
};
struct PosComp    : Component { float x = 0, y = 0, z = 0; void Update(float) override {} };
struct HealthComp : Component { int hp = 100, max = 100;   void Update(float) override {} };
struct StateComp  : Component { int state = 0; float t = 0; void Update(float) override {} };
struct InputComp  : Component { bool locked = false;        void Update(float) override {} };

// ---- Allocation policies -------------------------------------------------
struct DefaultPolicy {
    template <class T> using Alloc = std::allocator<T>;
    template <class T, class... A> static T* make(A&&... a) { return new T(std::forward<A>(a)...); }
    static void destroy(Component* c) { delete c; }
};

struct MimallocPolicy {
    template <class T> using Alloc = mi_stl_allocator<T>;
    template <class T, class... A> static T* make(A&&... a) {
        void* p = mi_malloc(sizeof(T));
        return new (p) T(std::forward<A>(a)...);
    }
    static void destroy(Component* c) { c->~Component(); mi_free(c); }
};

// ---- GameObject model (mirrors GameObject + Actor + Monster member bulk) ----
template <class P>
struct GameObjectT {
    struct CompDeleter { void operator()(Component* c) const { P::destroy(c); } };
    using CompPtr = std::unique_ptr<Component, CompDeleter>;

    std::string name_;
    std::vector<CompPtr, typename P::template Alloc<CompPtr>> components_;
    int actorId_ = -1; bool inputLocked_ = false; float frontVector_[3]{0, 0, 1};
    float rotationSpeed_ = 5.0f; float position_[3]{0, 0, 0}; int health_ = 100;
    int lastAttacker_ = -1; int gameObjectType_ = 0; std::int32_t entityId_ = -1;
    void* map_ = nullptr; int aiState_ = 0; long changeFlag_ = 0;
    int gridX = -1, gridY = -1; float speed = 0; void* bt_ = nullptr;
    float spawnPos_[3]{0, 0, 0}; std::uint64_t spawnRef_ = 0; void* tree_ = nullptr;
    bool deadNotified_ = false; int targetActorId_ = 0; std::string monsterName_;

    GameObjectT() { components_.reserve(16); }
    virtual ~GameObjectT() = default;
    void Update(float dt) { for (auto& c : components_) c->Update(dt); }
};

template <class P>
std::shared_ptr<GameObjectT<P>> Spawn() {
    using GO = GameObjectT<P>;
    auto go = std::allocate_shared<GO>(typename P::template Alloc<GO>{});
    go->components_.emplace_back(typename GO::CompPtr(P::template make<PosComp>()));
    go->components_.emplace_back(typename GO::CompPtr(P::template make<HealthComp>()));
    go->components_.emplace_back(typename GO::CompPtr(P::template make<StateComp>()));
    go->components_.emplace_back(typename GO::CompPtr(P::template make<InputComp>()));
    return go;
}

// ---- Workload 1: tight spawn + immediate despawn -------------------------
template <class P>
void BM_Spawn_Tight(benchmark::State& state) {
    for (auto _ : state) {
        auto go = Spawn<P>();
        go->Update(0.016f);
        benchmark::DoNotOptimize(go.get());
    }
    state.SetItemsProcessed(state.iterations());
}

// ---- Workload 2: steady-state churn with a live set ----------------------
// Each iteration frees one live object and spawns a replacement. This exposes
// allocator fragmentation / free-list behavior, which is closer to a running
// server where monsters die and spawn while thousands remain alive.
template <class P>
void BM_Spawn_Churn(benchmark::State& state) {
    const int live = static_cast<int>(state.range(0));
    std::vector<std::shared_ptr<GameObjectT<P>>> pool;
    pool.reserve(live);
    for (int i = 0; i < live; ++i) pool.push_back(Spawn<P>());

    std::mt19937 rng(12345);
    std::uniform_int_distribution<int> pick(0, live - 1);

    for (auto _ : state) {
        int idx = pick(rng);
        pool[idx] = Spawn<P>();
        pool[idx]->Update(0.016f);
        benchmark::DoNotOptimize(pool[idx].get());
    }
    state.SetItemsProcessed(state.iterations());
}

} // namespace

BENCHMARK_TEMPLATE(BM_Spawn_Tight, DefaultPolicy);
BENCHMARK_TEMPLATE(BM_Spawn_Tight, MimallocPolicy);

BENCHMARK_TEMPLATE(BM_Spawn_Churn, DefaultPolicy)->Arg(1000)->Arg(4000)->Arg(16000);
BENCHMARK_TEMPLATE(BM_Spawn_Churn, MimallocPolicy)->Arg(1000)->Arg(4000)->Arg(16000);
