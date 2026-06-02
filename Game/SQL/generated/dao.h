#pragma once
#include <mariadb/conncpp.hpp>
#include <string>
#include <vector>

class PlayerDAO {
public:
    PlayerDAO(sql::Connection* conn);

    void Insert();
    void Update();
    void Delete();

    // Select by primary key
    bool Select(int id);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;

public:
    int id;
    std::string name;
    int level;
};



// ----------------------------------------

class ItemDAO {
public:
    ItemDAO(sql::Connection* conn);

    void Insert();
    void Update();
    void Delete();

    // Select by primary key
    bool Select(int id);

    // Select by index columns (if any)
    std::vector<ItemDAO> SelectByIndex(int player_id);

private:
    sql::Connection* conn_;

public:
    int id;
    int player_id;
    int level;
};



// ----------------------------------------

class SkillDAO {
public:
    SkillDAO(sql::Connection* conn);

    void Insert();
    void Update();
    void Delete();

    // Select by primary key
    bool Select(int id);

    // Select by index columns (if any)
    std::vector<SkillDAO> SelectByIndex(int player_id);

private:
    sql::Connection* conn_;

public:
    int id;
    int player_id;
    int skill_id;
    int level;
};



// ----------------------------------------

class QuestActiveDAO {
public:
    QuestActiveDAO(sql::Connection* conn);

    void Insert();
    void Update();
    void Delete();

    // Select by primary key
    bool Select(int character_id, int quest_id);

    // Select by index columns (if any)
    std::vector<QuestActiveDAO> SelectByIndex(int character_id);

private:
    sql::Connection* conn_;

public:
    int character_id;
    int quest_id;
    int state;
    int progress1;
    int progress2;
    int progress3;
    std::string accept_time;
};



// ----------------------------------------

class QuestStateDAO {
public:
    QuestStateDAO(sql::Connection* conn);

    void Insert();
    void Update();
    void Delete();

    // Select by primary key
    bool Select(int character_id);

    // Select by index columns (if any)

private:
    sql::Connection* conn_;

public:
    int character_id;
    std::string flags;
};