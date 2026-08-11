#pragma once
#include <string>
#include <chrono>


struct PlayerVO {
    long long id;
    std::string name;
    int level;
};
// ----------------------------------------

struct PlayerItemVO {
    long long character_id;
    int item_id;
    int count;
};
// ----------------------------------------

struct PlayerSkillVO {
    long long character_id;
    int skill_id;
    int level;
};
// ----------------------------------------

struct PlayerWalletVO {
    long long character_id;
    long long gold;
};
// ----------------------------------------

struct QuestActiveVO {
    long long character_id;
    int quest_id;
    int state;
    int stage;
    int progress1;
    int progress2;
    int progress3;
    std::chrono::system_clock::time_point accept_time;
};
// ----------------------------------------

struct QuestStateVO {
    long long character_id;
    std::string flags;
};