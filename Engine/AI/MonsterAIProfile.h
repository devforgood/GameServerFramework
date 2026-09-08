#pragma once

#include <cstdint>
#include <string_view>

//---------------------------------------------------------------------------------------
// 몬스터의 성향(AI 프로필)과 그 성향이 쓰는 공격 패턴 표.
//
// ECS 백엔드에서 "결정"은 개체가 아니라 표에 있다(MonsterAISystem.h). 성향도 같은 규칙을
// 따른다 — 개체가 갖는 것은 프로필 번호와 페이즈 번호, 두 바이트뿐이고 실제 값(어떤 스킬을
// 어느 사거리에서 쓰는가)은 전부 여기 공유 표에 있다. 몬스터가 4만 마리여도 표는 하나다.
//
//   Aggressive  기본값. 시야에 든 적을 먼저 문다. 예전 트리 그대로다.
//   Passive     먼저 공격하지 않는다. 그래서 적 탐지 스캔 자체를 돌지 않는다 —
//               맞았을 때 때린 쪽을 대상으로 받는 것이 유일한 교전 진입 경로다.
//   Boss        체력 비율 구간마다 공격 패턴(스킬 + 사거리)이 바뀐다.
//
// 성향은 monster.json 의 "ai" 필드가 정한다(없거나 모르는 값이면 Aggressive).
// 이 헤더는 ECS 를 모른다 — Monster 와 MonsterAISystem 이 함께 쓰기 때문이다.
//---------------------------------------------------------------------------------------

namespace monsterai
{
	// 몬스터 근접 공격 스킬 id(skill.json 의 monster_only 스킬).
	// 사거리/각도/데미지/쿨다운 튜닝은 전부 데이터에서 한다 — 여기 있는 사거리는
	// "AI 가 공격 자세를 잡는 거리"(맨해튼)이고, 실제 타격 판정은 스킬 데이터가 한다.
	inline constexpr int32_t kMeleeSkillId = 3;
	inline constexpr int32_t kBossCleaveSkillId = 4;  // 보스 1페이즈: 앞을 쓸어내는 강타
	inline constexpr int32_t kBossNovaSkillId = 5;    // 보스 2페이즈: 사방으로 터지는 노바

	inline constexpr int32_t kMeleeAttackRange = 3;
	inline constexpr int32_t kBossCleaveRange = 4;
	inline constexpr int32_t kBossNovaRange = 7;

	// 보스가 2페이즈로 넘어가는 체력 비율. 이 값 이하로 떨어지면 패턴이 바뀐다.
	inline constexpr float kBossPhase2HealthRatio = 0.5f;

	enum class AIProfile : uint8_t
	{
		Aggressive,
		Passive,
		Boss,
		Count,
	};

	// 한 페이즈의 공격 패턴. 프로필마다 체력 비율 내림차순으로 늘어놓는다.
	struct AttackPattern
	{
		float healthAbove;    // 이 패턴이 서는 체력 비율 하한(초과일 때 이 패턴)
		int32_t skillId;      // 시전할 스킬(플레이어와 같은 SkillSet::TryCast 경로를 탄다)
		int32_t attackRange;  // 공격 자세를 잡는 거리(맨해튼). 사거리 조건 패스가 쓴다.
	};

	struct ProfileTraits
	{
		// false 면 시야 스캔(Map::DetectEnemy)을 아예 돌지 않는다. 평화로운 몬스터가
		// 싼 이유이자, 먼저 공격하지 않는다는 규칙이 지켜지는 지점이다.
		bool scansForEnemies;

		const AttackPattern* patterns;
		uint8_t patternCount;
	};

	// 프로필 이름 → 프로필. 모르는 이름이면 Aggressive(데이터 오타가 몬스터를 멈추게 하지 않는다).
	// 데이터를 읽는 시점(스폰)에만 부르므로 헤더에 둘 이유가 없다.
	AIProfile ParseProfile(std::string_view name);

	// 로그/툴용 이름. ParseProfile 이 되받을 수 있는 문자열을 돌려준다.
	const char* ProfileName(AIProfile profile);

	//-----------------------------------------------------------------------------------
	// 표와 조회 함수는 헤더에 둔다.
	//
	// AI 의 가장 안쪽 루프가 개체마다 이것을 부른다(MonsterAISystem 의 조건 평가 패스,
	// BT 의 탐지 노드). .cpp 에 두면 링크 타임 최적화가 없는 이 빌드에서 인라인되지 않아
	// 틱마다 함수 호출이 남는다 — 재보니 교전 중 ECS 틱이 그만큼(약 8~10%) 느려졌다.
	// 전부 constexpr 이라 대개 컴파일 타임에 접힌다.
	//-----------------------------------------------------------------------------------

	// 기본 성향. 예전 트리와 같은 값이다(근접 스킬 하나, 사거리 3).
	inline constexpr AttackPattern kSingleMeleePattern[] = {
		{ 0.0f, kMeleeSkillId, kMeleeAttackRange },
	};

	// 보스. 체력이 절반 이하로 떨어지면 붙어서 때리던 것을 멈추고,
	// 더 먼 거리에서 사방으로 터지는 노바로 바꾼다.
	inline constexpr AttackPattern kBossPatterns[] = {
		{ kBossPhase2HealthRatio, kBossCleaveSkillId, kBossCleaveRange },
		{ 0.0f,                   kBossNovaSkillId,   kBossNovaRange },
	};

	inline constexpr ProfileTraits kProfileTraits[] = {
		/* Aggressive */ { true,  kSingleMeleePattern, 1 },
		/* Passive    */ { false, kSingleMeleePattern, 1 },
		/* Boss       */ { true,  kBossPatterns,       2 },
	};

	inline constexpr uint8_t kProfileCount = static_cast<uint8_t>(AIProfile::Count);

	static_assert(sizeof(kProfileTraits) / sizeof(kProfileTraits[0]) == kProfileCount,
		"프로필을 추가했으면 특성 표도 함께 채워야 한다");

	inline constexpr const ProfileTraits& TraitsOf(AIProfile profile)
	{
		const uint8_t index = static_cast<uint8_t>(profile);
		return kProfileTraits[index < kProfileCount ? index : 0];
	}

	// 체력 비율에 해당하는 페이즈 번호. 페이즈가 하나뿐인 프로필은 항상 0 이다.
	inline constexpr uint8_t PhaseFor(AIProfile profile, float healthRatio)
	{
		const ProfileTraits& traits = TraitsOf(profile);
		for (uint8_t phase = 0; phase + 1 < traits.patternCount; ++phase)
		{
			if (healthRatio > traits.patterns[phase].healthAbove)
				return phase;
		}
		return static_cast<uint8_t>(traits.patternCount - 1);
	}

	// 페이즈 번호에 해당하는 패턴. 범위를 벗어난 번호는 마지막 페이즈로 잘린다.
	inline constexpr const AttackPattern& PatternOf(AIProfile profile, uint8_t phase)
	{
		const ProfileTraits& traits = TraitsOf(profile);
		const uint8_t last = static_cast<uint8_t>(traits.patternCount - 1);
		return traits.patterns[phase < traits.patternCount ? phase : last];
	}
}
