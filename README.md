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
