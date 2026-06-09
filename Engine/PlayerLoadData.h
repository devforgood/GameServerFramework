#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"

struct PlayerLoadData {
    PlayerVO player;
    std::vector<ItemVO> items;
    std::vector<SkillVO> skills;
    std::vector<QuestActiveVO> quest_actives;
    QuestStateVO quest_state;
};