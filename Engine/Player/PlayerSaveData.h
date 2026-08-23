#pragma once

#include <utility>
#include <vector>
#include "./SQL/generated/vo.h"
#include "DbRecord.h"

//
// DbSaveField
// ------------------------------------------------------------------
// std::optional 이 있던 자리에 쓰는 "비워도 버퍼는 남는" 필드.
//
// optional 은 reset() 하면 안에 든 vector/string 까지 파괴한다. 저장 버퍼를
// 돌려쓰려면 그 버퍼(용량)가 남아 있어야 하므로, 값은 그대로 두고 "있음/없음"
// 만 끄는 타입이 필요하다.
//
// 값을 넣는 방법은 두 가지다.
//   - emplace()  : 남아 있는 버퍼를 그대로 받아 그 자리에 채운다(재할당 없음).
//   - operator=  : 새 값을 옮겨 담는다. 이때는 기존 버퍼가 버려진다.
// 행이 여러 개인 필드(items/skills/quest_actives)는 emplace() 로 채운다.
//
template<typename T>
class DbSaveField
{
public:
    [[nodiscard]] bool has_value() const noexcept { return has_; }
    explicit operator bool() const noexcept { return has_; }

    T&       operator*()        noexcept { return value_; }
    const T& operator*()  const noexcept { return value_; }
    T*       operator->()       noexcept { return &value_; }
    const T* operator->() const noexcept { return &value_; }

    // "값 있음" 으로 표시하고 내부 버퍼를 그대로 돌려준다.
    // 이전 내용을 지우는 건 채우는 쪽 몫이다(트래커의 Flush(out) 이 그렇게 한다).
    T& emplace() noexcept { has_ = true; return value_; }

    DbSaveField& operator=(T&& v)      { value_ = std::move(v); has_ = true; return *this; }
    DbSaveField& operator=(const T& v) { value_ = v;            has_ = true; return *this; }

    // "값 없음" 으로 되돌리되 버퍼는 유지한다. 이 타입의 존재 이유.
    void reset() noexcept { has_ = false; }

private:
    T    value_{};
    bool has_ = false;
};

//
// PlayerSaveData
// ------------------------------------------------------------------
// 한 번의 저장에서 DB 로 나갈 변경분. 값이 채워진 필드만 기록된다.
// PlayerSaveBufferPool 이 이 구조체를 돌려쓰므로, Reset() 뒤에도 각 필드의
// 버퍼는 남아 다음 저장에서 재사용된다.
//
struct PlayerSaveData {
    DbSaveField<VOPlayer>                            player;
    DbSaveField<std::vector<DbRecord<VOPlayerItem>>> items;
    DbSaveField<std::vector<DbRecord<VOPlayerSkill>>> skills;
    DbSaveField<DbRecord<VOPlayerWallet>>            wallet;
    DbSaveField<DbRecord<VOPlayerLocation>>          location;
    DbSaveField<std::vector<DbRecord<VOQuestActive>>> quest_actives;
    DbSaveField<DbRecord<VOQuestState>>              quest_state;

    // 모든 필드를 "없음" 으로 되돌린다. 값과 버퍼는 그대로 남는다.
    void Reset() noexcept
    {
        player.reset();
        items.reset();
        skills.reset();
        wallet.reset();
        location.reset();
        quest_actives.reset();
        quest_state.reset();
    }
};
