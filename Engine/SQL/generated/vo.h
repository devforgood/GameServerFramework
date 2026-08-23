#pragma once

// ============================================================
// 이 파일은 SqlCodeGenerator 가 자동 생성한다. 직접 수정하지 마세요.
// 고칠 곳: SqlCodeGenerator/schema.xml + templates/vo.h.j2
// 다시 만들기: SqlCodeGenerator/build.bat (또는 python generate.py)
// ------------------------------------------------------------
// VO = Value Object (값 객체)
//   테이블 한 행을 그대로 담는 구조체. 컬럼 이름/타입이 1:1 로 대응한다.
//   DB 와 주고받는 그릇일 뿐이라 게임 로직을 여기에 두지 않는다.
//   읽고 쓰는 쪽은 DAO(dao.h), 애그리거트 단위 저장/로드는
//   Engine/Player/PlayerRepository 가 맡는다.
// ============================================================

#include <string>
#include <chrono>


struct VOPlayer {
    long long id;
    std::string name;
    int level;
    long long exp;
};
// ----------------------------------------

struct VOPlayerLocation {
    long long character_id;
    int map_id;
    double x;
    double y;
    double z;
};
// ----------------------------------------

struct VOPlayerItem {
    long long character_id;
    int item_id;
    int count;
};
// ----------------------------------------

struct VOPlayerSkill {
    long long character_id;
    int skill_id;
    int level;
};
// ----------------------------------------

struct VOPlayerWallet {
    long long character_id;
    long long gold;
};
// ----------------------------------------

struct VOQuestActive {
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

struct VOQuestState {
    long long character_id;
    std::string flags;
};
// ----------------------------------------

struct VOSessionToken {
    std::string token;
    std::string user_id;
    long long player_id;
    std::chrono::system_clock::time_point issued_at;
};