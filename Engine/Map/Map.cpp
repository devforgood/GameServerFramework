#include "Map.h"
#include "SendMessagePool.h"
#include "syncnet_generated.h"
#include <algorithm>
#include <iostream>
#include "Server.h"
#include "Monster.h"
#include "Character.h"
#include "NonPlayerCharacter.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "Player.h"
#include "PlayerEventBroker.h"
#include "ActorFactory.h"
#include "Common.h"
#include "ComponentRegistry.h"
#include "MonsterAISystem.h"
#include "NavMesh.h"
#include "INavMovement.h"
#include "NavMovementFactory.h"
#include "BTDebugSync.h"
#include "GameMode.h"
#include "GameModeFactory.h"
#include "GridManager.h"
#include "World.h"


//const float g_fDistance = std::powf(10.0f, 2);
const float g_fDistance = 10.0f;

// 게임 모드가 부활 시간을 정하지 않았을 때 쓰는 기본값(초).
// (부활 시 체력은 캐릭터의 최대 체력으로 회복한다 — Map::RespawnPlayer)
static constexpr float kDefaultRespawnSeconds = 5.0f;

Map::Map(World* world)
{
	world_ = world;
	navMesh_ = nullptr;
	movement_ = nullptr;
	gridManager_ = nullptr;

	systemManager_ = nullptr;
}

Map::~Map()
{
	// 살아있는 액터를 먼저 정리한다. Actor 소멸자(Actor::Clear)가 systemManager_ 를
	// 역참조하므로, systemManager_ 보다 먼저 파괴되어야 댕글링 포인터 접근을 막는다.
	//
	// 액터 목록만 비우면 부족하다 — 남아 있는 플레이어가 자기 캐릭터를 shared_ptr 로
	// 붙들고 있어서 그 액터는 여기서 죽지 않고 systemManager_ 보다 오래 살아남는다.
	// (플레이어가 접속한 채로 서버를 내리면 종료 시 크래시하는 경로였다.)
	for (auto& entry : players_)
	{
		if (entry.second != nullptr)
			entry.second->UnPossess();
	}
	players_.clear();

	actorMap_.clear();
	actorList_.clear();

	if (movement_)
	{
		delete movement_;
		movement_ = nullptr;
	}
	if (navMesh_)
	{
		delete navMesh_;
		navMesh_ = nullptr;
	}
	if (gridManager_)
	{
		delete gridManager_;
		gridManager_ = nullptr;
	}

	if (systemManager_)
	{
		delete systemManager_;
		systemManager_ = nullptr;
	}
}

bool Map::Init(const std::string& movementType, const gamedata::Map* mapData)
{
	mapData_ = mapData;

	// 게임 모드를 먼저 만든다 — 이동 전략(movement)을 여기서 읽는다.
	if (mapData_ != nullptr)
		gameMode_.reset(GameModeFactory::Create(mapData_->game_mode_id));

	// 이동 전략은 맵이 속한 게임 모드가 정한다(모드마다 다를 수 있다). 호출 측이
	// 명시하면(벤치마크/테스트) 그쪽이 우선하고, 둘 다 없으면 NavMovementFactory 가
	// crowd 로 폴백한다.
	std::string resolvedMovement = movementType;
	if (resolvedMovement.empty() && gameMode_ != nullptr && gameMode_->gamedata != nullptr)
		resolvedMovement = gameMode_->gamedata->movement;

	if (!InitNavigation(resolvedMovement))
		return false;

	gridManager_ = CreateGrid();
	sendMessageBuilder_ = SendMessagePool::Acquire();

	RefreshAoIMode(); // 맵 데이터의 aoi_radius 로 관심영역 사용 여부를 정한다

	InitEcs();

	// 진행 로직(lua) 시작. on_start 가 보스를 스폰할 수 있으므로 그리드/ECS 가 준비된 뒤에 부른다.
	if (gameMode_ != nullptr)
	{
		gameMode_->SetMap(this);
		gameMode_->LoadScript();
		gameMode_->Start();
	}
	return true;
}

// 공간 분할 셀 크기(월드 단위). 적 탐지 반경(g_fDistance=10)이 셀 5칸이 되도록 잡혀 있다.
static constexpr int kGridCellSize = 2;

// 그리드가 담지 못하는 좌표는 어떤 셀에도 들어가지 않는다(enterCell 이 조용히 무시한다).
// 그러면 그 액터는 적 탐지·AoI 구독·스킬 AoE 판정에서 전부 빠지므로,
// 크기는 반드시 네비메시의 실제 범위에서 뽑는다.
//
// Map.json 의 size 는 쓰지 않는다 — 툴이 씬 경계에서 채우는 메타데이터이고 엔진이 참조한 적이
// 없어서, 실제 지오메트리가 50유닛인 맵에 1000 이 적혀 있는 식으로 어긋나 있다.
GridManager* Map::CreateGrid() const
{
	// 네비메시를 못 읽는 경우(테스트 등)를 위한 기본값. 원점 기준 ±100 을 담는다.
	constexpr int kFallbackCells = 100;

	float bmin[3], bmax[3];
	if (navMesh_ == nullptr || !navMesh_->Bounds(bmin, bmax))
	{
		LOG.warn("Map {} navmesh 경계를 얻지 못해 기본 그리드({}x{}, 셀 {})를 사용합니다.",
			mapData_ != nullptr ? mapData_->id : 0, kFallbackCells, kFallbackCells, kGridCellSize);
		return new GridManager(kFallbackCells, kFallbackCells, kGridCellSize);
	}

	// 가장자리에 선 액터가 밀려나도 셀 밖으로 나가지 않도록 한 칸씩 여유를 둔다.
	//
	// 원점은 반드시 셀 크기의 배수로 내림한다. 네비메시 경계를 그대로 쓰면(예: -28.3)
	// 셀 경계가 소수점에 걸려, 정수 좌표에 서 있는 액터가 부동소수 오차로 매 틱 셀을 넘나들며
	// leave+enter 를 반복한다. 실제로 10,000마리 틱이 6.8ms → 7.9ms 로 느려졌다.
	// 배수로 맞추면 (좌표 - 원점) 이 이진수로 정확히 떨어져 이 흔들림이 사라진다.
	const float margin = static_cast<float>(kGridCellSize);
	const float originX = std::floor((bmin[0] - margin) / kGridCellSize) * kGridCellSize;
	const float originZ = std::floor((bmin[2] - margin) / kGridCellSize) * kGridCellSize;
	const int width = static_cast<int>(std::ceil((bmax[0] + margin - originX) / kGridCellSize));
	const int height = static_cast<int>(std::ceil((bmax[2] + margin - originZ) / kGridCellSize));

	LOG.info("Map {} 그리드 {}x{} 셀(셀 크기 {}), 원점 ({:.1f}, {:.1f}), navmesh 범위 x[{:.1f}, {:.1f}] z[{:.1f}, {:.1f}]",
		mapData_ != nullptr ? mapData_->id : 0, width, height, kGridCellSize,
		originX, originZ, bmin[0], bmax[0], bmin[2], bmax[2]);

	return new GridManager(width, height, kGridCellSize, originX, originZ);
}

bool Map::InitNavigation(const std::string& movementType)
{
	// 맵별 네비메시를 반드시 로드한다. 경로가 지정되지 않았거나 로드에 실패하면
	// 과거처럼 solo_navmesh.bin 으로 폴백하면 씬 지오메트리와 형상이 어긋나므로,
	// 폴백하지 않고 에러로 처리한다(호출자가 해당 맵 로드를 중단하도록 false 반환).
	navMesh_ = new NavMesh();
	if (mapData_ == nullptr || mapData_->navmesh_path.empty())
	{
		LOG.error("Map {} navmesh 경로가 지정되지 않았습니다. 맵 로드를 중단합니다.",
			mapData_ != nullptr ? mapData_->id : 0);
		return false;
	}
	if (!navMesh_->Load((GameDataPath::Resolve() + mapData_->navmesh_path).c_str()))
	{
		LOG.error("Map {} navmesh '{}' 로드에 실패했습니다. 맵 로드를 중단합니다.",
			mapData_->id, mapData_->navmesh_path);
		return false;
	}

	// 씬(Map.json) 좌표와 navmesh 정합 검증. 어긋나면 경고만 남긴다(로드는 계속).
	ValidateMapDataOnNavMesh();

	movement_ = NavMovementFactory::Create(movementType, navMesh_);
	movement_->Init();
	return true;
}

