#include "World.h"
#include "syncnet_generated.h"
#include "DetourCrowd.h"
#include <algorithm>
#include <vector>
#include <iostream>
#include "Server.h"
#include "Monster.h"
#include "Character.h"
#include "Vector3.h"
#include "LogHelper.h"
#include "DetourCommon.h"
#include "MathHelper.h"
#include "Player.h"
#include "ActorFactory.h"
#include "Common.h"
#include "Map.h"
#include "GameMode.h"
#include "GameModeFactory.h"



World::World()
{
	randomUtil_ = nullptr;
	timeStamp_ = nullptr;
}

World::~World()
{
	if (randomUtil_)
	{
		delete randomUtil_;
		randomUtil_ = nullptr;
	}
	if (timeStamp_)
	{
		delete timeStamp_;
		timeStamp_ = nullptr;
	}
}

void World::Init(const std::string& movementOverride)
{
	Monster::Initialize("mob.lua");
	Monster::registerLuaFunctionAll();

	// 게임 모드용 공유 lua 상태 생성 + 호스트 함수(GM_*) 등록.
	GameMode::InitializeLua();

	randomUtil_ = new RandomUtil();
	timeStamp_ = new TimeStamp();

	// 기본 게임 모드 부트스트랩(현재는 Field, id=1).
	// 추후 매치메이킹/세션이 모드 id 를 결정하도록 확장한다.
	gameMode_.reset(GameModeFactory::Create(1));

	// 게임 모드 데이터가 지정한 이동 전략으로 맵의 네비게이션을 초기화한다.
	// 미지정 시 NavMovementFactory 가 crowd 로 폴백한다.
	std::string movementType;
	if (gameMode_ && gameMode_->gamedata)
		movementType = gameMode_->gamedata->movement;

	// 명시적 오버라이드가 있으면 게임 모드 설정보다 우선한다(벤치마크/테스트).
	if (!movementOverride.empty())
		movementType = movementOverride;

	// 로드된 맵 데이터 중 field 타입(소속 게임 모드 type == "field") 맵을 모두 로드한다.
	auto& resource = ResourceLoader::Instance();
	std::vector<const gamedata::Map*> fieldMaps;
	for (const auto& pair : resource.GetMaps())
	{
		const gamedata::Map* mapData = pair.second;
		if (mapData == nullptr)
			continue;

		const gamedata::GameMode* gm = resource.GetGameMode(mapData->game_mode_id);
		if (gm == nullptr || gm->type != "field")
			continue;

		fieldMaps.push_back(mapData);
	}

	// GetMaps() 는 unordered_map 이라 순회 순서가 비결정적이므로, primary 맵(front)이
	// 항상 동일하도록 맵 id 오름차순으로 정렬한다.
	std::sort(fieldMaps.begin(), fieldMaps.end(),
		[](const gamedata::Map* a, const gamedata::Map* b) { return a->id < b->id; });

	for (const gamedata::Map* mapData : fieldMaps)
	{
		std::shared_ptr<Map> map = std::make_shared<Map>(this);
		map->Init(movementType);
		mapList_.push_back(map);
	}

	if (gameMode_ && !mapList_.empty())
	{
		gameMode_->SetMap(mapList_.front().get());
		gameMode_->LoadScript();
		gameMode_->Start();
	}
}

void World::update(float deltaTime)
{
	timeStamp_->update();

	//LOG.info("World update begin");
	for (std::list<std::shared_ptr<Map>>::iterator itr = mapList_.begin();itr!= mapList_.end();++itr)
		(*itr)->update(deltaTime);

	if (gameMode_)
		gameMode_->Update(deltaTime);
}

void World::join(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::join error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr != players_.end())
	{
		LOG.error("World::join error player already exists");
		return;
	}
	players_.insert(std::make_pair(player->GetPlayerId(), player));

	// todo : map 선택 로직 추가
	mapList_.begin()->get()->join(player);
}

void World::leave(std::shared_ptr<Player> player)
{
	if (player == nullptr)
	{
		LOG.error("World::leave error player is nullptr");
		return;
	}
	auto itr = players_.find(player->GetPlayerId());
	if (itr == players_.end())
	{
		LOG.error("World::leave error player not found");
		return;
	}
	players_.erase(itr);
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->leave(player);
}



std::shared_ptr<Actor> World::OnAddAgent(std::shared_ptr<Player> player, syncnet::GameObjectType type, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	return mapList_.begin()->get()->OnAddAgent(player, type, pos);
}

void World::OnRemoveAgent(int agent_id)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnRemoveAgent(agent_id);
}

void World::OnSetMoveTarget(int agent_id, const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetMoveTarget(agent_id, pos);
}

void World::OnSetRaycast(const syncnet::Vec3* pos)
{
	// todo : map 선택 로직 추가
	mapList_.begin()->get()->OnSetRaycast(pos);
}