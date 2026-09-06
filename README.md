# Game Server Framework

[한국어](README.ko.md) | English

A sample framework for building and experimenting with game server architecture. This repository combines C# service servers, native C++ game logic and engine libraries, Python data-generation tools, a Unity client, and FlatBuffers/Protobuf protocol definitions in one Visual Studio solution.

The current codebase is organized around Windows and Visual Studio 2022. Most production-style services target .NET Core 3.1, while the native game stack uses MSBuild C++ projects with the v143 toolset.

## At a Glance

| Area | Projects |
| --- | --- |
| Service servers | `Battle`, `Lobby`, `Login`, `Chat`, `Cache`, `IAP`, `RESTful` |
| Operations tools | `GmTool`, `Laboratory`, `ReadOnlyAnalyzerLib` |
| Native game stack | `Game`, `Engine`, `BehaviorTree`, `FiniteStateMachine`, `GameDataProtobuf`, `recastnavigation` |
| Data tooling | `GameData`, `GameDataFlow`, `SqlCodeGenerator`, `Models`, `Schemas` |
| Protocols | `flatbuffer`, `protos` |
| Client | `Client` Unity project |

## Repository Layout

### C# Services

| Path | Purpose | Runtime |
| --- | --- | --- |
| [Battle/](Battle/README.md) | In-game movement, skill, and state synchronization server | .NET Core 3.1 |
| [Lobby/](Lobby/README.md) | Lobby, matching, sessions, OAuth, and game result handling | .NET Core 3.1 |
| [Login/](Login/README.md) | ASP.NET Core login web/API server | .NET Core 3.1 |
| [Chat/](Chat/README.md) | Chat service server | .NET 5 |
| [Cache/](Cache/README.md) | Redis-backed cache and subscription modules | .NET Core 3.1 |
| [IAP/](IAP/README.md) | Google Play and App Store receipt validation modules | .NET Core 3.1 |
| [GmTool/](GmTool/README.ko.md) | GM and operations tool | .NET Core 3.1 |
| [RESTful/](RESTful/) | External REST API client/integration code | .NET Core 3.1 |

### Native Libraries

| Path | Purpose | Type |
| --- | --- | --- |
| [Game/](Game/README.md) | Actor, player, monster, skill, map, world, DB, Lua, and behavior-tree integration | C++ Application |
| [Engine/](Engine/README.md) | ECS, GridManager, RingBuffer, timestamp, and random utilities | C++ Static Library |
| [BehaviorTree/](BehaviorTree/README.md) | Custom behavior tree implementation and optimized/event variants | C++ Static Library |
| [FiniteStateMachine/](FiniteStateMachine/README.md) | Finite state machine examples and base structure | C++ Static Library |
| [GameDataProtobuf/](GameDataProtobuf/README.md) | Generated game-data Protobuf loader and native bindings | C++ Static Library |
| [recastnavigation/](recastnavigation/README.md) | Recast/Detour navigation mesh and pathfinding code | C++ Static Library |
| [UnitTest/](UnitTest/) | Native tests for engine/data-structure code | C++ Application |
| [Bot/](Bot/README.ko.md) | Multi-threaded load/perf client that logs in and hunts monsters over the real protocol | C++ Application |

### Data, Protocols, and Client

| Path | Purpose |
| --- | --- |
| [GameData/](GameData/README.md) | Source game data such as `item.json`, `skill.json`, `quest.json`, `Map.json`, and `GameMode.json` |
| [GameDataFlow/](GameDataFlow/README.md) | Python pipeline that generates Protobuf data and C++/C# factory code |
| [SqlCodeGenerator/](SqlCodeGenerator/README.md) | XML-schema-based DAO and SQL generator |
| [Models/](Models/README.md) | Shared database models and extension models |
| [flatbuffer/](flatbuffer/README.md) | `syncnet.fbs`, bundled `flatc.exe`, and FlatBuffers 1.12 libraries |
| [protos/](protos/README.md) | gRPC/Protobuf definitions including `chat.proto`, `lobby.proto`, and `dummycontrol.proto` |
| [Client/](Client/README.md) | Unity client project using Unity `2020.3.32f1` |