void Map::InitEcs()
{
	systemManager_ = new engine::SystemManager();

	// 컴포넌트 타입 목록은 ECS 모듈이 소유한다(새 컴포넌트 추가 시 Map 은 수정 불필요).
	engine::RegisterGameComponents(systemManager_->GetEntityManager());

	// 몬스터 AI 컴포넌트와 그것을 도는 시스템. AI 는 이동 시뮬레이션보다 먼저 돌아야 해서
	// systemManager_ 의 시스템 목록(단계 3)이 아니라 UpdateActors(단계 1)에서 직접 돌린다.
	monsterai::RegisterComponents(systemManager_->GetEntityManager());
	aiSystem_ = std::make_unique<monsterai::MonsterAISystem>(this, systemManager_->GetEntityManager());

	systemManager_->RegisterSystem<engine::TimerComponent>(
		[](float deltaTime, engine::TimerComponent& timer) {
			engine::TimerSystem::Update(deltaTime, timer);
		});

	systemManager_->RegisterSystem<engine::StateComponent, engine::PositionComponent>(
		[this](float, engine::StateComponent& state, engine::PositionComponent& position) {
			SyncActorState(state, position);
		});
}

// ── 관심영역(AoI) ── (설계 배경은 AOI_DESIGN.md)

// 관심영역 반경. 0 이하면 AoI 를 쓰지 않고 맵 전체를 한 메시지로 브로드캐스트한다.
//
// AoI 는 CPU 를 대역폭과 맞바꾸는 기법이다. 플레이어마다 메시지를 따로 조립하므로
// 시야가 겹치는 액터는 사람 수만큼 중복 직렬화된다. 맵이 시야 반경보다 크지 않으면
// 어차피 모두가 모두를 보므로, 걸러지는 것 없이 중복 비용만 남는다(측정치는 PERFORMANCE.md).
// 그래서 반경은 맵 크기에 맞춰 데이터로 정하고, 작은 맵은 0(브로드캐스트)으로 둔다.
// AoI 상태는 쓰는 맵에서만 할당한다(Map 의 멤버 배치를 건드리지 않기 위해 별도 객체).
//
// 뷰어는 '슬롯'(연속 배열의 인덱스)으로 다룬다. 셀 구독자 목록에도 이 슬롯 번호가 들어가므로,
// 액터 변경을 구독자에게 전달할 때 해시 조회 없이 배열 색인 한 번으로 끝난다.
// (매 틱 '변경 액터 수 × 구독자 수' 만큼 반복되는 자리라 조회 비용이 그대로 틱 비용이 된다.)
struct Map::AoIState
{
	// 한 틱 동안 그 플레이어에게 보낼 것들.
	// enter 는 전체 스냅샷, update 는 변경분, leave 는 시야에서 빠진 actorId.
	struct PendingView
	{
		std::vector<Actor*> enters;
		std::vector<Actor*> updates;
		std::vector<int> leaves;

		bool empty() const { return enters.empty() && updates.empty() && leaves.empty(); }
		void clear() { enters.clear(); updates.clear(); leaves.clear(); }
	};

	struct Slot
	{
		long playerId = -1;
		int cellX = 0;
		int cellY = 0;
		int cellRadius = 0;
		bool active = false;
		PendingView pending;
	};

	std::vector<Slot> slots;
	std::unordered_map<long, int> slotOf;   // playerId → 슬롯(구독/해제 때만 쓴다)
	std::vector<int> freeSlots;
	std::vector<IGridActor*> scratch;       // 셀 순회용 임시 버퍼(호출당 힙 할당 방지)
	float radiusOverride = 0.0f;            // 0 이하면 맵 데이터 값

	// 슬롯 하나의 메시지를 조립하는 동안만 쓰는 버퍼. 슬롯마다 지역 변수로 두면
	// 매 틱 '접속자 수' 만큼 힙 할당이 생긴다(조립이 끝나면 곧바로 버려지는데도).
	// 한 슬롯을 다 조립한 뒤에야 다음 슬롯으로 넘어가므로 하나면 충분하다.
	std::vector<flatbuffers::Offset<syncnet::ActorInfo>> infoScratch;

	// 액터가 셀을 넘을 때 '옛 셀 구독자'와 '새 셀 구독자'의 차집합을 구하는 표식 배열.
	// 슬롯 번호로 색인하고, 값이 이번 호출의 스탬프와 같으면 그 목록에 들어 있다는 뜻이다.
	// 스탬프를 매번 올리므로 호출마다 배열을 비울 필요가 없다.
	std::vector<uint64_t> cellMark;
	uint64_t cellStamp = 0;

	int AcquireSlot(long playerId)
	{
		auto itr = slotOf.find(playerId);
		if (itr != slotOf.end())
			return itr->second;

		int index;
		if (!freeSlots.empty())
		{
			index = freeSlots.back();
			freeSlots.pop_back();
			slots[index] = Slot();
		}
		else
		{
			index = static_cast<int>(slots.size());
			slots.emplace_back();
		}

		slots[index].playerId = playerId;
		slots[index].active = true;
		slotOf[playerId] = index;
		return index;
	}

	int FindSlot(long playerId) const
	{
		auto itr = slotOf.find(playerId);
		return itr != slotOf.end() ? itr->second : -1;
	}

	void ReleaseSlot(long playerId)
	{
		auto itr = slotOf.find(playerId);
		if (itr == slotOf.end())
			return;

		const int index = itr->second;
		slots[index] = Slot();
		freeSlots.push_back(index);
		slotOf.erase(itr);
	}
};

float Map::AoIRadius() const
{
	if (aoi_ != nullptr && aoi_->radiusOverride > 0.0f)
		return aoi_->radiusOverride;
	if (mapData_ != nullptr)
		return static_cast<float>(mapData_->aoi_radius);
	return 0.0f;
}

// 반경/데이터가 바뀌면 켜짐 여부를 다시 계산한다. 매 액터마다 확인하지 않도록 bool 로 캐시한다.
//
// 반경 override(SetAoIRadius)는 "지금 켜라"는 뜻이라 인구와 무관하게 그대로 켠다
// (벤치마크/단위 테스트가 소수의 플레이어로 관심영역 동작을 재현하는 통로다).
// 데이터(aoi_radius)는 "이 맵은 이 반경으로 쓸 수 있다"까지고, 실제로 켜는 것은 인구다.
void Map::RefreshAoIMode()
{
	if (aoi_ != nullptr && aoi_->radiusOverride > 0.0f)
	{
		aoiEnabled_ = true;
		return;
	}

	UpdateAoIMode();
}

void Map::SetAoIRadius(float radius)
{
	if (aoi_ == nullptr)
		aoi_ = std::make_unique<AoIState>();
	aoi_->radiusOverride = radius;
	RefreshAoIMode();
}

//---------------------------------------------------------------------------------------
// 인구에 따라 브로드캐스트 ↔ 관심영역을 오간다.
//
// 둘 중 어느 쪽이 싼지는 인구가 정한다(측정치는 PERFORMANCE.md 17절).
//   - 사람이 적으면 브로드캐스트가 싸다. 메시지를 한 번 만들어 전원이 같은 버퍼를 공유한다.
//   - 사람이 많으면 관심영역이 싸다. 뷰어마다 메시지를 따로 만드는 대신, 보내는 양이
//     '전원 × 전원' 에서 '시야 안 × 그 자리를 보는 사람' 으로 떨어진다.
// 봇 부하 측정에서 교차점은 600~800명 사이였다(800명에서 브로드캐스트는 p99 866ms 에
// drop 이 나고, 관심영역은 484ms 에 drop 이 없다. 600명에서는 반대다).
//
// 그래서 데이터로 한쪽을 고정하면 어느 구간에서든 한 번은 손해를 본다. 여기서 인구를 보고
// 바꾼다. 경계에서 오가지 않도록 켜는 값과 끄는 값을 벌려 둔다(히스테리시스).
//---------------------------------------------------------------------------------------
void Map::UpdateAoIMode()
{
	if (aoi_ != nullptr && aoi_->radiusOverride > 0.0f)
		return; // 명시적으로 켜 둔 상태다 — 인구로 끄지 않는다.

	// 데이터가 반경을 주지 않은 맵은 관심영역을 쓰지 않는다.
	if (mapData_ == nullptr || mapData_->aoi_radius <= 0.0)
	{
		if (aoiEnabled_)
			DisableAoI();
		return;
	}

	const size_t playerCount = players_.size();
	if (!aoiEnabled_ && playerCount >= kAoIEnablePlayers)
		EnableAoI();
	else if (aoiEnabled_ && playerCount < kAoIDisablePlayers)
		DisableAoI();
}

