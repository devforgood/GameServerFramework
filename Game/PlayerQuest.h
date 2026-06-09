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

    bool IsCompleted(int quest_id) const;

private:
    void setCompleted(int quest_id);

    // In-progress quests, keyed by quest_id
    std::unordered_map<int, QuestActiveVO> active_quests_;

    // quest_id set for quests not yet persisted to DB (need INSERT)
    std::unordered_set<int> new_active_quest_ids_;

    // Completed quest flags: 1 bit per quest_id, packed into bytes
    std::vector<uint8_t> completed_bits_;

    // true = quest_state row not yet in DB (needs INSERT), false = needs UPDATE
    bool quest_state_is_new_ = true;

    int character_id_ = 0;
};
