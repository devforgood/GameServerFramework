#pragma once

#include <vector>
#include "./SQL/generated/vo.h"

struct PlayerLoadData {
    PlayerVO player;
    std::vector<PlayerItemVO> items;
    std::vector<PlayerSkillVO> skills;
    PlayerWalletVO wallet;
    PlayerLocationVO location;
    std::vector<QuestActiveVO> quest_actives;
    QuestStateVO quest_state;
};

// 계정 행 확정 + 로드를 한 번의 DB 왕복으로 처리한 결과.
// ok=false 면 행을 만들지도 찾지도 못한 것이라(DB 장애 등) 로그인을 진행하면 안 된다.
struct PlayerLoadResult {
    long long dbPlayerId = 0;
    PlayerLoadData data;
    bool ok = false;
};
