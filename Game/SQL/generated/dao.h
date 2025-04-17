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