// 켜는 순간 이미 들어와 있는 플레이어 전원을 구독시킨다. 구독과 함께 스냅샷(enter)이
// 큐잉되므로, 전환 직후 한 틱은 브로드캐스트 한 틱과 같은 양이 나간다(그 뒤로 줄어든다).
void Map::EnableAoI()
{
	if (aoi_ == nullptr)
		aoi_ = std::make_unique<AoIState>();

	aoiEnabled_ = true;

	for (auto& entry : players_)
	{
		if (entry.second == nullptr)
			continue;
		auto character = entry.second->GetCharacter();
		if (character == nullptr)
			continue;

		SubscribeViewer(entry.first, character.get(), /*sendSnapshot=*/true);
	}

	LOG.info("Map {} 관심영역 켬 (플레이어 {}명, 반경 {:.1f})", GetMapId(), players_.size(), AoIRadius());
}

// 끌 때는 구독을 걷어내는 것만으로 부족하다. 관심영역에 걸러져 클라이언트가 아직 모르는
// 액터가 있는데, 브로드캐스트는 '이번 틱에 바뀐 것' 만 보내므로 가만히 있는 액터는
// 영영 전달되지 않는다. 그래서 전원을 변경됨으로 세워 다음 한 틱을 전체 스냅샷으로 만든다.
void Map::DisableAoI()
{
	aoiEnabled_ = false;

	if (aoi_ != nullptr)
	{
		std::vector<long> viewers;
		viewers.reserve(aoi_->slotOf.size());
		for (const auto& entry : aoi_->slotOf)
			viewers.push_back(entry.first);

		for (const long playerId : viewers)
			UnsubscribeViewer(playerId);

		for (AoIState::Slot& slot : aoi_->slots)
			slot.pending.clear();
	}

	for (auto& actor : actorList_)
	{
		if (actor != nullptr)
			actor->AddChangedFlag(static_cast<long>(GameObjectChangeType::All));
	}

	LOG.info("Map {} 관심영역 끔 (플레이어 {}명)", GetMapId(), players_.size());
}

bool Map::IsInViewOf(long playerId, int actorId)
{
	if (aoi_ == nullptr)
		return false;

	const int slot = aoi_->FindSlot(playerId);
	if (slot < 0)
		return false;

	auto actor = FindActor(actorId);
	if (actor == nullptr)
		return false;

	for (int subscriber : gridManager_->SubscribersOf(actor->GetGridX(), actor->GetGridY()))
	{
		if (subscriber == slot)
			return true;
	}
	return false;
}

// 캐릭터 주변 셀을 구독하고, 필요하면 그 안의 액터들을 enter 로 큐잉한다(입장 스냅샷).
void Map::SubscribeViewer(long playerId, Actor* character, bool sendSnapshot)
{
	if (character == nullptr || aoi_ == nullptr)
		return;

	const int slotIndex = aoi_->AcquireSlot(playerId);
	AoIState::Slot& slot = aoi_->slots[slotIndex];

	const auto [cellX, cellY] = gridManager_->CellOf(character->GetVecter2X(), character->GetVecter2Y());
	const int radius = gridManager_->CellRadius(AoIRadius());

	slot.cellX = cellX;
	slot.cellY = cellY;
	slot.cellRadius = radius;

	for (int dx = -radius; dx <= radius; ++dx)
	{
		for (int dy = -radius; dy <= radius; ++dy)
		{
			const int cx = cellX + dx;
			const int cy = cellY + dy;
			gridManager_->Subscribe(cx, cy, slotIndex);

			if (!sendSnapshot)
				continue;

			aoi_->scratch.clear();
			gridManager_->CollectActorsInCell(cx, cy, aoi_->scratch);
			for (IGridActor* entity : aoi_->scratch)
				aoi_->slots[slotIndex].pending.enters.push_back(static_cast<Actor*>(entity));
		}
	}
}

void Map::UnsubscribeViewer(long playerId)
{
	if (aoi_ == nullptr)
		return;

	const int slotIndex = aoi_->FindSlot(playerId);
	if (slotIndex < 0)
		return;

	const AoIState::Slot& slot = aoi_->slots[slotIndex];
	for (int dx = -slot.cellRadius; dx <= slot.cellRadius; ++dx)
		for (int dy = -slot.cellRadius; dy <= slot.cellRadius; ++dy)
			gridManager_->Unsubscribe(slot.cellX + dx, slot.cellY + dy, slotIndex);

	aoi_->ReleaseSlot(playerId);
}

// 플레이어 캐릭터가 셀을 옮겼다. 새로 들어온 셀의 액터는 enter, 빠진 셀의 액터는 leave 다.
void Map::OnViewerCellChanged(long playerId, Actor* character)
{
	const int slotIndex = aoi_->FindSlot(playerId);
	if (slotIndex < 0)
		return;

	AoIState::Slot& slot = aoi_->slots[slotIndex];
	const auto [newX, newY] = gridManager_->CellOf(character->GetVecter2X(), character->GetVecter2Y());
	if (newX == slot.cellX && newY == slot.cellY)
		return;

	const int radius = slot.cellRadius;
	const int oldX = slot.cellX;
	const int oldY = slot.cellY;

	auto inRange = [radius](int cell, int center) { return cell >= center - radius && cell <= center + radius; };

	// 빠진 셀: 구독 해제 + 그 안의 액터를 leave.
	for (int cx = oldX - radius; cx <= oldX + radius; ++cx)
	{
		for (int cy = oldY - radius; cy <= oldY + radius; ++cy)
		{
			if (inRange(cx, newX) && inRange(cy, newY))
				continue; // 새 범위에도 포함 — 유지

			gridManager_->Unsubscribe(cx, cy, slotIndex);

			aoi_->scratch.clear();
			gridManager_->CollectActorsInCell(cx, cy, aoi_->scratch);
			for (IGridActor* entity : aoi_->scratch)
				aoi_->slots[slotIndex].pending.leaves.push_back(entity->GetActorId());
		}
	}

	// 새로 들어온 셀: 구독 + 그 안의 액터를 enter.
	for (int cx = newX - radius; cx <= newX + radius; ++cx)
	{
		for (int cy = newY - radius; cy <= newY + radius; ++cy)
		{
			if (inRange(cx, oldX) && inRange(cy, oldY))
				continue; // 이전 범위에도 있었음 — 이미 구독 중

			gridManager_->Subscribe(cx, cy, slotIndex);

			aoi_->scratch.clear();
			gridManager_->CollectActorsInCell(cx, cy, aoi_->scratch);
			for (IGridActor* entity : aoi_->scratch)
				aoi_->slots[slotIndex].pending.enters.push_back(static_cast<Actor*>(entity));
		}
	}

	aoi_->slots[slotIndex].cellX = newX;
	aoi_->slots[slotIndex].cellY = newY;
}

// 액터가 셀을 옮겼다. 옛 셀에만 있던 구독자에게는 leave, 새 셀에만 있는 구독자에게는 enter.
void Map::OnActorCellChanged(Actor* actor, int oldCellX, int oldCellY, int newCellX, int newCellY)
{
	if (aoi_->slotOf.empty())
		return;

	// SubscribersOf 는 셀 안의 벡터를 그대로 가리킨다. 여기서 구독을 건드리지 않으므로
	// 두 참조는 함수가 끝날 때까지 유효하다(복사할 이유가 없다).
	const std::vector<int>& before = gridManager_->SubscribersOf(oldCellX, oldCellY);
	const std::vector<int>& after = gridManager_->SubscribersOf(newCellX, newCellY);
	if (before.empty() && after.empty())
		return;

	// 두 목록의 차집합. 예전에는 서로를 선형 탐색해서 '구독자 수의 제곱' 이었다 —
	// 사람이 몰린 셀에서는 액터 한 마리가 셀을 넘을 때마다 수천 번을 비교했고,
	// 셀을 넘는 액터는 매 틱 수백 마리다. 표식 배열을 쓰면 두 목록 길이의 합이 된다.
	std::vector<uint64_t>& mark = aoi_->cellMark;
	if (mark.size() < aoi_->slots.size())
		mark.resize(aoi_->slots.size(), 0);

	const uint64_t inAfter = ++aoi_->cellStamp;
	for (int slot : after)
		mark[slot] = inAfter;

	for (int slot : before)
	{
		if (mark[slot] != inAfter)
			aoi_->slots[slot].pending.leaves.push_back(actor->GetActorId());
	}

	const uint64_t inBefore = ++aoi_->cellStamp;
	for (int slot : before)
		mark[slot] = inBefore;

	for (int slot : after)
	{
		if (mark[slot] != inBefore)
			aoi_->slots[slot].pending.enters.push_back(actor);
	}
}