## Development Environment

- Windows
- Visual Studio 2022 Community
- MSBuild v143 C++ toolset
- C++20 for the main native projects
- .NET Core 3.1, .NET 5, and a small .NET 9 laboratory project
- Unity 2020.3.32f1
- Python 3.x
- Redis and MySQL/MariaDB for server-side runtime configuration

Additional build notes are in [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md).

## Build

Use the Visual Studio MSBuild executable instead of `dotnet msbuild`, because the solution contains C++ projects.

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" GameServerFramework.sln /t:Game /p:Configuration=Debug /p:Platform=x64 /v:minimal
```

Expected output:

```text
x64\Debug\Game.exe
```

Run C# services with `dotnet run` from the repository root:

```powershell
dotnet run --project Battle\Battle.csproj
dotnet run --project Lobby\Lobby.csproj
dotnet run --project Login\Login.csproj
dotnet run --project Chat\Chat.csproj
```

Service configuration lives in files such as `appsettings.json`, `appsettings.Release.json`, `*.service`, and `Credentials/`.

## Core Flows

### Login, Lobby, and Matching

`Login` provides the web/API entry point for authentication. `Lobby` handles sessions, OAuth providers, matching, and game result processing. It references `core`, `GameData`, `GameService`, and `Models`, and uses Redis and database-backed models for state.

### Battle and Runtime Game Logic

`Battle` links shared network code from the Unity client and focuses on in-game input, movement, skill, and state synchronization.

The native runtime logic is in `Game`: `Actor`, `Player`, `Monster`, `Skill`, `World`, `Map`, `NavMap`, `SqlClient`, and `BTDebugManager`. It references `Engine`, `BehaviorTree`, `GameDataProtobuf`, and `recastnavigation`.

### Data Generation

Source data is stored as JSON under [GameData/](GameData/). [GameDataFlow/](GameDataFlow/) generates Python, C++, and C# Protobuf outputs from `gamedata.proto`.

```powershell
cd GameDataFlow
.\build.bat
```

Generated outputs include:

- `GameDataFlow/gamedata_pb2.py`
- `GameDataProtobuf/`
- `Client/Assets/Scripts/GameData/`

### FlatBuffers Sync Schema

Runtime sync messages are defined in [flatbuffer/syncnet.fbs](flatbuffer/syncnet.fbs).

```powershell
cd flatbuffer
.\flatc.bat
```

If direct generation into target folders fails, generate inside `flatbuffer` first and then copy the generated files as described in [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md).

## Performance

### Monster AI: Behavior Tree to ECS

Every monster used to walk its own behavior tree from the root, every tick: 13 heap-allocated nodes per monster, virtual dispatch down the same path whether or not the first condition held. The tree is identical for all monsters, yet it was duplicated per instance.

[MonsterAISystem](Engine/AI/MonsterAISystem.h) replaces the traversal with batch passes over component arrays and is now the default backend (`BTBackend::Ecs`). This tree — one fallback over three conditions — compiles into a lookup table from a 3-bit condition mask to an action, so a tick becomes: scan the schedule, evaluate conditions over progressively narrower sets, index the table into per-action buckets, then run one loop per bucket. The two earlier backends are untouched and still selectable.

Four decisions carry the gain:

- **Hot/cold component split.** Only a 4-byte `AIScheduleComponent` array is swept every tick (160 KB at 40,000 monsters). The 64-byte agent component is touched only by entities whose turn it is.
- **Patrolling monsters think once per 10 ticks.** The expensive detection scan already ran at that interval; now everything else does too. Engaged or dying monsters still think every tick, and damage wakes an agent immediately.
- **Simulation time, not `steady_clock::now()`.** On Windows that call is QPC, and per-monster it becomes tick cost — besides making tests depend on the wall clock.
- **Actor state is written only when it changes**, instead of paying a component lookup every tick to re-assert `Patrol`.

**AI step only** (`BM_BTWorldTickActors`, one run):

| Monsters | behaviortree_cpp | In-house BT | **ECS** |
| --- | --- | --- | --- |
| 1,000 | 1.67 ms | 0.157 ms | **0.054 ms** |
| 10,000 | 49.4 ms | 2.08 ms | **0.630 ms** |

**Full world tick**, 500x500 map with 50 players (`BM_LargeMapBTBackend`, one run):

| Monsters | In-house BT | **ECS** |
| --- | --- | --- |
| 20,000 | 27.5 ms | **12.7 ms** |
| 60,000 | 107 ms | **50.1 ms** |
| 100,000 | — | **90.0 ms** |

Against the 10 Hz budget (100 ms), one thread went from roughly **55,000 to 110,000 monsters**. Cold start fell with it: the first tick after spawning 40,000 monsters went from 2,077 ms to 408 ms, because first-think time is now spread over 10 ticks by `actorId % 10`.

Trade-offs. The `BTDebugManager` viewer attaches only to behaviortree_cpp trees, so inspecting a tree means switching the backend back before monsters spawn. And changing the tree means changing the decision table with it — three conditions is eight rows, and the approach stops paying at around ten.

Measurement note: figures within one table come from a single run and are comparable to each other; across runs the large counts swing up to ±30%. Method, per-backend equivalence tests, and the remaining bottleneck (path generation, now the most expensive part of a patrol-only world) are in [Benchmark/PERFORMANCE.md](Benchmark/PERFORMANCE.md).

### Sending: one thread to a worker pool

At 1,000 players in one world, the frame was **91% send path and 2% world simulation**. A world is pinned to one thread by design — handlers and game logic never run concurrently, so nothing in the world needs a lock — and that one thread was spending its budget assembling and submitting packets.

Profiling the frame end to end ([Map.cpp](Engine/Map/Map.cpp) phase timers plus a per-call kernel micro-benchmark) split that cost in two, and the two halves behave differently:

- **Linear in players — `async_write`.** Each session gets exactly one message per 10 Hz tick, so calls scale as `sessions x tick rate` (11,500/s at 1,000 players) and `batch` sits at 1.00: there is nothing to coalesce. The call costs 13-25 us and **that cost is fixed per call** — a 13.5 KB message costs the same as a 1.7 KB one. Stripping asio away entirely, a raw `WSASend` on a loopback socket is 8-12 us, and a UDP comparison puts only ~2.3 us of that on the TCP stack. Most of it is the socket send path itself, which a real NIC pays too.
- **Quadratic in density — FlatBuffers serialization.** Each viewer's message is built from scratch over the actors in its window, so entries grow as `players x players in view`. Measured between 800 and 1,000 players: `async_write` calls rose 1.27x (linear) while serialized entries rose 1.70x (N^2.4), at a constant 83 ns per entry.

The linear half cannot shrink without lowering the send rate, which is sync quality. The quadratic half can. Both, however, are just work on one thread — and the box has 32 of them.

[`engine::ParallelFor`](ECS/ParallelFor.h) splits [`Map::SendPendingViews`](Engine/Map/Map.cpp) across a worker pool. Three properties matter:

- **One pool per process, not per world.** A world is one thread, so a pool per world would mean 16 worlds x 4 threads on a 16-core box. Worlds share it.
- **The calling thread works too**, as `workerIndex 0`. So zero auxiliary threads degrades to a plain sequential loop, and every path that never starts the pool — unit tests, benchmarks — runs exactly the code it ran before.
- **Chunks are handed out from an atomic cursor**, not partitioned up front. Per-viewer cost varies widely (176-242 entries per message is only the mean), and static partitioning would leave three threads waiting on whoever drew the heavy slice.

Splitting the loop is safe because of what is *not* running, not because the world thread waits — it does not wait, it participates. Within `UpdateGameLogic` there is no `poll()`, so no read or write completion runs; the update phases are sequential; and DB work posts its completion back to the owning `io_context` rather than touching the session from a pool thread. Inside the region the work partitions cleanly: slot to player to session is 1:1:1, so one thread owns a slot's pending view, its session's write queue, and its own scratch buffer. What is shared is read-only (actors — `GetActorInfo` is pure; the player map — leaves are deferred since the reentrancy fix) or already thread-safe (`SendMessagePool` is thread-local and reclaims by `use_count()==1` behind an acquire fence, which holds even when the borrowing and releasing threads differ). The barrier guards the *boundary* — it is why `pending.clear()` sits after the loop — not the inside.

Two things had to change alongside. The send counters were `thread_local`, which was correct only while one worker meant one thread; spread across a pool they would have **silently under-reported**, so they moved to per-`GameServer` atomics that the worker re-aggregates. And `Map::leave` — reachable from `Player::Send` when a send queue overflows — can now arrive from several threads at once, so the deferred-leave list took a mutex.

**Adjacent A/B**, same binary, config toggled, 1,000 players, runs back to back:

| | sim | frame | send phase | over budget | writes/s | entries/s |
| --- | --- | --- | --- | --- | --- | --- |
| Sequential | 443 ms/s | 560 ms/s | 381 ms/s | 4/688 | 11,658 | 2,395,053 |
| **Pool (3 + caller)** | **162 ms/s** | **290 ms/s** | **110 ms/s** | **0/691** | 11,600 | 2,226,366 |

`writes/s` and `entries/s` are within 1% and 7% — the same work was done. On top of it the tick went **2.7x lighter** and stopped overrunning its 100 ms budget. `UpdateSystems`, which was left serial, held at 41 -> 38 ms/s; that it did not move is itself evidence the measurement isolated the right term.

Trade-offs. Total CPU is unchanged — the work moved to other cores, so this buys **one world holding more players**, not more machine capacity; a deployment already running 16 worlds on 16 threads gains nothing. And the barrier costs tail latency: the world thread waits on the slowest chunk, and ping p99 went 119 -> 191 ms while p50 and p95 both improved (17 -> 16, 47 -> 38). No session was dropped in either run.

The bottleneck moved rather than vanished. Of the remaining 290 ms/s, **`PollIo` is now the largest single term at 128 ms/s (44%)** — write completions and inbound packet handling — against 110 ms/s for the send phase it used to dwarf.

Measurement note: figures within one table come from adjacent runs on an idle box; across runs the absolute milliseconds swing up to ±25%, which is why the stable counters (`writes/s`, `entries/s`) are quoted alongside them. Full method, the kernel micro-benchmark, the scaling derivation, and the rejected alternatives are in [Benchmark/PERFORMANCE.md](Benchmark/PERFORMANCE.md) (sections 24-31).

## Key Dependencies

- gRPC / Protocol Buffers
- FlatBuffers 1.12
- Recast/Detour
- Redis / StackExchange.Redis
- Entity Framework Core
- MySQL/MariaDB Connector
- Boost 1.87
- LuaJIT
- nlohmann.json
- spdlog
- Unity Ads, Analytics, Purchasing

## Notes

- The root solution is [GameServerFramework.sln](GameServerFramework.sln).
- Native projects primarily use `Debug|x64` and `Release|x64`.
- When adding generated C# files to Unity, add matching `.meta` files with unique GUIDs.
- Database schema and initialization SQL live under [Schemas/](Schemas/).
- If Git reports dubious ownership in a sandboxed shell, use:

```powershell
git -c safe.directory=D:/projects/GameServerFramework status --short
```
