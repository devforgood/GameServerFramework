#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"



using DataLoaderFunc = std::function<void(sql::Connection*, long, PlayerData&)>;

class Player;

class PlayerDataLoader {
public:
    static void LoadAll(sql::Connection* conn, long player_id, PlayerData& out_data);
    static void AsyncLoad(std::shared_ptr<Player> player);
};