// 액터를 새 좌표로 옮기면서 관심영역 장부(구독 셀 / 시야 진입·이탈)까지 갱신한다.
//
// 순간이동(부활, 게이트)이 쓰는 경로다. 평소 이동은 SyncActorState 가 movement 결과와
// 좌표를 대조해 같은 일을 하는데, 순간이동은 좌표를 직접 써 버려서 그 대조가 '변화 없음'
// 으로 보인다. 그러면 구독 셀이 옛 자리에 남아, 부활한 플레이어는 새 자리의 아무것도
// 못 받는다 — 자기 자신의 부활조차 못 받아서 클라는 계속 죽은 줄 안다.
// (실측: 1,000봇 2분에 서버는 1,698번 부활시켰는데 봇이 인지한 것은 228번뿐이었다.)
void Map::MoveActorAndUpdateView(Actor* actor, float x, float y, float z)
{
	const int oldCellX = actor->GetGridX();
	const int oldCellY = actor->GetGridY();

	actor->SetPosition(x, y, z);
	gridManager_->move(actor, x, z);

	if (!aoiEnabled_)
		return;

	if (actor->GetGridX() == oldCellX && actor->GetGridY() == oldCellY)
		return;

	if (actor->IsCharacter())
		OnViewerCellChanged(static_cast<Character*>(actor)->GetPlayerId(), actor);

	OnActorCellChanged(actor, oldCellX, oldCellY, actor->GetGridX(), actor->GetGridY());
}

// 변경된 액터를 그 액터가 있는 셀의 구독자에게만 큐잉한다(슬롯 색인이라 해시 조회가 없다).
void Map::QueueUpdate(Actor* actor)
{
	for (int slot : gridManager_->SubscribersOf(actor->GetGridX(), actor->GetGridY()))
		aoi_->slots[slot].pending.updates.push_back(actor);
}

// 플레이어마다 자기 시야 분량의 메시지를 조립해 보낸다. enter 는 전체 스냅샷, update 는 변경분만 담는다.
void Map::SendPendingViews()
{
	if (aoi_ == nullptr)
		return;

	BroadcastScope guard(*this);

	for (AoIState::Slot& slot : aoi_->slots)
	{
		if (!slot.active)
			continue;

		AoIState::PendingView& view = slot.pending;
		auto player = FindPlayer(slot.playerId);
		if (player == nullptr || view.empty())
		{
			view.clear();
			continue;
		}

		auto builder = SendMessagePool::Acquire();

		// 용량은 남겨 두고 내용만 비운다. reserve 는 이미 충분하면 아무것도 하지 않는다.
		std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& actors = aoi_->infoScratch;
		actors.clear();
		actors.reserve(view.enters.size() + view.updates.size());

		for (Actor* actor : view.enters)
			actors.push_back(actor->GetActorInfo(*builder, static_cast<long>(GameObjectChangeType::All)));
		for (Actor* actor : view.updates)
			actors.push_back(actor->GetActorInfo(*builder, actor->GetChangedFlag()));

		auto notify = syncnet::CreateUpdateActorNotifyDirect(*builder, &actors, nullptr, &view.leaves);
		auto msg = syncnet::CreateGameMessage(*builder, syncnet::GameMessages::GameMessages_UpdateActorNotify, notify.Union());
		builder->Finish(msg);
		player->Send(builder);

		view.clear();
	}
}

void Map::SyncActorState(engine::StateComponent& state, engine::PositionComponent& position)
{
	if (state.stateID == syncnet::AIState::AIState_Destroyed) {
		removedAgents_.push_back(state.ActorID);
	}

	if (movement_->IsActive(state.ActorID) == false)
		return;

	const float* npos = movement_->GetPos(state.ActorID);
	bool changed_position = !Vector3::equal(position.x, position.y, position.z, npos[0], npos[1], npos[2]);
	if (state.changeFlag == 0 && !changed_position)
		return;

	auto itr = actorMap_.find(state.ActorID);
	if (itr == actorMap_.end())
	{
		LOG.error("SendWorldState error agent not found in actorMap_");
		return;
	}
	auto actor = (Actor*)itr->second->get();

	// AoI 를 쓰지 않는 맵에서는 아래 두 분기가 모두 예측 가능한 bool 검사 하나로 끝난다.
	// (이 함수는 매 틱 액터마다 도는 가장 뜨거운 경로라, 여기에 조건이 붙으면 바로 비용이 된다.)
	if (aoiEnabled_)
	{
		const int oldCellX = actor->GetGridX();
		const int oldCellY = actor->GetGridY();

		if (changed_position)
		{
			actor->SetPosition(npos[0], npos[1], npos[2]);
			gridManager_->move(actor, npos[0], npos[2]);

			// 셀을 넘었으면 시야 진입/이탈이 생긴다. 플레이어 캐릭터면 자기 구독 범위도 함께 옮긴다.
			if (actor->GetGridX() != oldCellX || actor->GetGridY() != oldCellY)
			{
				if (actor->IsCharacter())
					OnViewerCellChanged(static_cast<Character*>(actor)->GetPlayerId(), actor);

				OnActorCellChanged(actor, oldCellX, oldCellY, actor->GetGridX(), actor->GetGridY());
			}
		}

		// 이 액터를 보고 있는 플레이어에게만 변경분을 큐잉한다(구독자 0이면 직렬화도 없다).
		QueueUpdate(actor);
	}
	else
	{
		if (changed_position)
		{
			actor->SetPosition(npos[0], npos[1], npos[2]);
			gridManager_->move(actor, npos[0], npos[2]);
		}

		// 브로드캐스트 모드: 한 번만 직렬화해 전원에게 같은 메시지를 보낸다.
		if (!players_.empty())
			actorPendingUpdates_.push_back(actor->GetActorInfo(*sendMessageBuilder_, actor->GetChangedFlag()));
	}

	actor->ResetChangedFlag();
}

int Map::GetMapId() const
{
	return mapData_ != nullptr ? mapData_->id : 0;
}

syncnet::Vec3 Map::GetPlayerSpawnPos() const
{
	if (mapData_ != nullptr && !mapData_->spawn_points.player_spawn.empty())
	{
		const auto& p = mapData_->spawn_points.player_spawn[0].position;
		return syncnet::Vec3(static_cast<float>(p.x), static_cast<float>(p.y), static_cast<float>(p.z));
	}
	return syncnet::Vec3(0, 0, 0);
}

// 마커 하나가 요구하는 위치에 몬스터 한 마리를 세운다. 성공하면 actor id, 실패하면 -1.
// MonsterSpawner 가 최초 스폰과 리스폰 양쪽에서 이 경로를 쓴다.
int Map::SpawnMonsterAt(const gamedata::MapSpawnPointsMonsterSpawn& marker,
	double x, double y, double z)
{
	// 스폰 위치는 Map.json(클라 좌표계) 기준. OnAddAgent 내부(Vector3 변환)에서
	// 클라 AddAgent 와 동일하게 서버 좌표계로 변환된다.
	syncnet::Vec3 pos(
		static_cast<float>(x),
		static_cast<float>(y),
		static_cast<float>(z));

	auto monster = OnAddAgent(nullptr, syncnet::GameObjectType::GameObjectType_Monster, &pos);
	if (monster == nullptr)
		return -1;

	// 마커가 정한 종류를 액터에 새긴다. 사망 이벤트에 실려 퀘스트의 kill 목표를
	// 종류별로 세는 근거가 된다(없으면 어떤 몬스터를 잡았는지 알 수 없다).
	if (auto* mob = dynamic_cast<Monster*>(monster.get()))
		mob->SetDataId(marker.monster_id);

	return monster->GetActorId();
}

