#pragma once

#include <vector>
#include "./SQL/generated/vo.h"

struct PlayerLoadData {
    PlayerVO player;
    std::vector<PlayerItemVO> items;
    std::vector<PlayerSkillVO> skills;
    PlayerWalletVO wallet;
    std::vector<QuestActiveVO> quest_actives;
    QuestStateVO quest_state;
};
