#pragma once
#include "Vector3.h"

// 스킬별 런타임 페이즈. Ready → (Cast 성공) → Active(지속시간이 있으면) → Cooldown → Ready.
enum class SkillPhase
{
	Ready,
	Active,
	Cooldown,
};

// 캐릭터별 스킬 런타임 상태. 스킬 정의(Skill)는 무상태 공유(flyweight)이므로,
// 시전마다 달라지는 값은 전부 여기에 보관하고 SkillSet 이 소유한다.
struct SkillState
{
	SkillPhase phase = SkillPhase::Ready;
	float activeRemaining = 0.0f;    // Active 페이즈 남은 시간(초)
	float cooldownRemaining = 0.0f;  // Cooldown 페이즈 남은 시간(초)
	bool inputLocked = false;        // 이 스킬이 캐스터 입력을 잠갔는지 (Active 종료 시 해제)
	float castTimeOffset = 0.0f;     // 시전 시점의 서버-클라 시차(초)
	float pulseTimer = 0.0f;         // 오라(pulse 효과) 누적 시간. pulse_interval 마다 방출 후 감산.
	Vector3 targetPos;               // 시전 목표 지점(서버 좌표계). dash 는 도달 가능 지점으로 보정한다.
	bool dashing = false;            // dash 효과로 돌진 중인지 (Skill::Tick 이 매 틱 전진시킨다)
	float dashSpeed = 0.0f;          // 돌진 속도(단위/초). dash 효과가 거리/지속시간으로 계산한다.
};
