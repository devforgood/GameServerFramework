#include "PlayerRepository.h"

#include <functional>
#include <memory>
#include <vector>

#include <mariadb/conncpp.hpp>

#include "LogHelper.h"
#include "Player.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerDbDispatcher.h"
#include "SQL/generated/dao.h"

namespace
{
    using LoaderFunc = std::function<void(sql::Connection*, long, PlayerLoadData&)>;
    using SaverFunc = std::function<void(sql::Connection*, const PlayerSaveData&)>;

    // DbRecord 의 action 을 DAO 호출로 옮긴다. 생성된 DAO 는 모두 같은 이름의
    // Insert/Update/Delete 를 가지므로 테이블마다 같은 코드를 반복할 이유가 없다.
    template<typename TDao, typename TVO>
    void ApplyRecord(TDao& dao, const DbRecord<TVO>& record)
    {
        switch (record.action)
        {
        case DbAction::Insert: dao.Insert(record.vo); break;
        case DbAction::Update: dao.Update(record.vo); break;
        case DbAction::Remove: dao.Delete(record.vo); break;
        }
    }

    // 읽기: 테이블별로 PlayerLoadData 를 채운다.
    const std::vector<LoaderFunc> kLoaders = {
        [](sql::Connection* conn, long id, PlayerLoadData& data) {
            PlayerDAO(conn).Select(id, data.player);
        },
        [](sql::Connection* conn, long id, PlayerLoadData& data) {
            data.items = PlayerItemDAO(conn).SelectByIndex(id);
        },
        [](sql::Connection* conn, long id, PlayerLoadData& data) {
            data.skills = PlayerSkillDAO(conn).SelectByIndex(id);
        },
        [](sql::Connection* conn, long id, PlayerLoadData& data) {
            PlayerWalletDAO(conn).Select(id, data.wallet);
        },
        [](sql::Connection* conn, long id, PlayerLoadData& data) {
            PlayerLocationDAO(conn).Select(id, data.location);
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
                PlayerItemDAO item_dao(conn);
                for (const auto& record : *data.items)
                    ApplyRecord(item_dao, record);
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.skills)
            {
                PlayerSkillDAO skill_dao(conn);
                for (const auto& record : *data.skills)
                    ApplyRecord(skill_dao, record);
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.wallet)
            {
                PlayerWalletDAO wallet_dao(conn);
                ApplyRecord(wallet_dao, *data.wallet);
            }
        },
        [](sql::Connection* conn, const PlayerSaveData& data) {
            if (data.location)
            {
                PlayerLocationDAO location_dao(conn);
                ApplyRecord(location_dao, *data.location);
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

namespace
{
    // name(=userId) 으로 캐릭터 행을 찾는다. 없으면 0.
    long long SelectIdByName(sql::Connection* conn, const std::string& userId)
    {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn->prepareStatement("SELECT id FROM player WHERE name = ?"));
        stmt->setString(1, userId);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next())
            return res->getInt64("id");
        return 0;
    }
}

long long PlayerRepository::ResolveAccountRow(sql::Connection* conn,
                                              const std::string& userId,
                                              long long authPlayerId)
{
    // 인증이 행 id 를 알려줬으면 그대로 쓴다(로비가 계정↔캐릭터 매핑을 아는 경우).
    if (authPlayerId != 0)
        return authPlayerId;

    if (conn == nullptr || userId.empty())
        return 0;

    try
    {
        if (const long long existing = SelectIdByName(conn, userId))
            return existing;

        // 첫 접속: 행을 만든다. name 은 UNIQUE 라, 동시에 두 세션이 들어오면
        // 한쪽만 성공한다 — 실패한 쪽은 다시 조회해 같은 행을 쓴다.
        try
        {
            PlayerVO vo{};
            vo.name = userId;
            vo.level = 1;
            vo.exp = 0;
            PlayerDAO(conn).Insert(vo);
        }
        catch (const std::exception&)
        {
            // UNIQUE 충돌로 보고 재조회한다. 정말 다른 오류였다면 아래에서 0 이 나온다.
        }

        return SelectIdByName(conn, userId);
    }
    catch (const std::exception& e)
    {
        LOG.error("ResolveAccountRow 실패 (userId '{}'): {}", userId, e.what());
        return 0;
    }
}

void PlayerRepository::AsyncResolveAndLoad(std::shared_ptr<Player> player,
                                           const std::string& userId,
                                           long long authPlayerId,
                                           std::function<void(Player&, bool)> onComplete)
{
    // 이 시점에는 아직 행 id 가 없으므로 디스패처가 넘겨주는 id 는 쓰지 않는다.
    PlayerDbDispatcher::DispatchWithoutId(player, "PlayerResolveLoad",
        [userId, authPlayerId](sql::Connection* conn) {
            auto result = std::make_shared<PlayerLoadResult>();
            result->dbPlayerId = ResolveAccountRow(conn, userId, authPlayerId);
            if (result->dbPlayerId == 0)
                return result; // ok = false

            LoadAll(conn, static_cast<long>(result->dbPlayerId), result->data);
            result->ok = true;
            return result;
        },
        [onComplete = std::move(onComplete)](Player& player, const PlayerLoadResult& result) {
            if (result.ok)
            {
                // 저장/로드 키를 확정한 뒤에 반영한다. 순서가 바뀌면 첫 저장이 0번 행으로 나간다.
                player.SetDbPlayerId(result.dbPlayerId);
                player.OnLoadedData(result.data);
            }
            onComplete(player, result.ok);
        });
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
    // 계정 행이 아직 확정되지 않았으면(로그인 전, 또는 확정 실패) 저장하지 않는다.
    // 이 검사가 없으면 0번 행이나 남의 행으로 나간다.
    if (player == nullptr || player->GetDbPlayerId() == 0)
        return;

    // DB 처리: 수집된 변경분을 저장한다. (결과 후처리 없음)
    PlayerDbDispatcher::Dispatch(player, "PlayerSave",
        [data](sql::Connection* conn, long /*player_id*/) {
            SaveAll(conn, *data);
        });
}
