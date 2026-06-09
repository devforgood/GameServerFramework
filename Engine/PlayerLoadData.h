#pragma once

#include <vector>
#include "./SQL/generated/vo.h"

struct PlayerLoadData {
    PlayerVO player;
    std::vector<ItemVO> items;
    std::vector<SkillVO> skills;
    std::vector<QuestActiveVO> quest_actives;
    QuestStateVO quest_state;
};