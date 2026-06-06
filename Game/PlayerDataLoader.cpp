#include "PlayerDataLoader.h"
#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include <iostream>

static const std::vector<DataLoaderFunc> player_data_loaders = {
    [](sql::Connection* conn, long id, PlayerData& data) {
        PlayerDAO(conn).Select(id, data.player);
    },
    [](sql::Connection* conn, long id, PlayerData& data) {
        data.items = ItemDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerData& data) {
        data.skills = SkillDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerData& data) {
        data.quest_actives = QuestActiveDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerData& data) {
        data.quest_state_found = QuestStateDAO(conn).Select(id, data.quest_state);
    }
};

void PlayerDataLoader::LoadAll(sql::Connection* conn, long player_id, PlayerData& out_data) {
    for (const auto& loader : player_data_loaders) {
        loader(conn, player_id, out_data);
    }
}

void PlayerDataLoader::AsyncLoad(std::shared_ptr<Player> player) {
    long player_id = player->player_id();
    int query_id = 2; // Can be a unique query tracker
    std::cout << "[Player " << player_id << "] Handling DB Query #" << query_id
        << " on post " << std::this_thread::get_id() << std::endl;

    auto session = player->get_session();
    if (!session) {
        std::cerr << "Session expired!" << std::endl;
        return;
    }
    auto io_context = player->get_server()->get_io_context();
    std::weak_ptr<Player> weak_player = player;

    boost::asio::post(player->get_strand().value(), [weak_player, player_id, query_id, io_context]() {
        std::cout << "[Player " << player_id << "] Handling DB Query #" << query_id
            << " on Thread " << std::this_thread::get_id() << std::endl;

        // Populate PlayerLoadData using the registered loaders
        auto loaded_data = std::make_shared<PlayerData>();
        sql::Connection* conn = SqlClientManager::getInstance().sqlClientPtr->getConnection();
        
        PlayerDataLoader::LoadAll(conn, player_id, *loaded_data);

        std::cout << "[Player " << player_id << "] Finished DB Query #" << query_id 
            << ", Loaded " << loaded_data->items.size() << " items, "
            << loaded_data->skills.size() << " skills." << std::endl;

        boost::asio::post(*io_context, [loaded_data, weak_player]() {
            if (auto player = weak_player.lock()) {
                std::cout << "[Player " << loaded_data->player.id << "] Player Name: " << loaded_data->player.name
                    << " on Thread " << std::this_thread::get_id() << std::endl;
                player->on_loaded_data(*loaded_data);
            }
        });
    });
}