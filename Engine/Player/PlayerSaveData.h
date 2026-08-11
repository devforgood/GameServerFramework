#pragma once

#include <vector>
#include <optional>
#include "./SQL/generated/vo.h"
#include "DbRecord.h"

struct PlayerSaveData {
    std::optional<PlayerVO> player;
    std::optional<std::vector<DbRecord<PlayerItemVO>>> items;
    std::optional<std::vector<DbRecord<PlayerSkillVO>>> skills;
    std::optional<DbRecord<PlayerWalletVO>> wallet;
    std::optional<std::vector<DbRecord<QuestActiveVO>>> quest_actives;
    std::optional<DbRecord<QuestStateVO>> quest_state;
};
