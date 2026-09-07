#include "MonsterAIProfile.h"

#include <cstddef>
#include <iterator>

namespace
{
	using monsterai::AttackPattern;

	// 기본 성향. 예전 트리와 같은 값이다(근접 스킬 하나, 사거리 3).
	constexpr AttackPattern kSingleMeleePattern[] = {
		{ 0.0f, monsterai::kMeleeSkillId, monsterai::kMeleeAttackRange },
	};

	// 보스. 체력이 절반 이하로 떨어지면 붙어서 때리던 것을 멈추고,
	// 더 먼 거리에서 사방으로 터지는 노바로 바꾼다.
	constexpr AttackPattern kBossPatterns[] = {
		{ monsterai::kBossPhase2HealthRatio, monsterai::kBossCleaveSkillId, monsterai::kBossCleaveRange },
		{ 0.0f,                              monsterai::kBossNovaSkillId,   monsterai::kBossNovaRange },
	};

	constexpr monsterai::ProfileTraits kProfileTraits[] = {
		/* Aggressive */ { true,  kSingleMeleePattern, 1 },
		/* Passive    */ { false, kSingleMeleePattern, 1 },
		/* Boss       */ { true,  kBossPatterns,       2 },
	};

	static_assert(std::size(kProfileTraits) == static_cast<size_t>(monsterai::AIProfile::Count),
		"프로필을 추가했으면 특성 표도 함께 채워야 한다");

	uint8_t ProfileIndex(monsterai::AIProfile profile)
	{
		const uint8_t index = static_cast<uint8_t>(profile);
		return index < std::size(kProfileTraits) ? index : 0;
	}
}

monsterai::AIProfile monsterai::ParseProfile(std::string_view name)
{
	if (name == "passive")
		return AIProfile::Passive;
	if (name == "boss")
		return AIProfile::Boss;
	return AIProfile::Aggressive;
}

const char* monsterai::ProfileName(AIProfile profile)
{
	switch (profile)
	{
	case AIProfile::Passive: return "passive";
	case AIProfile::Boss:    return "boss";
	default:                 return "aggressive";
	}
}

const monsterai::ProfileTraits& monsterai::TraitsOf(AIProfile profile)
{
	return kProfileTraits[ProfileIndex(profile)];
}

uint8_t monsterai::PhaseFor(AIProfile profile, float healthRatio)
{
	const ProfileTraits& traits = TraitsOf(profile);
	for (uint8_t phase = 0; phase + 1 < traits.patternCount; ++phase)
	{
		if (healthRatio > traits.patterns[phase].healthAbove)
			return phase;
	}
	return traits.patternCount > 0 ? static_cast<uint8_t>(traits.patternCount - 1) : uint8_t{ 0 };
}

const monsterai::AttackPattern& monsterai::PatternOf(AIProfile profile, uint8_t phase)
{
	const ProfileTraits& traits = TraitsOf(profile);
	const uint8_t last = static_cast<uint8_t>(traits.patternCount - 1);
	return traits.patterns[phase < traits.patternCount ? phase : last];
}
