#pragma once
#include "Component.h"
#include "DbChangeTracker.h"
#include "EventMessage.h"
#include "./SQL/generated/vo.h"
#include <vector>
#include <cstdint>

class PlayerQuest : public ComponentBase<PlayerQuest>
{
public:
    virtual void Start() override;
    virtual void Load(std::any data) override;
    virtual void Save(std::any data) override;

    // 이벤트 핸들러
    void OnEventActorKilled(const EventActorKilled& message);
    void OnEventPlayerJoined(const EventPlayerJoined& message);

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
    QuestStateVO buildStateVO() const;

    // 진행 중 퀘스트의 DB 변경 추적 (quest_id 기준)
    DbCollectionTracker<int, QuestActiveVO> active_quests_;

    // quest_state 행(완료 플래그)의 DB 변경 추적
    DbRowTracker<QuestStateVO> quest_state_;

    // 완료 퀘스트 플래그: quest_id 당 1비트, 바이트 단위로 패킹
    std::vector<uint8_t> completed_bits_;

    int character_id_ = 0;
};
