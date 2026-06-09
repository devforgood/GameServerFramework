#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"

struct PlayerSaveData {
    std::optional<PlayerVO> player;
    std::optional<std::vector<ItemVO>> items;
    std::optional<std::vector<SkillVO>> skills;
    std::optional<std::vector<QuestActiveVO>> quest_actives;
    std::optional<QuestStateVO> quest_state;
};