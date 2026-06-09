#include "PlayerDataSaver.h"
#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "PlayerSaveData.h"
#include <iostream>
#include "SQL/generated/dao.h"


void PlayerDataSaver::AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerSaveData> data)
{
    boost::asio::post(player->GetStrand().value(), [data]() {

        sql::Connection* conn = SqlClientManager::getInstance().sqlClientPtr->getConnection();

        if (data->player)
        {
            PlayerDAO(conn).Update(*data->player);
        }

        if (data->items)
        {
            ItemDAO item_dao(conn);
            for (const auto& vo : *data->items)
                item_dao.Update(vo);
        }

        if (data->skills)
        {
            SkillDAO skill_dao(conn);
            for (const auto& vo : *data->skills)
                skill_dao.Update(vo);
        }

        if (data->quest_actives)
        {
            QuestActiveDAO quest_dao(conn);
            for (const auto& record : *data->quest_actives)
            {
                if (record.action == DbAction::Insert)
                    quest_dao.Insert(record.vo);
                else if (record.action == DbAction::Update)
                    quest_dao.Update(record.vo);
                else if (record.action == DbAction::Remove)
                    quest_dao.Delete(record.vo);
            }
        }

        if (data->quest_state)
        {
            QuestStateDAO state_dao(conn);
            if (data->quest_state->action == DbAction::Insert)
                state_dao.Insert(data->quest_state->vo);
            else
                state_dao.Update(data->quest_state->vo);
        }
    });
}
