#pragma once

#include <memory>

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
    // 비동기: DB 스레드에서 로드 후 게임 스레드에서 Player 에 반영.
    static void AsyncLoad(std::shared_ptr<Player> player);

    // 비동기: 수집된 변경분을 DB 스레드에서 저장(fire-and-forget).
    static void AsyncSave(std::shared_ptr<Player> player,
                          std::shared_ptr<PlayerSaveData> data);

    // 동기(재사용/테스트용): 등록된 처리기들을 순회한다.
    static void LoadAll(sql::Connection* conn, long player_id, PlayerLoadData& out_data);
    static void SaveAll(sql::Connection* conn, const PlayerSaveData& data);
};
