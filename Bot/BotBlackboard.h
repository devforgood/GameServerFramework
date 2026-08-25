#pragma once

#include <random>

#include "BotQuestBrain.h"
#include "WorldView.h"

namespace bot
{
	// BT 노드가 세계에 영향을 주는 유일한 통로.
	//
	// 노드가 소켓을 직접 만지지 않게 하는 이유는 두 가지다. 시나리오 로직을 네트워크 없이
	// 단위 테스트로 돌릴 수 있고(가짜 구현을 끼운다), 전송 경로(레이트리밋/큐)를 봇 쪽
	// 한 곳에서만 관리할 수 있다.
	struct BotActions
	{
		virtual ~BotActions() = default;

		virtual void MoveTo(const Vec3& pos) = 0;
		virtual void Attack(int target_actor_id, const Vec3& target_pos) = 0;

		// NPC/오브젝트 상호작용. 서버가 거리와 맵을 검증하고, 통과하면 퀘스트의 talk 목표가
		// 오르며 대화가 걸린 NPC 면 첫 대화 노드가 이어서 온다.
		virtual void Interact(int target_id) = 0;

		// 대화 선택지. node_id 는 지금 보고 있는 노드이고(서버가 아는 것과 다르면 거절한다),
		// choice_index 는 '서버가 보낸 목록'에서의 번호다. 음수면 창을 닫는다.
		virtual void SelectDialog(int node_id, int choice_index) = 0;

		// 선택 보상이 있는 퀘스트의 완료. 대화로는 무엇을 고를지 전할 수 없어 서버가
		// 거절하므로, 클라이언트와 같이 번호를 실어 직접 보낸다.
		virtual void CompleteQuest(int quest_id, int reward_choice) = 0;

		// 게이트 진입. 목적지는 게이트가 정한다(요청에 담지 않는다).
		virtual void EnterGate(int gate_id) = 0;
	};

	struct AiSettings
	{
		float search_radius = 30.0f;
		float attack_range = 1.6f;
		int attack_skill_id = 1;
		int attack_interval_ms = 600;
		int move_repath_ms = 500;
		float wander_radius = 15.0f;
		int wander_interval_ms = 4000;
		float arrive_epsilon = 1.0f;
	};

	// 봇 한 명의 판단 재료. 봇이 소유하고 그 봇의 워커 스레드에서만 접근한다
	// (봇 사이에 공유되는 상태는 하나도 없다 — 그래서 락이 없다).
	struct BotBlackboard
	{
		BotActions* actions = nullptr;
		AiSettings ai;

		// 봇 시작 이후 경과 초(단조 시계). 모든 쿨다운 비교는 이 값으로 한다.
		double now = 0.0;

		// 서버가 준 내 캐릭터의 actor id. 0 도 유효한 값이므로(내비메시 crowd 의 첫 에이전트가
		// 0 을 받는다) "캐릭터가 있는가" 는 id 가 아니라 이 플래그로 판단한다.
		bool has_character = false;
		int self_actor_id = 0;
		Vec3 self_pos;
		Vec3 spawn_pos;
		bool self_dead = false;

		WorldView view;

		// 시나리오(메인 퀘스트) 진행 상태와 지금의 목표. 설정에서 꺼 두면 비활성이고,
		// 그때 봇은 예전처럼 자유 사냥만 한다.
		BotQuestBrain quest;

		int target_actor_id = 0;

		double next_attack_at = 0.0;
		double next_move_at = 0.0;
		double next_wander_at = 0.0;
		Vec3 wander_target;

		// 봇마다 독립된 난수원. 전역 엔진을 공유하면 워커 스레드끼리 경쟁하고,
		// 시드가 같으면 봇 전원이 같은 지점으로 몰려가 부하가 한쪽에 쏠린다.
		std::mt19937 rng;

		uint64_t bt_ticks = 0;

		float RandomFloat(float min_value, float max_value)
		{
			std::uniform_real_distribution<float> distribution(min_value, max_value);
			return distribution(rng);
		}
	};
}
