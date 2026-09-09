#include "CheatCommands.h"

#include <algorithm>
#include <vector>

#include "GameObject.h"
#include "Player.h"
#include "PlayerSkill.h"
#include "GameData/ResourceLoader.h"
#include "gamedata.h"
#include "LogHelper.h"

namespace
{
	// 앞뒤 공백을 벗기고, 명령 앞에 붙은 '/' 도 벗긴다.
	// 채팅 창에 "/allskill" 로 쳐도 "allskill" 로 쳐도 같은 명령이 되게 한다.
	std::string_view Trim(std::string_view text)
	{
		const auto isSpace = [](char c) { return c == ' ' || c == '\t' || c == '\r' || c == '\n'; };
		while (!text.empty() && isSpace(text.front()))
			text.remove_prefix(1);
		while (!text.empty() && isSpace(text.back()))
			text.remove_suffix(1);
		return text;
	}

	// 첫 낱말을 명령 이름으로 떼어낸다. 인자를 받는 치트가 생기면 나머지를 여기서 넘긴다.
	std::string_view FirstWord(std::string_view text)
	{
		const size_t end = text.find_first_of(" \t");
		return end == std::string_view::npos ? text : text.substr(0, end);
	}

	// 배운 스킬을 캐릭터에 즉시 싣는다. 이게 없으면 다음 접속까지 못 쓴다
	// (캐릭터의 SkillSet 은 빙의 시점에 한 번만 채워진다 — PlayerQuest 의 스킬 보상과 같다).
	void ReloadCharacterSkills(GameObject* player)
	{
		if (auto* asPlayer = dynamic_cast<Player*>(player))
			asPlayer->ApplyOwnedSkillsToCharacter();
	}

	//-----------------------------------------------------------------------------------
	// allskill : skill.json 의 플레이어 스킬을 전부 습득한다.
	//
	// 몬스터 전용(monster_only)은 뺀다. 넣어 봐야 SkillSet::InitFromOwned 가 거르므로
	// player_skill 테이블에 쓸 수 없는 줄만 쌓인다. 패시브(오라)는 넣는다 — 보유만으로
	// 적용되는 진짜 플레이어 스킬이다.
	//-----------------------------------------------------------------------------------
	cheat::Result AllSkill(GameObject* player)
	{
		cheat::Result result;
		result.handled = true;

		auto* skills = player->GetComponent<PlayerSkill>();
		if (skills == nullptr)
		{
			result.reply = "치트 allskill: 이 캐릭터에는 스킬 컴포넌트가 없습니다.";
			return result;
		}

		int learned = 0;
		int total = 0;
		for (const auto& pair : ResourceLoader::Instance().GetSkills())
		{
			const gamedata::Skill* data = pair.second;
			if (data == nullptr || data->monster_only)
				continue;

			++total;
			if (skills->LearnSkill(static_cast<int>(pair.first)))
				++learned;
		}

		ReloadCharacterSkills(player);

		result.reply = "치트 allskill: 스킬 " + std::to_string(total) + "개 보유"
			+ " (이번에 새로 배운 것 " + std::to_string(learned) + "개).";
		return result;
	}

	struct Command
	{
		const char* name;
		const char* usage;
		cheat::Result (*run)(GameObject* player);
	};

	// 치트를 추가하려면 여기 한 줄 넣고 함수를 하나 쓰면 된다. help 목록도 이 표에서 나온다.
	constexpr Command kCommands[] = {
		{ "allskill", "allskill  - 플레이어가 쓸 수 있는 모든 스킬을 습득한다", &AllSkill },
	};
}

namespace cheat
{

Result Execute(GameObject* player, std::string_view line)
{
	Result result;

	if (player == nullptr)
	{
		result.reply = "치트: 캐릭터가 없어 실행할 수 없습니다.";
		return result;
	}

	std::string_view text = Trim(line);
	if (!text.empty() && text.front() == '/')
		text = Trim(text.substr(1));

	if (text.empty())
		return result; // 빈 줄은 조용히 무시한다(엔터만 친 경우)

	const std::string_view name = FirstWord(text);

	// help 는 표를 그대로 읽어 주는 것이라 표 밖에 둔다(자기 자신을 목록에 넣지 않는다).
	if (name == "help" || name == "?")
	{
		result.handled = true;
		result.reply = "치트 목록:";
		for (const Command& command : kCommands)
			result.reply += "\n  " + std::string(command.usage);
		return result;
	}

	for (const Command& command : kCommands)
	{
		if (name != command.name)
			continue;

		// 치트는 운영에서 열려 있으면 안 되는 기능이라, 실행 사실 자체를 warn 으로 남긴다.
		auto* asPlayer = dynamic_cast<Player*>(player);
		LOG.warn("치트 실행: '{}' (player {})", std::string(name),
			asPlayer != nullptr ? asPlayer->GetPlayerId() : 0);
		return command.run(player);
	}

	result.reply = "알 수 없는 치트: '" + std::string(name) + "'. 'help' 로 목록을 봅니다.";
	return result;
}

} // namespace cheat
