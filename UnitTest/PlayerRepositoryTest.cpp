#include <gtest/gtest.h>

#include "PlayerRepository.h"
#include "PlayerSaveData.h"
#include "DbRecord.h"
#include "SQL/generated/vo.h"

// PlayerRepository 단위 테스트.
//
// PlayerRepository 는 생성된 DAO(DAOPlayer/DAOPlayerItem/...) 위에 얹는 얇은
// 오케스트레이션 계층이라, 실제 쿼리 동작은 살아있는 DB 연결이 필요하다.
// 여기서는 DB 연결 없이 검증 가능한 핵심 계약만 다룬다.
//
// 검증 대상: SaveAll 의 "변경분 게이팅" 계약.
//   - 게임의 1분 단위 지연 저장은 Dirty 컴포넌트만 PlayerSaveData 에 담아 넘긴다.
//   - 따라서 채워지지 않은(optional 이 비어있는) 필드에 대해서는 DAO/커넥션을
//     절대 건드리면 안 된다.
//   DAO 생성자는 포인터만 보관하고 쿼리 메서드에서만 conn 을 역참조하므로,
//   "기록할 게 없을 때" SaveAll 에 null 커넥션을 넘겨도 안전해야 한다.
//   게이팅이 깨져 어떤 필드를 항상 기록하면 이 테스트에서 null 역참조로 죽는다.

// 아무 변경분도 없는 PlayerSaveData 는 어떤 DAO 쿼리도 실행하지 않는다.
TEST(PlayerRepositoryTest, EmptySaveDataTouchesNoConnection)
{
    PlayerSaveData data{}; // 모든 optional 이 nullopt

    PlayerRepository::SaveAll(nullptr, data);

    SUCCEED(); // null 역참조 없이 여기까지 도달하면 게이팅이 올바른 것
}

// 컬렉션 필드가 "비어있지만 존재"하는 경우(빈 vector)에도 쿼리는 발생하지 않는다.
// (DAO 는 생성되지만 순회 대상이 없어 Update/Insert/Delete 가 호출되지 않아야 한다.)
TEST(PlayerRepositoryTest, EmptyCollectionsIssueNoQuery)
{
    PlayerSaveData data{};
    data.items = std::vector<DbRecord<VOPlayerItem>>{};
    data.skills = std::vector<DbRecord<VOPlayerSkill>>{};
    data.quest_actives = std::vector<DbRecord<VOQuestActive>>{};
    // player / quest_state 는 단일 레코드라 set 되면 즉시 기록되므로 비워둔다.

    PlayerRepository::SaveAll(nullptr, data);

    SUCCEED();
}

// quest_actives 가 set 되어도 레코드가 없으면 어떤 동작(Insert/Update/Delete)도
// 분기되지 않는다 — action 매핑이 레코드 단위로만 동작함을 확인.
TEST(PlayerRepositoryTest, NoQuestActiveRecordsDispatchNoAction)
{
    PlayerSaveData data{};
    data.quest_actives = std::vector<DbRecord<VOQuestActive>>{}; // 레코드 0개

    PlayerRepository::SaveAll(nullptr, data);

    SUCCEED();
}
