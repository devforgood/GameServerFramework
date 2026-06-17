#include "PlayerDataLoader.h"
#include "PlayerLoadData.h"
#include "DbThreadDispatcher.h"

using DataLoaderFunc = std::function<void(sql::Connection*, long, PlayerLoadData&)>;

static const std::vector<DataLoaderFunc> player_data_loaders = {
    [](sql::Connection* conn, long id, PlayerLoadData& data) {
        PlayerDAO(conn).Select(id, data.player);
    },
    [](sql::Connection* conn, long id, PlayerLoadData& data) {
        data.items = ItemDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerLoadData& data) {
        data.skills = SkillDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerLoadData& data) {
        data.quest_actives = QuestActiveDAO(conn).SelectByIndex(id);
    },
    [](sql::Connection* conn, long id, PlayerLoadData& data) {
        QuestStateDAO(conn).Select(id, data.quest_state);
    }
};

void PlayerDataLoader::LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data) {
    for (const auto& loader : player_data_loaders) {
        loader(conn, player_id, out_data);
    }
}

void PlayerDataLoader::AsyncLoad(std::shared_ptr<Player> player) {
    DbThreadDispatcher::Dispatch(player, "PlayerLoad",
        // DB 처리: 등록된 로더들로 PlayerLoadData 를 채운다.
        [](sql::Connection* conn, long player_id) {
            auto loaded_data = std::make_shared<PlayerLoadData>();
            LoadAll(conn, player_id, *loaded_data);
            return loaded_data;
        },
        // 결과 처리: 게임 스레드에서 플레이어에 로드 결과를 반영한다.
        [](Player& player, const PlayerLoadData& data) {
            player.OnLoadedData(data);
        });
}