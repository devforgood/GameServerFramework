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
#include "LogHelper.h"

//
// PlayerDbDispatcher
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
class PlayerDbDispatcher
{
private:
    // 스레드 로컬 커넥션으로 DB 작업을 실행한다.
    // 정상 경로는 ping 없이 바로 실행하고(비용 0), 예외가 났을 때만
    // isConnectionValid() 로 "커넥션 끊김"과 "실제 쿼리 오류"를 구분한다.
    // 끊긴 경우에만 재연결 후 단 한 번 재시도한다.
    template <typename Fn>
    static auto RunWithReconnect(Fn&& fn) -> decltype(fn(static_cast<sql::Connection*>(nullptr)))
    {
        SqlClient* client = SqlClientManager::getInstance().sqlClientPtr.get();
        try {
            return fn(client->getConnection());
        }
        catch (const std::exception& e) {
            // 커넥션이 살아있으면 진짜 쿼리/데이터 오류 → 그대로 전파.
            if (client->isConnectionValid())
                throw;

            // 커넥션이 끊겼다 → 재연결 후 1회 재시도.
            LOG.warn("DB connection lost, reconnecting and retrying once: {}", e.what());
            sql::Connection* conn = client->reconnect();
            return fn(conn);
        }
    }

public:
    // 결과를 게임 스레드로 되돌려 후처리하는 작업(예: 로드).
    //   db_work     : DB 스레드에서 실행. 후처리로 넘길 결과(보통 shared_ptr)를 반환.
    //   on_complete : 결과를 들고 io_context 에서 실행. player 가 살아있을 때만 호출된다.
    template <typename DbWork, typename OnComplete>
    static void Dispatch(const std::shared_ptr<Player>& player,
                         std::string taskName,
                         DbWork&& dbWork,
                         OnComplete&& onComplete)
    {
        auto strand = player->GetStrand();
        if (!strand)
            return; // 세션 만료

        const long playerId = player->GetPlayerId();
        auto ioContext = player->GetServer()->get_io_context();
        std::weak_ptr<Player> weakPlayer = player;

        // DB 스레드 풀 모니터링: post 시점에 큐 대기 측정을 시작한다.
        const uint64_t monToken = DbThreadMonitor::Instance().BeginEnqueue(taskName, playerId);

        boost::asio::post(strand.value(),
            [playerId, ioContext, weakPlayer, monToken,
             dbWork = std::forward<DbWork>(dbWork),
             onComplete = std::forward<OnComplete>(onComplete)]() mutable
            {
                DbTaskScope mon_scope(monToken);

                auto result = RunWithReconnect(
                    [&](sql::Connection* conn) { return dbWork(conn, playerId); });

                boost::asio::post(*ioContext,
                    [weakPlayer, result = std::move(result),
                     onComplete = std::move(onComplete)]() mutable
                    {
                        if (auto player = weakPlayer.lock())
                            onComplete(*player, *result);
                    });
            });
    }

    // 결과 후처리가 필요 없는 fire-and-forget 작업(예: 저장).
    template <typename DbWork>
    static void Dispatch(const std::shared_ptr<Player>& player,
                         std::string taskName,
                         DbWork&& dbWork)
    {
        auto strand = player->GetStrand();
        if (!strand)
            return; // 세션 만료

        const long playerId = player->GetPlayerId();
        const uint64_t monToken = DbThreadMonitor::Instance().BeginEnqueue(taskName, playerId);

        boost::asio::post(strand.value(),
            [playerId, monToken, dbWork = std::forward<DbWork>(dbWork)]() mutable
            {
                DbTaskScope monScope(monToken);

                RunWithReconnect(
                    [&](sql::Connection* conn) { dbWork(conn, playerId); return 0; });
            });
    }
};
