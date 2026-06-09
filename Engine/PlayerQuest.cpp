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
    quests_to_delete_.clear();
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

    // Build DbRecord list for active quests (insert/update) + completed quests (delete)
    std::vector<DbRecord<QuestActiveVO>> records;
    records.reserve(active_quests_.size() + quests_to_delete_.size());

    for (const auto& [quest_id, vo] : active_quests_)
    {
        DbAction action = new_active_quest_ids_.count(quest_id)
            ? DbAction::Insert
            : DbAction::Update;
        records.push_back({ vo, action });
    }

    for (const auto& vo : quests_to_delete_)
    {
        records.push_back({ vo, DbAction::Remove });
    }

    save_data->quest_actives = std::move(records);

    // Promote newly inserted quests to existing on next save
    new_active_quest_ids_.clear();
    quests_to_delete_.clear();

    // Build DbRecord for completed quest state
    QuestStateVO state_vo;
    state_vo.character_id = character_id_;
    state_vo.flags.assign(completed_bits_.begin(), completed_bits_.end());

    DbAction state_action = quest_state_is_new_ ? DbAction::Insert : DbAction::Update;
    save_data->quest_state = DbRecord<QuestStateVO>{ state_vo, state_action };

    quest_state_is_new_ = false;
}

bool PlayerQuest::AcceptQuest(int quest_id)
{
    if (active_quests_.count(quest_id))
        return false;

    QuestActiveVO vo{};
    vo.character_id = character_id_;
    vo.quest_id = quest_id;
    vo.state = 0;
    vo.progress1 = 0;
    vo.progress2 = 0;
    vo.progress3 = 0;
    vo.accept_time = std::chrono::system_clock::now();

    active_quests_[quest_id] = vo;
    new_active_quest_ids_.insert(quest_id);
    markDirty();
    return true;
}

bool PlayerQuest::CompleteQuest(int quest_id)
{
    auto it = active_quests_.find(quest_id);
    if (it == active_quests_.end())
        return false;

    // Only need to DELETE from DB if the row was already persisted
    bool was_in_db = (new_active_quest_ids_.count(quest_id) == 0);
    if (was_in_db)
    {
        quests_to_delete_.push_back(it->second);
    }

    active_quests_.erase(it);
    new_active_quest_ids_.erase(quest_id);
    setCompleted(quest_id);  // also calls markDirty()
    return true;
}

void PlayerQuest::UpdateProgress(int quest_id, int progress1, int progress2, int progress3)
{
    auto it = active_quests_.find(quest_id);
    if (it == active_quests_.end())
        return;

    it->second.progress1 = progress1;
    it->second.progress2 = progress2;
    it->second.progress3 = progress3;
    markDirty();
}

bool PlayerQuest::IsActive(int quest_id) const
{
    return active_quests_.count(quest_id) > 0;
}

bool PlayerQuest::IsCompleted(int quest_id) const
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completed_bits_.size())
        return false;
    return (completed_bits_[byte_idx] >> (quest_id % 8)) & 1;
}

const QuestActiveVO* PlayerQuest::GetActiveQuest(int quest_id) const
{
    auto it = active_quests_.find(quest_id);
    if (it == active_quests_.end())
        return nullptr;
    return &it->second;
}

void PlayerQuest::setCompleted(int quest_id)
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completed_bits_.size())
        completed_bits_.resize(byte_idx + 1, 0);
    completed_bits_[byte_idx] |= static_cast<uint8_t>(1 << (quest_id % 8));
    markDirty();
}
