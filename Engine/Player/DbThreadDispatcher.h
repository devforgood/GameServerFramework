#pragma once

#include <cstdint>
#include <memory>
#include <string>
#include <utility>
#include <boost/asio/post.hpp>
#include <mariadb/conncpp.hpp>

#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "DbThreadMonitor.h"

//
// DbThreadDispatcher
// ------------------------------------------------------------------
// 플레이어 관련 비동기 DB 작업을 DB 스레드 풀로 디스패치하는 헬퍼.
//
// 호출자는 두 가지 역할만 람다로 정의하면 된다.
//   1) DbWork     : DB 스레드에서 실행되는 실제 쿼리 처리.   (conn, player_id) -> result
//   2) OnComplete : 게임 스레드(io_context)에서 실행되는 결과 후처리. (player, result)
//
// io_context / strand / weak_player / 모니터링 토큰 / 커넥션 획득 같은
// 매번 반복되던 보일러플레이트는 모두 내부에서 캡슐화한다.
// 세션이 만료되어 strand 를 얻을 수 없으면 작업은 조용히 무시된다.
//
class DbThreadDispatcher
{
public:
    // 결과를 게임 스레드로 되돌려 후처리하는 작업(예: 로드).
    //   db_work     : DB 스레드에서 실행. 후처리로 넘길 결과(보통 shared_ptr)를 반환.
    //   on_complete : 결과를 들고 io_context 에서 실행. player 가 살아있을 때만 호출된다.
    template <typename DbWork, typename OnComplete>
    static void Dispatch(const std::shared_ptr<Player>& player,
                         std::string task_name,
                         DbWork&& db_work,
                         OnComplete&& on_complete)
    {
        auto strand = player->GetStrand();
        if (!strand)
            return; // 세션 만료

        const long player_id = player->GetPlayerId();
        auto io_context = player->GetServer()->get_io_context();
        std::weak_ptr<Player> weak_player = player;

        // DB 스레드 풀 모니터링: post 시점에 큐 대기 측정을 시작한다.
        const uint64_t mon_token = DbThreadMonitor::Instance().BeginEnqueue(task_name, player_id);

        boost::asio::post(strand.value(),
            [player_id, io_context, weak_player, mon_token,
             db_work = std::forward<DbWork>(db_work),
             on_complete = std::forward<OnComplete>(on_complete)]() mutable
            {
                DbTaskScope mon_scope(mon_token);

                sql::Connection* conn = SqlClientManager::getInstance().sqlClientPtr->getConnection();
                auto result = db_work(conn, player_id);

                boost::asio::post(*io_context,
                    [weak_player, result = std::move(result),
                     on_complete = std::move(on_complete)]() mutable
                    {
                        if (auto player = weak_player.lock())
                            on_complete(*player, *result);
                    });
            });
    }

    // 결과 후처리가 필요 없는 fire-and-forget 작업(예: 저장).
    template <typename DbWork>
    static void Dispatch(const std::shared_ptr<Player>& player,
                         std::string task_name,
                         DbWork&& db_work)
    {
        auto strand = player->GetStrand();
        if (!strand)
            return; // 세션 만료

        const long player_id = player->GetPlayerId();
        const uint64_t mon_token = DbThreadMonitor::Instance().BeginEnqueue(task_name, player_id);

        boost::asio::post(strand.value(),
            [player_id, mon_token, db_work = std::forward<DbWork>(db_work)]() mutable
            {
                DbTaskScope mon_scope(mon_token);

                sql::Connection* conn = SqlClientManager::getInstance().sqlClientPtr->getConnection();
                db_work(conn, player_id);
            });
    }
};