int Map::SpawnMonstersFromData()
{
	if (mapData_ == nullptr)
		return 0;

	monsterSpawner_.Build(mapData_, world_ != nullptr ? world_->random_util() : nullptr);

	const int spawned = monsterSpawner_.SpawnInitial(
		[this](const gamedata::MapSpawnPointsMonsterSpawn& marker, double x, double y, double z)
		{
			return SpawnMonsterAt(marker, x, y, z);
		});

	if (spawned > 0)
	{
		LOG.info("Map {} 몬스터 {}마리 스폰(monster_spawn 마커 {}곳, 정원 {}마리)",
			GetMapId(), spawned, monsterSpawner_.GroupCount(), monsterSpawner_.DesiredCount());
	}
	return spawned;
}

// 마커의 정원을 유지한다. 죽은 개체는 여기서 걷어내고, spawn_interval 이 지나면 다시 채운다.
int Map::UpdateMonsterSpawns(float deltaTime)
{
	if (monsterSpawner_.GroupCount() == 0)
		return 0;

	const int spawned = monsterSpawner_.Update(deltaTime,
		[this](int actor_id)
		{
			// 체력이 0 이 되는 순간 정원에서 빠진다. 시체가 치워질 때까지(Destroyed 2초)
			// 기다리면 리스폰이 그만큼 늦어진다.
			auto actor = FindActor(actor_id);
			return actor != nullptr && !actor->IsDead();
		},
		[this](const gamedata::MapSpawnPointsMonsterSpawn& marker, double x, double y, double z)
		{
			return SpawnMonsterAt(marker, x, y, z);
		});

	if (spawned > 0)
		LOG.debug("Map {} 몬스터 {}마리 리스폰", GetMapId(), spawned);

	return spawned;
}

int Map::SpawnNpcsFromData()
{
	int spawned = 0;
	for (const auto& [npcId, npc] : ResourceLoader::Instance().GetNpcs())
	{
		if (npc == nullptr || npc->map_id != GetMapId())
			continue;

		// 맞을 수 있어야 액터다. hp 가 없는 NPC 는 클라가 씬에 배치한 정적 데이터일 뿐이고,
		// 서버는 Interact 의 거리 판정에만 쓴다.
		if (npc->hp <= 0)
			continue;

		// 위치는 npc.json(클라 좌표계) 기준. 몬스터 스폰과 같은 변환을 탄다.
		const syncnet::Vec3 clientPos(
			static_cast<float>(npc->position.x),
			static_cast<float>(npc->position.y),
			static_cast<float>(npc->position.z));
		Vector3 pos(&clientPos);

		auto actor = std::make_shared<NonPlayerCharacter>(this);

		// 데이터를 먼저 새긴다 — Init 이 move_speed 를 읽고, 목적지도 여기서 풀린다.
		actor->SetData(npc);

		if (!actor->Init(pos))
		{
			LOG.error("Map {} NPC {} 스폰 실패(위치 {}, {}, {})",
				GetMapId(), npc->id, npc->position.x, npc->position.y, npc->position.z);
			continue;
		}

		gridManager_->add(actor.get());

		auto itr = actorList_.insert(actorList_.end(), actor);
		actorMap_.insert(std::make_pair(actor->GetActorId(), itr));
		actor->SetChangedFlag(static_cast<long>(GameObjectChangeType::All));
		++spawned;
	}

	if (spawned > 0)
		LOG.info("Map {} NPC {}명 스폰(npc.json 기준)", GetMapId(), spawned);
	return spawned;
}

bool Map::ResolveGateTarget(int targetId, const gamedata::Map*& outMap, syncnet::Vec3& outPos)
{
	auto& resource = ResourceLoader::Instance();

	// 게이트 위 도착(양방향 통로의 짝 게이트).
	if (const gamedata::MapGate* gate = resource.GetMapGate(targetId))
	{
		outMap = gate->parent;
		outPos = syncnet::Vec3(
			static_cast<float>(gate->position.x),
			static_cast<float>(gate->position.y),
			static_cast<float>(gate->position.z));
		return outMap != nullptr;
	}

	// 스폰 지점 도착(레이드 등 인스턴스 입구는 짝 게이트가 없다).
	if (const gamedata::MapSpawnPointsPlayerSpawn* spawn = resource.GetMapSpawnPointsPlayerSpawn(targetId))
	{
		outMap = spawn->parent;
		outPos = syncnet::Vec3(
			static_cast<float>(spawn->position.x),
			static_cast<float>(spawn->position.y),
			static_cast<float>(spawn->position.z));
		return outMap != nullptr;
	}

	return false;
}

int Map::ValidateMapDataOnNavMesh(const gamedata::Map* mapData, const NavMesh* navMesh)
{
	if (mapData == nullptr || navMesh == nullptr || !navMesh->IsLoaded())
		return 0;

	// 수평 허용 오차(m). 마커가 메시 가장자리에서 이 이상 벗어나 있으면 정합 이상으로 본다.
	// 수직은 지형 높이 차이를 감안해 넉넉히 잡는다.
	constexpr float kHorizontalTolerance = 2.0f;
	const float halfExtents[3] = { kHorizontalTolerance, 10.0f, kHorizontalTolerance };

	int mismatches = 0;
	auto check = [&](double cx, double cy, double cz, const char* kind, int id)
	{
		// Map.json은 클라 좌표계 — navmesh(서버 좌표계)로 x 반전 변환 후 질의한다.
		const float pos[3] = {
			Vector3::convert_x(static_cast<float>(cx)),
			static_cast<float>(cy),
			static_cast<float>(cz) };

		dtPolyRef ref = 0;
		float nearest[3] = { 0, 0, 0 };
		dtStatus status = navMesh->query()->findNearestPoly(pos, halfExtents, navMesh->filter(), &ref, nearest);
		if (dtStatusFailed(status) || ref == 0)
		{
			LOG.warn("Map {} navmesh 정합 이상: {} {} 위치 클라({}, {}, {}) 주변 {}m 내에 navmesh 폴리곤이 없습니다. "
				"씬과 navmesh('{}')가 어긋났는지 확인하세요.",
				mapData->id, kind, id, cx, cy, cz, kHorizontalTolerance, mapData->navmesh_path);
			++mismatches;
		}
	};

	for (const auto& gate : mapData->gates)
		check(gate.position.x, gate.position.y, gate.position.z, "gate", gate.id);

	for (const auto& s : mapData->spawn_points.player_spawn)
		check(s.position.x, s.position.y, s.position.z, "player_spawn", s.id);
	for (const auto& s : mapData->spawn_points.monster_spawn)
		check(s.position.x, s.position.y, s.position.z, "monster_spawn", s.id);
	for (const auto& s : mapData->spawn_points.boss_spawn)
		check(s.position.x, s.position.y, s.position.z, "boss_spawn", s.id);

	if (mismatches == 0)
		LOG.info("Map {} navmesh 정합 확인: 게이트/스폰 마커 모두 navmesh 위에 있습니다.", mapData->id);
	else
		LOG.warn("Map {} navmesh 정합 이상 {}건 — 클라 씬과 서버 navmesh가 어긋났을 수 있습니다.", mapData->id, mismatches);

	return mismatches;
}

void Map::update(float deltaTime)
{
	//LOG.info("World update begin");
	// 단계별 메서드는 프로파일링/벤치마크에서 개별 측정할 수 있도록 public 으로 노출돼 있다.
	// 호출 순서/동작은 여기서 순서대로 부르는 것과 동일하다.
	UpdateActors(deltaTime);
	UpdateMovement(deltaTime);
	UpdateSystems(deltaTime);
	UpdatePlayerDeath(deltaTime);
	UpdateMonsterSpawns(deltaTime);
	UpdateGameMode(deltaTime);
	SendWorldState();
	//LOG.info("World update end");
}

// 단계 1: 살아있는 액터의 BehaviorTree tick(몬스터 AI: 적 탐지/추격/배회 등).
void Map::UpdateActors(float deltaTime)
{
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
		(*itr)->Update(deltaTime);

	// ECS 백엔드 몬스터는 액터 순회가 아니라 여기서 한꺼번에 사고한다.
	// 액터 루프 뒤에 두는 이유: 다른 백엔드도 Monster::Update 안에서 스킬 쿨다운을 진행한 뒤
	// 트리를 틱했다. 순서를 맞춰야 같은 틱에 같은 결정을 내린다.
	aiSystem_->Update(deltaTime);
}

// 단계 2: 네비게이션 이동 전략(Detour crowd / waypoint) 시뮬레이션.
void Map::UpdateMovement(float deltaTime)
{
	movement_->Update(deltaTime);
}

// 단계 3: ECS 시스템(위치 변경 감지 → ActorInfo 누적, 제거 대상 수집 등).
void Map::UpdateSystems(float deltaTime)
{
	systemManager_->Update(deltaTime);
}

