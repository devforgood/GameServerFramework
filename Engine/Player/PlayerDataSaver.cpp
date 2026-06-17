#include "PlayerDataSaver.h"
#include "PlayerSaveData.h"
#include "DbThreadDispatcher.h"
#include "SQL/generated/dao.h"


void PlayerDataSaver::AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerSaveData> data)
{
    // DB 처리: 수집된 변경분을 저장한다. (결과 후처리 없음)
    DbThreadDispatcher::Dispatch(player, "PlayerSave",
        [data](sql::Connection* conn, long /*player_id*/) {
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

