#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"
#include "GameObject.h"


class Player;
struct PlayerLoadData;

class PlayerDataLoader
{
public:
    static void LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data);
    static void AsyncLoad(std::shared_ptr<Player> player);
};