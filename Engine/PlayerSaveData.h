#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"
#include "DbRecord.h"

struct PlayerSaveData {
    std::optional<PlayerVO> player;
    std::optional<std::vector<ItemVO>> items;
    std::optional<std::vector<SkillVO>> skills;
    std::optional<std::vector<DbRecord<QuestActiveVO>>> quest_actives;
    std::optional<DbRecord<QuestStateVO>> quest_state;
};