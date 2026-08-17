#pragma once
#include <variant>
#include <string>
#include <iostream>

// 액터 처치 (처치 퀘스트, 보스 토벌 등)
struct EventActorDead
{
	int killer_actor_id;
	int victim_actor_id;

	// 처치된 대상의 데이터 id(monster.json). 퀘스트의 kill 목표는 "고블린 10마리"처럼
	// 종류로 세므로, 매 스폰마다 달라지는 actor id 로는 셀 수 없다.
	// 0 이면 데이터 id 를 모르는 대상(플레이어 등).
	int victim_data_id = 0;

	// 이 처치를 나눠 갖는 인원 수(파티 킬 크레딧). 혼자면 1.
	// 퀘스트 목표는 인원과 상관없이 각자 1마리로 세지만, 경험치는 이 수로 나눈다 —
	// 나누지 않으면 파티를 맺는 것만으로 사냥 효율이 인원 배로 뛴다.
	int credit_share = 1;

	// 처치 보상 경험치(monster.json 의 exp). 종류마다 다르므로 이벤트에 실어 나른다 —
	// 예전에는 수신 측이 상수 50 을 썼고, 슬라임과 고대 드래곤의 보상이 같았다.
	int reward_exp = 0;
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

// 아이템 사용 (사용 퀘스트)
struct EventItemUsed
{
	int player_id;
	int item_id;
	int count;
};

// 스킬 사용 (스킬 사용 퀘스트, 숙련도)
struct EventSkillUsed
{
	int player_id;
	int skill_id;
};

// 레벨 상승 (레벨 달성 퀘스트, 컨텐츠 해금)
struct EventLevelUp
{
	int player_id;
	int new_level;
};

// 퀘스트 수락 (클라 동기화, 추적 목록)
struct EventQuestAccepted
{
	int player_id;
	int quest_id;
};

// 퀘스트 완료 (연계 퀘스트 진행)
struct EventQuestCompleted
{
	int player_id;
	int quest_id;
};

// 퀘스트 실패 (제한 시간 초과 등)
struct EventQuestFailed
{
	int player_id;
	int quest_id;
};

// 특정 지역 진입 (탐험 퀘스트, 영역 트리거). area_id 는 맵 id 다.
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

// 맵 오브젝트 상호작용 (상자 개봉, 장치 조작 등). object_id 는 Map.json 의 오브젝트 id.
struct EventObjectInteracted
{
	int player_id;
	int object_id;
};

// 호위/보호 대상 NPC 가 죽었다. npc_id 는 npc.json 의 NPC id 다.
// 그 NPC 를 호위/보호 중이던 퀘스트는 실패한다 — 되살아난 NPC 로 이어서 할 수 없다.
struct EventNpcDead
{
	int player_id;
	int npc_id;
};

// 호위 대상 NPC 가 목적지에 닿았다.
struct EventNpcEscorted
{
	int player_id;
	int npc_id;
};

using EventMessage = std::variant<
	EventActorDead
	, EventPlayerJoined
	, EventItemAcquired
	, EventItemUsed
	, EventSkillUsed
	, EventLevelUp
	, EventQuestAccepted
	, EventQuestCompleted
	, EventQuestFailed
	, EventAreaEntered
	, EventNpcInteracted
	, EventObjectInteracted
	, EventNpcDead
	, EventNpcEscorted
>;
