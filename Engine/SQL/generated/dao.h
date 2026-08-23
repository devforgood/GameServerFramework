#pragma once

// ============================================================
// 이 파일은 SqlCodeGenerator 가 자동 생성한다. 직접 수정하지 마세요.
// 고칠 곳: SqlCodeGenerator/schema.xml + templates/dao.h.j2
// 다시 만들기: SqlCodeGenerator/build.bat (또는 python generate.py)
// ------------------------------------------------------------
// DAO = Data Access Object (데이터 접근 객체)
//   테이블 하나에 대한 CRUD 만 담당한다. 커넥션을 들고 SQL 을 실행할 뿐,
//   여러 테이블을 묶는 규칙이나 게임 로직은 갖지 않는다.
//   그 위층(캐릭터 단위로 묶어 저장/로드) 은 Engine/Player/PlayerRepository.
//   다루는 행 구조체는 VO(vo.h).
// ============================================================

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

class DAOPlayer {
public:
    DAOPlayer(sql::Connection* conn);

    void Insert(const VOPlayer& vo);
    void Update(const VOPlayer& vo);
    void Delete(const VOPlayer& vo);

    // Select by primary key
    bool Select(long long id, VOPlayer& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOPlayerLocation {
public:
    DAOPlayerLocation(sql::Connection* conn);

    void Insert(const VOPlayerLocation& vo);
    void Update(const VOPlayerLocation& vo);
    void Delete(const VOPlayerLocation& vo);

    // Select by primary key
    bool Select(long long character_id, VOPlayerLocation& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOPlayerItem {
public:
    DAOPlayerItem(sql::Connection* conn);

    void Insert(const VOPlayerItem& vo);
    void Update(const VOPlayerItem& vo);
    void Delete(const VOPlayerItem& vo);

    // Select by primary key
    bool Select(long long character_id, int item_id, VOPlayerItem& out_vo);

    // Select by index columns (if any)
    std::vector<VOPlayerItem> SelectByIndex(long long character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOPlayerSkill {
public:
    DAOPlayerSkill(sql::Connection* conn);

    void Insert(const VOPlayerSkill& vo);
    void Update(const VOPlayerSkill& vo);
    void Delete(const VOPlayerSkill& vo);

    // Select by primary key
    bool Select(long long character_id, int skill_id, VOPlayerSkill& out_vo);

    // Select by index columns (if any)
    std::vector<VOPlayerSkill> SelectByIndex(long long character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOPlayerWallet {
public:
    DAOPlayerWallet(sql::Connection* conn);

    void Insert(const VOPlayerWallet& vo);
    void Update(const VOPlayerWallet& vo);
    void Delete(const VOPlayerWallet& vo);

    // Select by primary key
    bool Select(long long character_id, VOPlayerWallet& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOQuestActive {
public:
    DAOQuestActive(sql::Connection* conn);

    void Insert(const VOQuestActive& vo);
    void Update(const VOQuestActive& vo);
    void Delete(const VOQuestActive& vo);

    // Select by primary key
    bool Select(long long character_id, int quest_id, VOQuestActive& out_vo);

    // Select by index columns (if any)
    std::vector<VOQuestActive> SelectByIndex(long long character_id);

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOQuestState {
public:
    DAOQuestState(sql::Connection* conn);

    void Insert(const VOQuestState& vo);
    void Update(const VOQuestState& vo);
    void Delete(const VOQuestState& vo);

    // Select by primary key
    bool Select(long long character_id, VOQuestState& out_vo);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;
};



// ----------------------------------------

class DAOSessionToken {
public:
    DAOSessionToken(sql::Connection* conn);

    void Insert(const VOSessionToken& vo);
    void Update(const VOSessionToken& vo);
    void Delete(const VOSessionToken& vo);

    // Select by primary key
    bool Select(std::string token, VOSessionToken& out_vo);

    // Select by index columns (if any)
    std::vector<VOSessionToken> SelectByIndex(std::string user_id);

private:
    sql::Connection* conn_;
};