#pragma once
#include <variant>
#include <string>
#include <iostream>

// 액터 처치 (처치 퀘스트, 보스 토벌 등)
struct EventActorDead
{
	int killer_actor_id;
	int victim_actor_id;
};

// 플레이어 입장
struct EventPlayerJoined
{
	int player_id;
};

// 아이템 획득 (수집 퀘스트)
struct EventItemAcquired
{
	int player_id;
	int item_id;
	int count;
};

// 레벨 상승 (레벨 달성 퀘스트, 컨텐츠 해금)
struct EventLevelUp
{
	int player_id;
	int new_level;
};

// 퀘스트 완료 (연계 퀘스트 진행)
struct EventQuestCompleted
{
	int player_id;
	int quest_id;
};

// 특정 지역 진입 (탐험 퀘스트, 영역 트리거)
struct EventAreaEntered
{
	int player_id;
	int area_id;
};

// NPC 상호작용 (대화 퀘스트)
struct EventNpcInteracted
{
	int player_id;
	int npc_id;
};

using EventMessage = std::variant<
	EventActorDead
	, EventPlayerJoined
	, EventItemAcquired
	, EventLevelUp
	, EventQuestCompleted
	, EventAreaEntered
	, EventNpcInteracted
>;
