#pragma once

#include <functional>
#include <memory>
#include <string>

namespace sql { class Connection; }

class Player;
struct PlayerLoadData;
struct PlayerSaveData;

//
// PlayerRepository
// ------------------------------------------------------------------
// 생성된 테이블 단위 DAO(PlayerDAO, ItemDAO, ...) 위에 얹는
// Player 애그리거트 단위 영속화 리포지토리.
//
// 비동기 실행(스레드 디스패치/모니터링/커넥션 획득)은 PlayerDbDispatcher
// 에 위임하고, 이 클래스는 "무엇을 읽고/쓸지"만 책임진다.
// Load/Save 모두 "레지스트리 -> 동기 *All -> Async*" 동일한 구조를 가진다.
//
class PlayerRepository
{
public:
    // 계정(userId)에 대응하는 캐릭터 행 id 를 찾고, 없으면 만든다. DB 스레드에서 호출한다.
    // authPlayerId 가 0 이 아니면(=인증이 행 id 를 알려줬으면) 그대로 쓴다.
    // 실패하면 0 을 돌려준다.
    static long long ResolveAccountRow(sql::Connection* conn,
                                       const std::string& userId,
                                       long long authPlayerId);

    // 비동기: 계정 행을 확정한 뒤 로드하고, 게임 스레드에서 Player 에 반영한다.
    // onComplete 는 반영이 끝난 뒤 게임 스레드에서 호출된다(성공 여부를 받는다).
    // 로그인 응답은 이 콜백에서 보내야 한다 — 그 전에는 어느 맵/좌표로 보낼지 알 수 없다.
    static void AsyncResolveAndLoad(std::shared_ptr<Player> player,
                                    const std::string& userId,
                                    long long authPlayerId,
                                    std::function<void(Player&, bool)> onComplete);

    // 비동기: DB 스레드에서 로드 후 게임 스레드에서 Player 에 반영.
    // (행 id 가 이미 확정된 경우에만 쓴다)
    static void AsyncLoad(std::shared_ptr<Player> player);

    // 비동기: 수집된 변경분을 DB 스레드에서 저장(fire-and-forget).
    static void AsyncSave(std::shared_ptr<Player> player,
                          std::shared_ptr<PlayerSaveData> data);

    // 동기(재사용/테스트용): 등록된 처리기들을 순회한다.
    static void LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data);
    static void SaveAll(sql::Connection* conn, const PlayerSaveData& data);
};
