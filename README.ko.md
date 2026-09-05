# Game Server Framework

한국어 | [English](README.md)

게임 서버 구조를 빠르게 실험하고 확장하기 위한 샘플 프레임워크입니다. C# 서비스 서버, C++ 게임 로직과 엔진 라이브러리, Python 데이터 생성 도구, Unity 클라이언트, FlatBuffers/Protobuf 프로토콜 정의가 한 솔루션 안에 함께 구성되어 있습니다.

현재 코드는 Windows와 Visual Studio 2022를 기준으로 빌드됩니다. 서비스 서버는 주로 .NET Core 3.1을 사용하고, 네이티브 게임 로직은 v143 툴셋 기반의 C++ 프로젝트로 구성되어 있습니다.

## 한눈에 보기

| 영역 | 프로젝트 |
| --- | --- |
| 서비스 서버 | `Battle`, `Lobby`, `Login`, `Chat`, `Cache`, `IAP`, `RESTful` |
| 운영/실험 도구 | `GmTool`, `Laboratory`, `ReadOnlyAnalyzerLib` |
| 네이티브 게임 스택 | `Game`, `Engine`, `BehaviorTree`, `FiniteStateMachine`, `GameDataProtobuf`, `recastnavigation` |
| 데이터 도구 | `GameData`, `GameDataFlow`, `SqlCodeGenerator`, `Models`, `Schemas` |
| 프로토콜 | `flatbuffer`, `protos` |
| 클라이언트 | `Client` Unity 프로젝트 |

## 저장소 구조

### C# 서비스

| 경로 | 역할 | 런타임 |
| --- | --- | --- |
| [Battle/](Battle/README.md) | 인게임 이동, 스킬, 상태 동기화 서버 | .NET Core 3.1 |
| [Lobby/](Lobby/README.md) | 로비, 매칭, 세션, OAuth, 게임 결과 처리 | .NET Core 3.1 |
| [Login/](Login/README.md) | ASP.NET Core 기반 로그인 웹/API 서버 | .NET Core 3.1 |
| [Chat/](Chat/README.md) | 채팅 서비스 서버 | .NET 5 |
| [Cache/](Cache/README.md) | Redis 기반 캐시와 구독 모듈 | .NET Core 3.1 |
| [IAP/](IAP/README.md) | Google Play/App Store 영수증 검증 모듈 | .NET Core 3.1 |
| [GmTool/](GmTool/README.ko.md) | GM/운영 도구 | .NET Core 3.1 |
| [RESTful/](RESTful/) | 외부 REST API 클라이언트와 연동 코드 | .NET Core 3.1 |

### C++ 게임 로직과 라이브러리

| 경로 | 역할 | 형식 |
| --- | --- | --- |
| [Game/](Game/README.md) | Actor, Player, Monster, Skill, Map, World, DB, Lua, Behavior Tree 연동을 포함한 게임 실행 프로젝트 | C++ Application |
| [Engine/](Engine/README.md) | ECS, GridManager, RingBuffer, 시간/랜덤 유틸리티 | C++ Static Library |
| [BehaviorTree/](BehaviorTree/README.md) | 자체 Behavior Tree 구현과 이벤트/최적화 버전 | C++ Static Library |
| [FiniteStateMachine/](FiniteStateMachine/README.md) | FSM 예제와 기본 상태 머신 구조 | C++ Static Library |
| [GameDataProtobuf/](GameDataProtobuf/README.md) | 게임 데이터 Protobuf 로더와 생성 산출물 | C++ Static Library |
| [recastnavigation/](recastnavigation/README.md) | Recast/Detour 기반 네비게이션 메시와 경로 탐색 | C++ Static Library |
| [UnitTest/](UnitTest/) | C++ 엔진/자료구조 테스트 프로젝트 | C++ Application |
| [Bot/](Bot/README.ko.md) | 실제 프로토콜로 접속해 몬스터를 사냥하는 부하/성능 테스트 클라이언트(멀티스레드) | C++ Application |

### 데이터, 프로토콜, 클라이언트

| 경로 | 역할 |
| --- | --- |
| [GameData/](GameData/README.md) | `item.json`, `skill.json`, `quest.json`, `Map.json`, `GameMode.json` 등 원본 게임 데이터 |
| [GameDataFlow/](GameDataFlow/README.md) | JSON 게임 데이터를 Protobuf 바이너리와 C++/C# 팩토리 코드로 변환하는 Python 도구 |
| [SqlCodeGenerator/](SqlCodeGenerator/README.md) | XML 스키마 기반 DAO/SQL 생성 도구 |
| [Models/](Models/README.md) | 서버에서 공유하는 DB 모델과 확장 모델 |
| [flatbuffer/](flatbuffer/README.md) | `syncnet.fbs`, `flatc.exe`, FlatBuffers 1.12 라이브러리와 생성 도구 |
| [protos/](protos/README.md) | `chat.proto`, `lobby.proto`, `dummycontrol.proto` 등 gRPC/Protobuf 정의 |
| [Client/](Client/README.md) | Unity 클라이언트 프로젝트. 현재 에디터 버전은 `2020.3.32f1` |