// 단계 4: 게임 모드 진행.
//
// lua 는 게임 상태를 직접 못 읽는다 — GM_IsBossDead / GM_GetAlivePlayerCount 같은 조회
// 함수는 GameMode 에 저장된 값을 돌려줄 뿐이다. 그 값을 실제 월드에서 채우는 곳이 여기다.
void Map::UpdateGameMode(float deltaTime)
{
	if (gameMode_ == nullptr || gameMode_->IsEnded())
		return;

	// 보스 처치 판정. 액터가 사라졌거나 사망 상태면 처치로 본다.
	if (bossActorId_ >= 0 && !gameMode_->boss_dead())
	{
		auto boss = FindActor(bossActorId_);
		if (boss == nullptr
			|| boss->GetState() == syncnet::AIState::AIState_Dead
			|| boss->GetState() == syncnet::AIState::AIState_Destroyed)
		{
			gameMode_->set_boss_dead(true);
			LOG.info("Map {} 보스(actor {}) 처치", GetMapId(), bossActorId_);
		}
	}

	gameMode_->Update(deltaTime);
}

// 단계 4: 플레이어 사망/부활.
//
// 게임 모드와 무관하게 항상 돌아야 한다. 예전에는 UpdateGameMode 안에 있어서
// 게임 모드가 없는 필드 맵(또는 모드 종료 후)에서는 죽어도 아무 일이 없었다 —
// 체력이 0 인 채로 계속 돌아다닐 수 있었다.
void Map::UpdatePlayerDeath(float deltaTime)
{
	// 사망 판정. 체력이 0 이하로 떨어지는 순간 한 번만 처리한다.
	for (auto& entry : players_)
	{
		if (entry.second == nullptr)
			continue;
		auto character = entry.second->GetCharacter();
		if (character == nullptr || !character->IsDead())
			continue;
		if (deadPlayers_.find(entry.first) != deadPlayers_.end())
			continue;

		deadPlayers_.insert(entry.first);
		character->SetState(syncnet::AIState::AIState_Dead);

		// 죽은 캐릭터는 조작할 수 없다. 입력 잠금 하나로 이동/스킬이 모두 막힌다
		// (SetMoveTarget/UseSkill 핸들러와 SkillSet::TryCast 가 이 값을 본다).
		character->SetInputLocked(true);
		movement_->Stop(character->GetActorId());

		// 부활 예약. 게임 모드가 있으면 그 규칙(respawn_time)을, 없으면 기본값을 쓴다.
		// lua 의 on_player_dead 가 GM_SchedulePlayerRespawn 으로 덮어쓸 수 있다.
		SchedulePlayerRespawn(entry.first, ResolveRespawnSeconds());

		// 생존 수를 먼저 갱신해야 lua 의 전멸 판정(GM_GetAlivePlayerCount)이 맞는다.
		if (gameMode_ != nullptr && !gameMode_->IsEnded())
		{
			gameMode_->set_alive_player_count(CountAlivePlayers());
			LOG.info("Map {} 플레이어 {} 사망(생존 {}명)", GetMapId(), entry.first, gameMode_->alive_player_count());
			gameMode_->OnPlayerDead(entry.second.get());
		}
		else
		{
			LOG.info("Map {} 플레이어 {} 사망", GetMapId(), entry.first);
		}
	}

	// 부활 대기 시간 소진. on_player_dead 가 방금 넣은 항목도 여기서 함께 줄어든다.
	for (auto itr = pendingRespawns_.begin(); itr != pendingRespawns_.end(); )
	{
		itr->second -= deltaTime;
		if (itr->second > 0.0f)
		{
			++itr;
			continue;
		}
		const long playerId = itr->first;
		itr = pendingRespawns_.erase(itr);
		RespawnPlayer(playerId);
	}
}

float Map::ResolveRespawnSeconds() const
{
	if (gameMode_ != nullptr && gameMode_->gamedata != nullptr
		&& gameMode_->gamedata->rules.respawn_time > 0)
	{
		return static_cast<float>(gameMode_->gamedata->rules.respawn_time);
	}
	return kDefaultRespawnSeconds;
}

std::vector<std::shared_ptr<Player>> Map::GetPlayers() const
{
	std::vector<std::shared_ptr<Player>> result;
	result.reserve(players_.size());
	for (const auto& entry : players_)
	{
		if (entry.second != nullptr)
			result.push_back(entry.second);
	}
	return result;
}

int Map::CountAlivePlayers()
{
	int alive = 0;
	for (auto& entry : players_)
	{
		if (entry.second == nullptr)
			continue;
		auto character = entry.second->GetCharacter();
		if (character != nullptr && character->GetHealth() > 0)
			++alive;
	}
	return alive;
}

void Map::SchedulePlayerRespawn(long playerId, float seconds)
{
	// 0 이하면 다음 틱에 바로 되살린다(즉시 부활 설정).
	pendingRespawns_[playerId] = seconds > 0.0f ? seconds : 0.0f;
}

void Map::RespawnPlayer(long playerId)
{
	deadPlayers_.erase(playerId);

	auto player = FindPlayer(playerId);
	if (player == nullptr)
		return;
	auto character = player->GetCharacter();
	if (character == nullptr)
		return;

	// 스폰 지점으로 되돌린다. GetPlayerSpawnPos 는 클라 좌표계라 서버 좌표계로 변환한다.
	const syncnet::Vec3 spawn = GetPlayerSpawnPos();
	Vector3 serverPos(&spawn);
	movement_->TeleportAgent(character->GetActorId(), serverPos.pos());

	// 좌표를 직접 쓰는 이동이므로 관심영역 장부도 여기서 함께 옮겨야 한다
	// (SyncActorState 는 이 이동을 '변화 없음' 으로 본다).
	MoveActorAndUpdateView(character.get(), serverPos.x, serverPos.y, serverPos.z);

	// 최대 체력으로 되살린다. 예전에는 상수 100 이라, 레벨이 오를수록 부활 직후
	// 체력이 최대치에 한참 못 미쳤다(레벨 20 이면 1500 중 100).
	character->SetHealth(character->GetMaxHealth());
	character->SetState(syncnet::AIState::AIState_Patrol); // 살아있는 기본 상태
	character->SetInputLocked(false);                      // 사망 시 걸었던 조작 잠금 해제

	if (gameMode_ != nullptr && !gameMode_->IsEnded())
		gameMode_->set_alive_player_count(CountAlivePlayers());

	LOG.info("Map {} 플레이어 {} 부활(pos {}, {}, {})",
		GetMapId(), playerId, spawn.x(), spawn.y(), spawn.z());
}

int Map::SpawnBoss(int bossId)
{
	if (mapData_ == nullptr)
		return -1;

	// 보스 위치는 boss_spawn 마커. 없으면 스폰하지 않는다 — 아무 데나 세우면
	// navmesh 밖이나 입구 한복판에 나올 수 있다.
	if (mapData_->spawn_points.boss_spawn.empty())
	{
		LOG.error("Map {} boss_spawn 마커가 없어 보스 {} 를 스폰할 수 없습니다.", GetMapId(), bossId);
		return -1;
	}

	const auto& marker = mapData_->spawn_points.boss_spawn.front();
	syncnet::Vec3 pos(
		static_cast<float>(marker.position.x),
		static_cast<float>(marker.position.y),
		static_cast<float>(marker.position.z));

	auto boss = OnAddAgent(nullptr, syncnet::GameObjectType::GameObjectType_Monster, &pos);
	if (boss == nullptr)
	{
		LOG.error("Map {} 보스 {} 스폰 실패(위치 {}, {}, {})",
			GetMapId(), bossId, marker.position.x, marker.position.y, marker.position.z);
		return -1;
	}

	// 보스도 monster.json 의 한 종류다. 보스 토벌 퀘스트가 kill 목표로 셀 수 있어야 한다.
	if (auto* mob = dynamic_cast<Monster*>(boss.get()))
		mob->SetDataId(bossId);

	// 보스 체력은 게임 모드 데이터가 정한다(없으면 몬스터 기본값 유지).
	if (gameMode_ != nullptr && gameMode_->gamedata != nullptr && gameMode_->gamedata->boss_info.boss_hp > 0)
		boss->SetHealth(gameMode_->gamedata->boss_info.boss_hp);

	bossActorId_ = boss->GetActorId();
	LOG.info("Map {} 보스 {} 스폰(actor {}, hp {})", GetMapId(), bossId, bossActorId_, boss->GetHealth());
	return bossActorId_;
}

