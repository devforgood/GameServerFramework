#pragma once
#include <string>
#include <chrono>
#include <sstream>
#include <format>
#include <mariadb/conncpp.hpp>

inline std::chrono::system_clock::time_point fromMySQLDateTime(const sql::SQLString& dt) {
    std::chrono::system_clock::time_point tp;
    std::istringstream ss(dt.c_str());
    std::chrono::from_stream(ss, "%Y-%m-%d %H:%M:%S", tp);
    return tp;
}

inline std::string toMySQLDateTime(const std::chrono::system_clock::time_point& tp) {
    return std::format("{:%Y-%m-%d %H:%M:%S}", tp);
}


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