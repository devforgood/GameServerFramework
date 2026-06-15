#pragma once
#include <unordered_map>
#include <vector>
#include <optional>
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
    std::vector<DbRecord<TVO>> Flush()
    {
        std::vector<DbRecord<TVO>> records;
        records.reserve(rows_.size() + deleted_.size());

        for (auto& [key, entry] : rows_)
        {
            if (entry.state == RowState::New)
                records.push_back({ entry.vo, DbAction::Insert });
            else if (entry.state == RowState::Modified)
                records.push_back({ entry.vo, DbAction::Update });
            entry.state = RowState::Persisted;
        }

        for (auto& vo : deleted_)
            records.push_back({ std::move(vo), DbAction::Remove });
        deleted_.clear();

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

    // 변경분이 있으면 DbRecord 를 만들고 상태를 초기화(persisted=true, dirty=false).
    // 변경분이 없으면 std::nullopt.
    std::optional<DbRecord<TVO>> Flush(TVO vo)
    {
        if (!HasPendingChanges())
            return std::nullopt;

        DbAction action = persisted_ ? DbAction::Update : DbAction::Insert;
        persisted_ = true;
        dirty_ = false;
        return DbRecord<TVO>{ std::move(vo), action };
    }

private:
    bool persisted_ = false;
    bool dirty_     = false;
};