// 프로파일링용: 모든 액터에 대해 DetectEnemy 만 1회씩 호출(적 탐지 비용 격리 측정).
int Map::ProfileDetectEnemyAll()
{
	int found = 0;
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		if (DetectEnemy((Actor*)itr->get()) >= 0)
			++found;
	}
	return found;
}

// 프로파일링용: SyncActorState 가 액터마다 부르는 GetActorInfo(직렬화)만 격리해 돌린다.
// 실제 전송은 하지 않고, 만든 오프셋과 빌더는 그대로 버린다.
int Map::ProfileSerializeAll()
{
	auto builder = SendMessagePool::Acquire();
	int count = 0;
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		Actor* actor = itr->get();
		auto offset = actor->GetActorInfo(*builder, actor->GetChangedFlag());
		(void)offset;
		++count;
	}
	return count;
}

// 플레이어마다 자기 관심영역 분량의 메시지를 조립해 보낸다(맵 전체 브로드캐스트가 아니다).
void Map::SendWorldState()
{
	// 관전자가 없으면 만들 것도 보낼 것도 없다. 제거 대기 액터 정리는 그대로 수행한다.
	if (players_.empty())
	{
		this->raycasts_.clear();
		for (auto& actor_id : removedAgents_)
			OnRemoveAgent(actor_id);

		if (aoi_ != nullptr)
		{
			for (AoIState::Slot& slot : aoi_->slots)
				slot.pending.clear();
		}
		removedAgents_.clear();
		return;
	}

	if (AoIEnabled())
	{
		SendPendingViews();
	}
	else
	{
		SendBroadcastState();
	}

	SendDebugRaycasts();
	SendTreeDebugSync();

	for (auto& actor_id : removedAgents_)
	{
		OnRemoveAgent(actor_id);
	}

	removedAgents_.clear();
}

// 브로드캐스트 모드: 이번 틱에 바뀐 액터를 한 메시지로 묶어 맵의 전원에게 보낸다.
// 시야 반경이 맵 크기 이상이라 어차피 모두가 모두를 보는 맵에서는 이쪽이 싸다.
void Map::SendBroadcastState()
{
	if (actorPendingUpdates_.empty())
		return;

	auto agents = sendMessageBuilder_->CreateVector(actorPendingUpdates_);
	auto notify = syncnet::CreateUpdateActorNotify(*sendMessageBuilder_, agents, 0, 0);
	auto msg = syncnet::CreateGameMessage(*sendMessageBuilder_, syncnet::GameMessages::GameMessages_UpdateActorNotify, notify.Union());
	sendMessageBuilder_->Finish(msg);

	SendBroadcast(sendMessageBuilder_);

	sendMessageBuilder_ = SendMessagePool::Acquire();
	actorPendingUpdates_.clear();
}

// 디버그 레이캐스트 표식은 관심영역과 무관한 개발 편의 기능이라 그대로 전원에게 보낸다.
void Map::SendDebugRaycasts()
{
	if (raycasts_.empty())
		return;

	auto builder = SendMessagePool::Acquire();
	std::vector<flatbuffers::Offset<syncnet::DebugRaycast>> debugs;
	debugs.reserve(raycasts_.size());
	for (size_t i = 0; i < raycasts_.size(); ++i)
		debugs.push_back(syncnet::CreateDebugRaycast(*builder, 0, &raycasts_[i]));
	raycasts_.clear();

	auto notify = syncnet::CreateUpdateActorNotifyDirect(*builder, nullptr, &debugs, nullptr);
	auto msg = syncnet::CreateGameMessage(*builder, syncnet::GameMessages::GameMessages_UpdateActorNotify, notify.Union());
	builder->Finish(msg);
	SendBroadcast(builder);
}

void Map::SendTreeDebugSync()
{
	// 직렬화는 BT 디버그 모듈(BTDebugSync)이 담당하고, 맵은 브로드캐스트만 한다.
	// 보낼 스냅샷이 없거나 BT 디버그가 꺼진 빌드면 nullptr 라 아무것도 하지 않는다.
	auto msg = BTDebugSync::BuildMessage();
	if (msg != nullptr)
		SendBroadcast(msg);
}

void Map::SendBroadcast(std::shared_ptr<send_message> msg)
{
	BroadcastScope guard(*this);

	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		itr->second->Send(msg);
	}
}

void Map::SendBroadcast(std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except)
{
	BroadcastScope guard(*this);

	for (auto itr = players_.begin(); itr != players_.end(); ++itr)
	{
		if (itr->second.get() == except.get())
			continue;

		itr->second->Send(msg);
	}
}

// 그 액터가 있는 자리에서 일어난 일을 '그 자리를 보고 있는 사람' 에게만 알린다.
// 관심영역이 꺼져 있으면(작은 맵) 예전처럼 맵 전원에게 보낸다.
//
// 스킬 시전 알림이 이 경로를 쓴다. 예전에는 전원 브로드캐스트라 시전 한 번이
// '접속자 수' 만큼의 메시지가 됐고, 전투가 몰리면 그 곱이 워커 스레드를 통째로
// 잡아먹었다(800명 측정에서 8,700 msg/s 가 47,000 msg/s 로 튀었다).
void Map::SendToViewersOf(Actor* source, std::shared_ptr<send_message> msg, std::shared_ptr<Player>& except)
{
	if (!AoIEnabled() || source == nullptr || aoi_ == nullptr)
	{
		SendBroadcast(msg, except);
		return;
	}

	BroadcastScope guard(*this);

	const long exceptId = except != nullptr ? except->GetPlayerId() : -1;

	for (int slot : gridManager_->SubscribersOf(source->GetGridX(), source->GetGridY()))
	{
		const long viewerId = aoi_->slots[slot].playerId;
		if (viewerId == exceptId)
			continue;

		auto player = FindPlayer(viewerId);
		if (player != nullptr)
			player->Send(msg);
	}
}

void Map::OnRemoveAgent(int actor_id)
{
	auto itr = actorMap_.find(actor_id);
	if (itr == actorMap_.end())
	{
		LOG.error("OnRemoveAgent error not exist in monstersMap_");
		return;
	}

	Actor* removed = (Actor*)itr->second->get();

	if (removed->GetType() == syncnet::GameObjectType_Character)
	{
		auto character = std::dynamic_pointer_cast<Character>(*itr->second);

		// 이 캐릭터가 보던 관심영역 구독을 먼저 걷어낸다.
		UnsubscribeViewer(character->GetPlayerId());

		auto itr_player = players_.find(character->GetPlayerId());
		if (itr_player != players_.end())
		{
			players_.erase(itr_player);
			UpdateAoIMode(); // 인구가 줄었다.
		}
		ForgetPlayerModeState(character->GetPlayerId());
	}

	// 사라지는 액터는 그 자리를 보고 있던 플레이어들의 시야에서도 빠진다.
	// (사망 연출은 이미 상태 동기화로 전달됐고, 여기 leave 는 목록에서 지우라는 뜻이다.)
	if (AoIEnabled())
	{
		for (long viewerId : gridManager_->SubscribersOf(removed->GetGridX(), removed->GetGridY()))
			aoi_->slots[viewerId].pending.leaves.push_back(actor_id);
	}

	gridManager_->remove(removed);
	actorList_.erase(itr->second);
	actorMap_.erase(itr);

	movement_->RemoveAgent(actor_id);

}

std::shared_ptr<Actor> Map::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	auto actor = ActorFactory::CreateActor(this, player, type, pos);
	if (actor == nullptr)
	{
		LOG.error("OnAddAgent error in ActorFactory::CreateActor()");
		return nullptr;
	}

	auto itr = actorList_.insert(actorList_.end(), actor);
	actorMap_.insert(std::make_pair(actor->GetActorId(), itr));
	actor->SetChangedFlag(static_cast<long>(GameObjectChangeType::All));

	// 플레이어 캐릭터가 생기는 시점이 곧 그 플레이어의 관심영역이 열리는 시점이다.
	// (입장 시점에는 아직 캐릭터가 없어 구독할 중심 좌표가 없다.)
	if (AoIEnabled() && player != nullptr && actor->IsCharacter())
		SubscribeViewer(player->GetPlayerId(), actor.get(), true);

	// 캐릭터가 이 맵에 실제로 생기는 시점이 곧 지역 진입이다(최초 로그인이든 게이트
	// 이동이든 모두 여기를 지난다). 탐험 목표(reach)가 이 이벤트로 진행된다.
	if (player != nullptr && actor->IsCharacter())
	{
		if (auto* broker = player->GetComponent<PlayerEventBroker>())
			broker->publish(EventAreaEntered{ static_cast<int>(player->GetPlayerId()), GetMapId() });
	}

	return actor;
}


