#include "PlayerQuest.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "GameObject.h"

void PlayerQuest::Start()
{
    auto eventBroker = game_object->GetComponent<PlayerEventBroker>();
    eventBroker->subscribe<PlayerQuest, EventActorDead, &PlayerQuest::OnEventActorDead>(this);
    eventBroker->subscribe<PlayerQuest, EventPlayerJoined, &PlayerQuest::OnEventPlayerJoined>(this);
}

void PlayerQuest::OnEventActorDead(const EventActorDead& message)
{
    // 예시: 처치 이벤트에 반응하여 퀘스트 진행도 갱신
    int quest_id = 1; // 예시 퀘스트 ID
    if (IsActive(quest_id))
    {
        // 진행도 업데이트 (예시로 progress1 증가)
        const QuestActiveVO* vo = GetActiveQuest(quest_id);
        if (vo)
        {
            UpdateProgress(quest_id, vo->progress1 + 1, vo->progress2, vo->progress3);
        }
    }
}

void PlayerQuest::OnEventPlayerJoined(const EventPlayerJoined& message)
{
    // 예시: 처치 이벤트에 반응하여 퀘스트 진행도 갱신
    int quest_id = 1; // 예시 퀘스트 ID
    if (IsActive(quest_id))
    {
        // 진행도 업데이트 (예시로 progress1 증가)
        const QuestActiveVO* vo = GetActiveQuest(quest_id);
        if (vo)
        {
            UpdateProgress(quest_id, vo->progress1 + 1, vo->progress2, vo->progress3);
        }
    }
}


void PlayerQuest::Load(std::any data)
{
    const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

    characterId_ = load_data.player.id;

    // 진행 중 퀘스트를 기존(Persisted) 상태로 로드
    activeQuests_.Clear();
    for (const auto& vo : load_data.quest_actives)
        activeQuests_.AddPersisted(vo.quest_id, vo);

    // 완료 퀘스트 플래그를 DB 의 raw 바이트 문자열에서 복원
    const std::string& flags = load_data.quest_state.flags;
    completedBits_.assign(flags.begin(), flags.end());

    // character_id 가 0 이면 행이 아직 없음 -> 첫 저장 시 INSERT
    questState_.SetPersisted(load_data.quest_state.character_id != 0);
}

void PlayerQuest::Save(std::any data)
{
    auto* save_data = std::any_cast<PlayerSaveData*>(data);

    // 변경된 진행 퀘스트만 INSERT/UPDATE/DELETE 레코드로
    if (activeQuests_.HasPendingChanges())
        save_data->quest_actives = activeQuests_.Flush();

    // 완료 플래그가 변경되었거나 신규 행이면 quest_state 레코드 생성
    if (auto record = questState_.Flush(buildStateVO()))
        save_data->quest_state = std::move(record);
}

bool PlayerQuest::AcceptQuest(int quest_id)
{
    QuestActiveVO vo{};
    vo.character_id = characterId_;
    vo.quest_id = quest_id;
    vo.state = 0;
    vo.progress1 = 0;
    vo.progress2 = 0;
    vo.progress3 = 0;
    vo.accept_time = std::chrono::system_clock::now();

    if (!activeQuests_.Add(quest_id, vo))
        return false;

    markDirty();
    return true;
}

bool PlayerQuest::CompleteQuest(int quest_id)
{
    if (!activeQuests_.Contains(quest_id))
        return false;

    // 진행 목록에서 제거(기존 행이면 DELETE 대기열로, 신규 행이면 그냥 폐기)
    activeQuests_.Remove(quest_id);
    setCompleted(quest_id);  // 완료 플래그 갱신 + markDirty
    return true;
}

void PlayerQuest::UpdateProgress(int quest_id, int progress1, int progress2, int progress3)
{
    QuestActiveVO* vo = activeQuests_.Modify(quest_id);
    if (vo == nullptr)
        return;

    vo->progress1 = progress1;
    vo->progress2 = progress2;
    vo->progress3 = progress3;
    markDirty();
}

bool PlayerQuest::IsActive(int quest_id) const
{
    return activeQuests_.Contains(quest_id);
}

bool PlayerQuest::IsCompleted(int quest_id) const
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completedBits_.size())
        return false;
    return (completedBits_[byte_idx] >> (quest_id % 8)) & 1;
}

const QuestActiveVO* PlayerQuest::GetActiveQuest(int quest_id) const
{
    return activeQuests_.Find(quest_id);
}

void PlayerQuest::setCompleted(int quest_id)
{
    size_t byte_idx = static_cast<size_t>(quest_id) / 8;
    if (byte_idx >= completedBits_.size())
        completedBits_.resize(byte_idx + 1, 0);
    completedBits_[byte_idx] |= static_cast<uint8_t>(1 << (quest_id % 8));
    questState_.MarkDirty();
    markDirty();
}

QuestStateVO PlayerQuest::buildStateVO() const
{
    QuestStateVO vo;
    vo.character_id = characterId_;
    vo.flags.assign(completedBits_.begin(), completedBits_.end());
    return vo;
}
