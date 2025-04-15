#pragma once
#include <mariadb/conncpp.hpp>
#include <string>

class PlayerDAO {
public:
    PlayerDAO(sql::Connection* conn);

    void Insert(std::string name, int level);
    void Update(int id, std::string name, int level);
    void Delete(int id);

private:
    sql::Connection* conn_;
};

// ----------------------------------------

class ItemDAO {
public:
    ItemDAO(sql::Connection* conn);

    void Insert(std::string name, int level);
    void Update(int id, std::string name, int level);
    void Delete(int id);

private:
    sql::Connection* conn_;
};