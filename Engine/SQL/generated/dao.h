#pragma once
#include <mariadb/conncpp.hpp>
#include <string>
#include <sstream>
#include <format>
#include <vector>
#include "vo.h"

inline std::chrono::system_clock::time_point fromMySQLDateTime(const sql::SQLString& dt) {
    std::chrono::system_clock::time_point tp;
    std::istringstream ss(dt.c_str());
    std::chrono::from_stream(ss, "%Y-%m-%d %H:%M:%S", tp);
    return tp;
}

inline std::string toMySQLDateTime(const std::chrono::system_clock::time_point& tp) {
    return std::format("{:%Y-%m-%d %H:%M:%S}", tp);
}

class PlayerDAO {
public:
    PlayerDAO(sql::Connection* conn);

    void Insert(const PlayerVO& vo);
    void Update(const PlayerVO& vo);
    void Delete(const PlayerVO& vo);

    // Select by primary key
    bool Select(int id, PlayerVO& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class ItemDAO {
public:
    ItemDAO(sql::Connection* conn);

    void Insert(const ItemVO& vo);
    void Update(const ItemVO& vo);
    void Delete(const ItemVO& vo);

    // Select by primary key
    bool Select(int id, ItemVO& out_vo);

    // Select by index columns (if any)
    std::vector<ItemVO> SelectByIndex(int player_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class SkillDAO {
public:
    SkillDAO(sql::Connection* conn);

    void Insert(const SkillVO& vo);
    void Update(const SkillVO& vo);
    void Delete(const SkillVO& vo);

    // Select by primary key
    bool Select(int id, SkillVO& out_vo);

    // Select by index columns (if any)
    std::vector<SkillVO> SelectByIndex(int player_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class QuestActiveDAO {
public:
    QuestActiveDAO(sql::Connection* conn);

    void Insert(const QuestActiveVO& vo);
    void Update(const QuestActiveVO& vo);
    void Delete(const QuestActiveVO& vo);

    // Select by primary key
    bool Select(int character_id, int quest_id, QuestActiveVO& out_vo);

    // Select by index columns (if any)
    std::vector<QuestActiveVO> SelectByIndex(int character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class QuestStateDAO {
public:
    QuestStateDAO(sql::Connection* conn);

    void Insert(const QuestStateVO& vo);
    void Update(const QuestStateVO& vo);
    void Delete(const QuestStateVO& vo);

    // Select by primary key
    bool Select(int character_id, QuestStateVO& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};