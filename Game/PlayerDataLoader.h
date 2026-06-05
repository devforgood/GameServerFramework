#pragma once

#include <vector>
#include <functional>
#include <memory>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"

struct PlayerLoadData {
    PlayerVO player;
    std::vector<ItemVO> items;
    std::vector<SkillVO> skills;
    std::vector<QuestActiveVO> quest_actives;
    QuestStateVO quest_state;
    bool quest_state_found = false;
};

using DataLoaderFunc = std::function<void(sql::Connection*, long, PlayerLoadData&)>;

class Player;

class PlayerDataLoader {
public:
    static void LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data);
    static void AsyncLoad(std::shared_ptr<Player> player);
};