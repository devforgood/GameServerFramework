#include "PlayerQuest.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"

void PlayerQuest::Load(std::any data)
{
    const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

    character_id_ = load_data.player.id;

    // Load active (in-progress) quests
    active_quests_.clear();
    new_active_quest_ids_.clear();
    for (const auto& vo : load_data.quest_actives)
    {
        active_quests_[vo.quest_id] = vo;
    }

    // Load completed quest flags from raw byte string stored in DB
    const std::string& flags = load_data.quest_state.flags;
    completed_bits_.assign(flags.begin(), flags.end());

    // If character_id is 0 the row did not exist yet → needs INSERT on first save
    quest_state_is_new_ = (load_data.quest_state.character_id == 0);
}

void PlayerQuest::Save(std::any data)
{
    auto* save_data = std::any_cast<PlayerSaveData*>(data);

    // Build DbRecord list for active quests
    std::vector<DbRecord<QuestActiveVO>> records;
    records.reserve(active_quests_.size());
    for (const auto& [quest_id, vo] : active_quests_)
    {
        DbAction action = new_active_quest_ids_.count(quest_id)
            ? DbAction::Insert
            : DbAction::Update;
        records.push_back({ vo, action });
    }
    save_data->quest_actives = std::move(records);

    // Promote newly inserted quests to existing on next save
    new_active_quest_ids_.clear();

    // Build DbRecord for completed quest state
    QuestStateVO state_vo;
    state_vo.character_id = character_id_;
    state_vo.flags.assign(completed_bits_.begin(), completed_bits_.end());

    DbAction state_action = quest_state_is_new_ ? DbAction::Insert : DbAction::Update;
    save_data->quest_state = DbRecord<QuestStateVO>{ state_vo, state_action };

    quest_state_is_new_ = false;
}

bool PlayerQuest::IsCompleted(int quest_id) const
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completed_bits_.size())
        return false;
    return (completed_bits_[byte_idx] >> (quest_id % 8)) & 1;
}

void PlayerQuest::setCompleted(int quest_id)
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completed_bits_.size())
        completed_bits_.resize(byte_idx + 1, 0);
    completed_bits_[byte_idx] |= static_cast<uint8_t>(1 << (quest_id % 8));
    markDirty();
}
