#pragma once
#include <string>
#include <chrono>


struct PlayerVO {
    int id;
    std::string name;
    int level;
};
// ----------------------------------------

struct ItemVO {
    int id;
    int player_id;
    int level;
};
// ----------------------------------------

struct SkillVO {
    int id;
    int player_id;
    int skill_id;
    int level;
};
// ----------------------------------------

struct QuestActiveVO {
    int character_id;
    int quest_id;
    int state;
    int progress1;
    int progress2;
    int progress3;
    std::chrono::system_clock::time_point accept_time;
};
// ----------------------------------------

struct QuestStateVO {
    int character_id;
    std::string flags;
};