void Map::OnSetMoveTarget(int actor_id, const syncnet::Vec3* pos)
{
	this->GetNavMap()->SetMoveTarget(actor_id, Vector3(pos).pos(), false);

}

void Map::OnSetRaycast(const syncnet::Vec3* pos)
{
	float hitPoint[3];
	if (this->GetNavMap()->Raycast(0, Vector3(pos).pos(), hitPoint))
	{
		syncnet::Vec3 pos(hitPoint[0] * -1, hitPoint[1], hitPoint[2]);
		this->raycasts_.push_back(pos);
	}
}

// 이미 잡아 둔 대상이 아직 유효한지만 확인한다(교전 중 전체 재탐색 대체).
// 전체 탐색은 시야 반경 안 모든 셀을 훑고 후보마다 시야 판정을 하지만, 여기서는
// 대상 하나에 대해 거리 1회 + 시야 판정 1회로 끝난다(약 1/3 비용).
bool Map::IsTargetVisible(Actor* viewer, int targetActorId)
{
	if (viewer == nullptr || targetActorId < 0)
		return false;

	// 판정 기준은 DetectEnemy 와 정확히 같아야 한다(맵에 존재 + 시야 반경 + 가림 없음).
	// 여기서만 추가 조건(예: 사망 제외)을 걸면, 검증에서 놓친 대상을 바로 다음 전체 탐색이
	// 다시 잡아 타깃이 매 틱 붙었다 떨어지는 진동이 생긴다.
	auto target = FindActor(targetActorId);
	if (target == nullptr)
		return false;

	const float dx = target->GetVector2X() - viewer->GetVector2X();
	const float dy = target->GetVector2Y() - viewer->GetVector2Y();
	if (dx * dx + dy * dy > g_fDistance * g_fDistance)
		return false;

	float hitPoint[3];
	const float* targetPos = GetNavMap()->GetPos(targetActorId);
	return GetNavMap()->Raycast(viewer->GetActorId(), targetPos, hitPoint) == false;
}

int Map::DetectEnemy(Actor* actor)
{
	float hitPoint[3];

	// 캐릭터만, 실제 거리로 컬링해서 가져온다. detectScratch_ 버퍼를 재사용해 호출당 힙 할당이 없다.
	gridManager_->getCharactersInViewRange(actor, g_fDistance, detectScratch_);

	for (auto* target : detectScratch_)
	{
		const float* targetPos = this->GetNavMap()->GetPos(target->GetActorId());

		if (this->GetNavMap()->Raycast(actor->GetActorId(), targetPos, hitPoint) == false)
		{
			return target->GetActorId();
		}
	}
	return -1;
}

std::vector<IGridActor*> Map::get_actors_in_range(Actor* actor, float range, float dirDeg, float angle)
{
	return gridManager_->getEntitiesInAoEMask(actor->GetVecter2X(), actor->GetVecter2Y(), range, dirDeg, angle);
}

std::vector<IGridActor*> Map::get_actors_in_radius(float centerX, float centerZ, float radius)
{
	// dirDeg 인자만 받는 오버로드는 angle=360(완전한 원형)으로 처리된다.
	return gridManager_->getEntitiesInAoEMask(centerX, centerZ, radius, 0.0f);
}

void Map::Enter(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::Enter error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr != players_.end())
	{
		LOG.error("Map::Enter error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->GetPlayerId(), player));

	// 인구가 늘었다 — 관심영역을 켜야 하는 구간인지 본다.
	UpdateAoIMode();

	// 진행 로직에 입장을 알린다. 생존 수를 먼저 채워야 on_player_join 안에서 조회해도 맞는다.
	if (gameMode_ != nullptr)
	{
		gameMode_->set_alive_player_count(CountAlivePlayers());
		gameMode_->OnPlayerJoin(player.get());
	}
}

void Map::SendStateTo(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::SendStateTo error player is nullptr");
		return;
	}

	if (!AoIEnabled())
	{
		// 브로드캐스트 모드: 맵의 모든 액터 상태를 그대로 보낸다.
		auto builder_ptr = SendMessagePool::Acquire();
		std::vector<flatbuffers::Offset<syncnet::ActorInfo>> agents;
		GetAgentsInfo(builder_ptr, agents);
		auto updateActorNotify = syncnet::CreateUpdateActorNotifyDirect(*builder_ptr, &agents, nullptr, nullptr);
		auto send_msg = syncnet::CreateGameMessage(*builder_ptr, syncnet::GameMessages::GameMessages_UpdateActorNotify, updateActorNotify.Union());
		builder_ptr->Finish(send_msg);
		player->Send(builder_ptr);
		return;
	}

	// 관심영역 모드: 시야 안의 액터만 보낸다. 캐릭터가 아직 없으면(입장 직후) 기준점이 없으므로,
	// 캐릭터가 생기는 시점(OnAddAgent)에 구독과 함께 스냅샷이 나간다.
	auto character = player->GetCharacter();
	if (character == nullptr)
		return;

	SubscribeViewer(player->GetPlayerId(), character.get(), true);

	// 구독하면서 쌓인 스냅샷(enter)을 바로 내보낸다.
	SendPendingViews();
}

void Map::join(std::shared_ptr<Player> player)
{
	Enter(player);
	SendStateTo(player);
}

void Map::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("Map::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr == players_.end())
	{
		LOG.error("Map::leave error player not found");
		return;
	}

	// 브로드캐스트 순회 중이면 지금 지울 수 없다 - 순회 중인 이터레이터와, 관심영역이라면
	// 셀 구독자 벡터의 참조까지 무효가 된다. 순회가 끝나는 자리에서 처리한다.
	if (broadcastDepth_ > 0)
	{
		if (std::find(deferredLeaves_.begin(), deferredLeaves_.end(), player) == deferredLeaves_.end())
			deferredLeaves_.push_back(player);
		return;
	}

	UnsubscribeViewer(player->GetPlayerId());
	players_.erase(itr);
	ForgetPlayerModeState(player->GetPlayerId());

	// 인구가 줄었다 — 브로드캐스트가 다시 싼 구간인지 본다.
	UpdateAoIMode();
}

// 순회 중이라 미뤄 둔 퇴장을 실제로 처리한다. 마지막 BroadcastScope 가 닫힐 때 불린다.
// 이 안의 leave 가 또 브로드캐스트를 부르지는 않지만, 목록을 비우며 도는 편이 안전하다.
void Map::DrainDeferredLeaves()
{
	while (!deferredLeaves_.empty())
	{
		std::vector<std::shared_ptr<Player>> pending;
		pending.swap(deferredLeaves_);

		for (auto& player : pending)
			leave(player);
	}
}

// 맵을 떠난 플레이어의 사망/부활 대기 상태를 지운다. 남겨 두면 다른 맵으로 간 뒤에도
// 여기서 부활 타이머가 돌아 엉뚱한 맵의 스폰 지점으로 순간이동시킨다.
void Map::ForgetPlayerModeState(long playerId)
{
	deadPlayers_.erase(playerId);
	pendingRespawns_.erase(playerId);

	if (gameMode_ != nullptr)
		gameMode_->set_alive_player_count(CountAlivePlayers());
}

std::shared_ptr<Player> Map::FindPlayer(long player_id)
{
	auto itr = players_.find(player_id);
	if (itr == players_.end())
		return nullptr;
	return itr->second;
}

std::shared_ptr<Actor> Map::FindActor(int actor_id)
{
	auto itr = actorMap_.find(actor_id);
	if (itr == actorMap_.end())
		return nullptr;
	return *itr->second;
}

void Map::GetAgentsInfo(std::shared_ptr<send_message>& msg, std::vector<flatbuffers::Offset<syncnet::ActorInfo>>& agent_info_vector)
{
	for (std::list<std::shared_ptr<Actor>>::iterator itr = actorList_.begin(); itr != actorList_.end(); ++itr)
	{
		auto actor = itr->get();
		agent_info_vector.push_back(actor->GetActorInfo(*msg, static_cast<long>(GameObjectChangeType::All)));
	}
}
