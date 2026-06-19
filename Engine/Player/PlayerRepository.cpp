#include "PlayerRepository.h"

#include <functional>
#include <vector>

#include "Player.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerDbDispatcher.h"
#include "SQL/generated/dao.h"

namespace
{
    using LoaderFunc = std::function<void(sql::Connection*, long, PlayerLoadData&)>;
    using SaverFunc = std::function<void(sql::Connection*, const PlayerSaveData&)>;

    // 읽기: 테이블별로 PlayerLoadData 를 채운다.
    const std::vector<LoaderFunc> kLoaders = {
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
        },
    };

    // 쓰기: 변경된(optional 이 채워진) 테이블만 반영한다. kLoaders 와 대칭.
    const std::vector<SaverFunc> kSavers = {
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.player)
                PlayerDAO(conn).Update(*data.player);
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.items)
            {
                ItemDAO item_dao(conn);
                for (const auto& vo : *data.items)
                    item_dao.Update(vo);
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.skills)
            {
                SkillDAO skill_dao(conn);
                for (const auto& vo : *data.skills)
                    skill_dao.Update(vo);
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.quest_actives)
            {
                QuestActiveDAO quest_dao(conn);
                for (const auto& record : *data.quest_actives)
                {
                    if (record.action == DbAction::Insert)
                        quest_dao.Insert(record.vo);
                    else if (record.action == DbAction::Update)
                        quest_dao.Update(record.vo);
                    else if (record.action == DbAction::Remove)
                        quest_dao.Delete(record.vo);
                }
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.quest_state)
            {
                QuestStateDAO state_dao(conn);
                if (data.quest_state->action == DbAction::Insert)
                    state_dao.Insert(data.quest_state->vo);
                else
                    state_dao.Update(data.quest_state->vo);
            }
        },
    };
}

void PlayerRepository::LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data)
{
    for (const auto& loader : kLoaders)
        loader(conn, player_id, out_data);
}

void PlayerRepository::SaveAll(sql::Connection* conn, const PlayerSaveData& data)
{
    for (const auto& saver : kSavers)
        saver(conn, data);
}

void PlayerRepository::AsyncLoad(std::shared_ptr<Player> player)
{
    PlayerDbDispatcher::Dispatch(player, "PlayerLoad",
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

void PlayerRepository::AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerSaveData> data)
{
    // DB 처리: 수집된 변경분을 저장한다. (결과 후처리 없음)
    PlayerDbDispatcher::Dispatch(player, "PlayerSave",
        [data](sql::Connection* conn, long /*player_id*/) {
            SaveAll(conn, *data);
        });
}
