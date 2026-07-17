#pragma once
#include "Vector3.h"

// 스킬 시전 요청의 네트워크 비의존 표현.
// PlayerController(syncnet::UseSkill)와 AI(BehaviorTree) 어느 쪽에서 시작해도
// SkillSet::TryCast 하나의 파이프라인으로 스킬을 사용할 수 있게 한다.
struct CastContext
{
	int skillId = 0;
	int targetActorId = -1;
	Vector3 targetPos;               // 서버 좌표계
	float clientDuration = 0.0f;     // 클라가 보고한 지속시간(초). 0 이하면 gamedata.duration 사용
	float serverClientTimeOffset = 0.0f; // 서버-클라 시차(초)
};

// 시전 결과. Success 외에는 상태 변화가 없어야 한다(쿨다운 미소모, 브로드캐스트 금지).
enum class CastResult
{
	Success = 0,
	SkillNotFound,   // 보유하지 않았거나 데이터에 없는 스킬
	OnCooldown,      // 쿨다운 중
	AlreadyActive,   // 지속시간(Active) 진행 중
	InputLocked,     // 캐스터 입력 잠금 상태(다른 스킬 시전 중 등)
	CasterInvalid,   // 캐스터/데이터가 유효하지 않음
	UnknownEffect,   // 데이터의 effect type 이 레지스트리에 없음
};
