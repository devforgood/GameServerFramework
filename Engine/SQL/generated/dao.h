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
    bool Select(long long id, PlayerVO& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class PlayerLocationDAO {
public:
    PlayerLocationDAO(sql::Connection* conn);

    void Insert(const PlayerLocationVO& vo);
    void Update(const PlayerLocationVO& vo);
    void Delete(const PlayerLocationVO& vo);

    // Select by primary key
    bool Select(long long character_id, PlayerLocationVO& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class PlayerItemDAO {
public:
    PlayerItemDAO(sql::Connection* conn);

    void Insert(const PlayerItemVO& vo);
    void Update(const PlayerItemVO& vo);
    void Delete(const PlayerItemVO& vo);

    // Select by primary key
    bool Select(long long character_id, int item_id, PlayerItemVO& out_vo);

    // Select by index columns (if any)
    std::vector<PlayerItemVO> SelectByIndex(long long character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class PlayerSkillDAO {
public:
    PlayerSkillDAO(sql::Connection* conn);

    void Insert(const PlayerSkillVO& vo);
    void Update(const PlayerSkillVO& vo);
    void Delete(const PlayerSkillVO& vo);

    // Select by primary key
    bool Select(long long character_id, int skill_id, PlayerSkillVO& out_vo);

    // Select by index columns (if any)
    std::vector<PlayerSkillVO> SelectByIndex(long long character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class PlayerWalletDAO {
public:
    PlayerWalletDAO(sql::Connection* conn);

    void Insert(const PlayerWalletVO& vo);
    void Update(const PlayerWalletVO& vo);
    void Delete(const PlayerWalletVO& vo);

    // Select by primary key
    bool Select(long long character_id, PlayerWalletVO& out_vo);

    // Select by index columns (if any)

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
    bool Select(long long character_id, int quest_id, QuestActiveVO& out_vo);

    // Select by index columns (if any)
    std::vector<QuestActiveVO> SelectByIndex(long long character_id);

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
    bool Select(long long character_id, QuestStateVO& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class SessionTokenDAO {
public:
    SessionTokenDAO(sql::Connection* conn);

    void Insert(const SessionTokenVO& vo);
    void Update(const SessionTokenVO& vo);
    void Delete(const SessionTokenVO& vo);

    // Select by primary key
    bool Select(std::string token, SessionTokenVO& out_vo);

    // Select by index columns (if any)
    std::vector<SessionTokenVO> SelectByIndex(std::string user_id);

private:
    sql::Connection* conn_;
};