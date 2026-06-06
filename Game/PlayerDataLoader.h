#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"




class Player;
struct PlayerData;

class PlayerDataLoader {
public:
    static void LoadAll(sql::Connection* conn, long player_id, PlayerData& out_data);
    static void AsyncLoad(std::shared_ptr<Player> player);
};