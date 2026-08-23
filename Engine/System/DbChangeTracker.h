#pragma once
#include <unordered_map>
#include <vector>
#include <cstdint>
#include "DbRecord.h"

// ============================================================
// DB 변경 추적 표준 컨테이너
//
// 컴포넌트마다 "신규/수정/삭제"를 제각각 추적하던 보일러플레이트를
// 정형화한다. 컴포넌트는 아래 두 컨테이너만 멤버로 두고:
//   - Load 시  : AddPersisted / SetPersisted 로 DB 의 기존 상태를 채우고
//   - 게임 로직 : Add / Modify / Remove / MarkDirty 로 변경하고
//   - Save 시  : Flush() 로 변경분만 DbRecord 목록으로 뽑아낸다.
// ============================================================

// 키 기반 행 컬렉션(예: 진행 중 퀘스트, 인벤토리 아이템)의 DB 변경을 추적.
// 변경된 행만 INSERT/UPDATE/DELETE 레코드로 만든다("변경분만 기록").
template<typename TKey, typename TVO>
class DbCollectionTracker
{
public:
    // Load 경로: DB 에 이미 존재하는 행을 변경 없음(Persisted) 상태로 채운다.
    void AddPersisted(const TKey& key, TVO vo)
    {
        rows_[key] = Entry{ std::move(vo), RowState::Persisted };
    }

    // 신규 행 추가 (저장 시 INSERT). 이미 같은 키가 있으면 false.
    bool Add(const TKey& key, TVO vo)
    {
        if (rows_.find(key) != rows_.end())
            return false;
        rows_[key] = Entry{ std::move(vo), RowState::New };
        return true;
    }

    bool Contains(const TKey& key) const
    {
        return rows_.find(key) != rows_.end();
    }

    // 읽기 전용 접근. 없으면 nullptr.
    const TVO* Find(const TKey& key) const
    {
        auto it = rows_.find(key);
        return it == rows_.end() ? nullptr : &it->second.vo;
    }

    // 수정용 접근. 기존(Persisted) 행은 Modified 로 표시(저장 시 UPDATE),
    // 신규(New) 행은 New 유지(저장 시 INSERT). 없으면 nullptr.
    TVO* Modify(const TKey& key)
    {
        auto it = rows_.find(key);
        if (it == rows_.end())
            return nullptr;
        if (it->second.state == RowState::Persisted)
            it->second.state = RowState::Modified;
        return &it->second.vo;
    }

    // 행 제거. 기존/수정 행은 DELETE 대기열로, 신규 행은 그냥 폐기(DB 에 없으므로).
    bool Remove(const TKey& key)
    {
        auto it = rows_.find(key);
        if (it == rows_.end())
            return false;
        if (it->second.state != RowState::New)
            deleted_.push_back(std::move(it->second.vo));
        rows_.erase(it);
        return true;
    }

    // 키 스냅샷. 순회 도중 Add/Remove 로 컨테이너가 바뀌어도 안전하도록 복사해 준다.
    // out 을 재사용하면 반복 호출해도 추가 할당이 없다.
    void CollectKeys(std::vector<TKey>& out) const
    {
        out.clear();
        out.reserve(rows_.size());
        for (const auto& [key, entry] : rows_)
            out.push_back(key);
    }

    size_t Size() const { return rows_.size(); }

    // 저장할 변경분이 있는지 (신규/수정/삭제 중 하나라도)
    bool HasPendingChanges() const
    {
        if (!deleted_.empty())
            return true;
        for (const auto& [key, entry] : rows_)
            if (entry.state != RowState::Persisted)
                return true;
        return false;
    }

    void Clear()
    {
        rows_.clear();
        deleted_.clear();
    }

    // 변경분을 DbRecord 목록으로 만들고 추적 상태를 초기화한다.
    // (New/Modified -> Persisted 로 승격, 삭제 대기열 비움)
    //
    // out 은 비우고 채우지만 이미 확보된 용량은 그대로 쓴다. 저장 버퍼를
    // 돌려쓰는 쪽(PlayerSaveBufferPool)이 매 저장마다 벡터를 새로 할당하지
    // 않도록 하기 위한 오버로드다.
    void Flush(std::vector<DbRecord<TVO>>& out)
    {
        out.clear();
        out.reserve(rows_.size() + deleted_.size());

        for (auto& [key, entry] : rows_)
        {
            if (entry.state == RowState::New)
                out.push_back({ entry.vo, DbAction::Insert });
            else if (entry.state == RowState::Modified)
                out.push_back({ entry.vo, DbAction::Update });
            entry.state = RowState::Persisted;
        }

        for (auto& vo : deleted_)
            out.push_back({ std::move(vo), DbAction::Remove });
        deleted_.clear();
    }

    // 새 벡터를 돌려주는 편의 오버로드(테스트/일회성 경로).
    std::vector<DbRecord<TVO>> Flush()
    {
        std::vector<DbRecord<TVO>> records;
        Flush(records);
        return records;
    }

private:
    enum class RowState : uint8_t { Persisted, New, Modified };
    struct Entry
    {
        TVO      vo;
        RowState state;
    };

    std::unordered_map<TKey, Entry> rows_;
    std::vector<TVO>                deleted_;
};


// 플레이어당 1개인 단일 행(예: quest_state)의 DB 변경을 추적.
// INSERT/UPDATE 구분 + 변경 여부만 관리한다.
template<typename TVO>
class DbRowTracker
{
public:
    // Load 경로: DB 에 행이 이미 존재했는지 설정.
    // true  -> 저장 시 UPDATE, false -> 저장 시 INSERT
    void SetPersisted(bool persisted)
    {
        persisted_ = persisted;
        dirty_ = false;
    }

    // 값이 변경되었음을 표시
    void MarkDirty() { dirty_ = true; }

    bool IsPersisted() const { return persisted_; }

    // 저장할 변경분이 있는지 (신규 행이거나 값이 변경됨)
    bool HasPendingChanges() const { return dirty_ || !persisted_; }

    // 변경분이 있으면 out 에 레코드를 만들고 상태를 초기화(persisted=true,
    // dirty=false). 변경분이 없으면 false 를 돌려주고 out 은 건드리지 않는다.
    //
    // VO 는 fill(out.vo) 로 "이미 있는 자리"에 채운다. 임시 VO 를 만들어 move 로
    // 넣으면 VO 안의 문자열/벡터가 매 저장마다 새 버퍼로 바뀌어, 저장 버퍼를
    // 돌려쓰는(PlayerSaveBufferPool) 의미가 사라진다.
    template<typename TFill>
    bool Flush(DbRecord<TVO>& out, TFill&& fill)
    {
        if (!HasPendingChanges())
            return false;

        fill(out.vo);
        out.action = persisted_ ? DbAction::Update : DbAction::Insert;
        persisted_ = true;
        dirty_ = false;
        return true;
    }

private:
    bool persisted_ = false;
    bool dirty_     = false;
};