## 개발 환경

- Windows
- Visual Studio 2022 Community
- MSBuild v143 C++ toolset
- C++20 설정이 적용된 주요 C++ 프로젝트
- .NET Core 3.1, .NET 5, 일부 .NET 9 실험 프로젝트
- Unity 2020.3.32f1
- Python 3.x
- Redis, MySQL/MariaDB 등 서버 실행에 필요한 외부 서비스

빌드 환경 메모와 검증된 명령은 [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md)에 정리되어 있습니다.

## 빌드와 실행

솔루션에는 C++ 프로젝트가 포함되어 있으므로 `dotnet msbuild` 대신 Visual Studio의 MSBuild를 사용합니다.

```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\amd64\MSBuild.exe" GameServerFramework.sln /t:Game /p:Configuration=Debug /p:Platform=x64 /v:minimal
```

대표 출력물:

```text
x64\Debug\Game.exe
```

C# 서버는 루트에서 다음처럼 실행할 수 있습니다.

```powershell
dotnet run --project Battle\Battle.csproj
dotnet run --project Lobby\Lobby.csproj
dotnet run --project Login\Login.csproj
dotnet run --project Chat\Chat.csproj
```

서버 설정은 주로 `appsettings.json`, `appsettings.Release.json`, `*.service`, `Credentials/` 아래의 인증서 파일에 들어 있습니다.

## 핵심 흐름

### 로그인, 로비, 매칭

`Login`은 인증을 위한 웹/API 진입점을 제공합니다. `Lobby`는 사용자 세션, OAuth 외부 인증, 매칭, 게임 결과 처리를 담당합니다. `Lobby`는 `core`, `GameData`, `GameService`, `Models`를 참조하며 Redis와 DB 모델을 함께 사용합니다.

### 전투와 런타임 게임 로직

`Battle`은 Unity 클라이언트의 서버 공용 네트워크 코드를 링크해서 빌드합니다. 인게임 입력, 이동, 스킬, 상태 변경 동기화가 중심입니다.

네이티브 런타임 로직은 `Game` 프로젝트에 모여 있습니다. `Actor`, `Player`, `Monster`, `Skill`, `World`, `Map`, `NavMap`, `SqlClient`, `BTDebugManager` 등이 포함되며 `Engine`, `BehaviorTree`, `GameDataProtobuf`, `recastnavigation`을 참조합니다.

### 공간 처리와 AI

`Engine`은 `GridManager`, ECS, RingBuffer 같은 서버 사이드 자료구조와 유틸리티를 제공합니다. `BehaviorTree`와 `FiniteStateMachine`은 AI 상태 전이와 행동 제어를 실험하기 위한 C++ 라이브러리입니다.

### 데이터 생성

원본 데이터는 [GameData/](GameData/)의 JSON 파일에서 관리합니다. [GameDataFlow/](GameDataFlow/)는 `gamedata.proto`를 기준으로 Python, C++, C# Protobuf 산출물을 생성합니다.

```powershell
cd GameDataFlow
.\build.bat
```

생성되는 주요 산출물:

- `GameDataFlow/gamedata_pb2.py`
- `GameDataProtobuf/`
- `Client/Assets/Scripts/GameData/`

### FlatBuffers 동기화 스키마

런타임 동기화 메시지는 [flatbuffer/syncnet.fbs](flatbuffer/syncnet.fbs)에 정의되어 있습니다.

```powershell
cd flatbuffer
.\flatc.bat
```

일부 환경에서는 대상 폴더로 직접 생성하는 과정이 실패할 수 있습니다. 이 경우 [BUILD_ENVIRONMENT.md](BUILD_ENVIRONMENT.md)의 절차처럼 `flatbuffer` 폴더에 먼저 생성한 뒤 `Game/`과 `Client/Assets/Scripts/flatbuffers/`로 복사합니다.

## 성능

### 몬스터 AI — 비헤이비어 트리에서 ECS로

예전에는 몬스터 한 마리마다 노드 객체 13개를 힙에 만들고, 매 틱 그 트리를 뿌리부터 가상 호출로 타고 내려갔습니다. 첫 조건이 실패해도 그 사실을 알기까지 같은 경로를 다 지나갑니다. 트리 구조는 모든 몬스터가 똑같은데 마리 수만큼 복제된 셈입니다.

