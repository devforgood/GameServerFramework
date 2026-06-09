#pragma once
#include "Component.h"
#include "./SQL/generated/vo.h"
#include <unordered_map>
#include <unordered_set>
#include <vector>
#include <cstdint>

class PlayerQuest : public ComponentBase<PlayerQuest>
{
public:
    virtual void Load(std::any data) override;
    virtual void Save(std::any data) override;

    // 퀘스트 수락: 이미 진행 중이면 false 반환
    bool AcceptQuest(int quest_id);

    // 퀘스트 완료: 진행 중이 아니면 false 반환
    bool CompleteQuest(int quest_id);

    // 진행 중인 퀘스트 진행도 갱신
    void UpdateProgress(int quest_id, int progress1, int progress2 = 0, int progress3 = 0);

    bool IsActive(int quest_id) const;
    bool IsCompleted(int quest_id) const;

    const QuestActiveVO* GetActiveQuest(int quest_id) const;

private:
    void setCompleted(int quest_id);

    // In-progress quests, keyed by quest_id
    std::unordered_map<int, QuestActiveVO> active_quests_;

    // quest_id set for quests not yet persisted to DB (need INSERT)
    std::unordered_set<int> new_active_quest_ids_;

    // Quests completed this session that need DELETE from quest_active table
    std::vector<QuestActiveVO> quests_to_delete_;

    // Completed quest flags: 1 bit per quest_id, packed into bytes
    std::vector<uint8_t> completed_bits_;

    // true = quest_state row not yet in DB (needs INSERT), false = needs UPDATE
    bool quest_state_is_new_ = true;

    int character_id_ = 0;
};