[MonsterAISystem](Engine/AI/MonsterAISystem.h)은 이 순회를 컴포넌트 배열에 대한 배치 패스로 바꾼 세 번째 백엔드이고, 지금은 기본값입니다(`BTBackend::Ecs`). 이 트리는 Fallback 하나에 조건이 셋뿐이라 **조건 비트 3개에서 행동으로 가는 표 하나로 컴파일됩니다.** 그래서 한 틱이 이렇게 됩니다 — 스케줄 배열을 훑어 이번에 사고할 개체를 고르고, 조건을 점점 좁아지는 무리에만 평가하고, 조건 비트로 표를 찾아 행동별 버킷에 담고, 버킷마다 한 가지 일만 하는 루프를 돌립니다. 기존 두 백엔드는 그대로 두었고 여전히 선택할 수 있습니다.

성능의 근거가 되는 선택은 넷입니다.

- **컴포넌트를 뜨거운 것과 차가운 것으로 쪼갰습니다.** 매 틱 전수 순회하는 것은 4바이트짜리 `AIScheduleComponent` 배열뿐입니다(40,000마리라도 160KB). 64바이트 에이전트 컴포넌트는 사고할 차례가 된 개체만 만집니다.
- **배회 중에는 10틱에 한 번만 사고합니다.** 가장 비싼 적 탐지 스캔은 예전에도 10틱마다 걸렀지만 나머지 노드는 매 틱 다 돌았습니다. 이제 그 주기가 사고 전체의 주기입니다. 교전 중이거나 사망 처리 중이면 예전처럼 매 틱 사고하고, 피격은 즉시 깨웁니다.
- **벽시계를 버렸습니다.** 휴식과 타임아웃이 전부 시뮬레이션 시간 비교입니다. `steady_clock::now()`는 윈도우에서 QPC라 마리마다 부르면 그대로 틱 비용이 되고, 무엇보다 테스트가 벽시계에 의존하게 됩니다.
- **상태는 바뀔 때만 씁니다.** 예전 트리는 배회 중에도 매 틱 `SetState(Patrol)`을 불러 컴포넌트 해시 조회를 지불했습니다.

**AI 단계만** (`BM_BTWorldTickActors`, 같은 실행):

| 몬스터 | behaviortree_cpp | 인하우스 BT | **ECS** |
| --- | --- | --- | --- |
| 1,000 | 1.67 ms | 0.157 ms | **0.054 ms** |
| 10,000 | 49.4 ms | 2.08 ms | **0.630 ms** |

**월드 전체 틱**, 500×500 맵에 플레이어 50명 (`BM_LargeMapBTBackend`, 같은 실행):

| 몬스터 | 인하우스 BT | **ECS** |
| --- | --- | --- |
| 20,000 | 27.5 ms | **12.7 ms** |
| 60,000 | 107 ms | **50.1 ms** |
| 100,000 | — | **90.0 ms** |

10Hz 예산(100ms) 기준으로 **한 스레드 수용량이 약 55,000마리에서 약 110,000마리가 됐습니다.** 콜드 스타트도 함께 줄었습니다 — 40,000마리를 스폰한 직후 첫 틱이 2,077ms에서 408ms가 됐는데, 첫 사고 시점이 `actorId % 10`으로 흩어져 그 비용이 10틱에 나뉘기 때문입니다.

대가도 있습니다. BT 디버그 뷰어(`BTDebugManager`)는 behaviortree_cpp 트리에만 붙으므로, 트리를 들여다보려면 몬스터 스폰 **전에** 백엔드를 되돌려야 합니다. 그리고 트리 구조를 바꾸면 결정표도 함께 고쳐야 합니다 — 지금은 조건 셋에 8칸이고, 조건이 열 개쯤 되면 이 방식은 더 이상 이득이 아닙니다.

측정 주의: 한 표 안의 값은 **같은 실행에서** 잰 것이라 서로 비교할 수 있습니다. 실행을 나누면 큰 마리 수에서 ±30%까지 흔들립니다. 측정 방법, 백엔드 간 동작 일치 테스트, 남은 병목(이제 배회 전용 구성에서 제일 비싼 것은 AI가 아니라 경로 산출입니다)은 [Benchmark/PERFORMANCE.md](Benchmark/PERFORMANCE.md)에 있습니다.

## 주요 의존성

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

## 작업 메모

- 루트 솔루션 파일은 [GameServerFramework.sln](GameServerFramework.sln)입니다.
- C++ 프로젝트는 주로 `Debug|x64`, `Release|x64` 구성을 사용합니다.
- Unity 생성 C# 파일을 추가할 때는 고유 GUID를 가진 `.meta` 파일도 함께 추가해야 합니다.
- DB 스키마와 초기화 SQL은 [Schemas/](Schemas/) 아래에 있습니다.
- 샌드박스 계정에서 `git status`가 dubious ownership 오류를 내면 다음 형식으로 확인할 수 있습니다.

```powershell
git -c safe.directory=D:/projects/GameServerFramework status --short
```
