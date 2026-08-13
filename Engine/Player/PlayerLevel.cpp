#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "GameObject.h"
#include "GameData/ResourceLoader.h"
#include "PartyPolicy.h"

void PlayerLevel::Start()
{
    auto eventBroker = game_object->GetComponent<PlayerEventBroker>();
    eventBroker->subscribe<PlayerLevel, EventActorDead, &PlayerLevel::OnEventActorDead>(this);
}

void PlayerLevel::OnEventActorDead(const EventActorDead& message)
{
    // 몬스터 처치 보상으로 경험치를 획득하고 레벨 업을 시도한다.
    // 파티로 잡았다면 크레딧을 나눠 가진 인원수만큼 분배된다(혼자면 그대로).
    GainExp(PartyPolicy::Instance().ShareExp(kExpPerKill, message.credit_share));
}

void PlayerLevel::GainExp(int amount)
{
    if (amount <= 0)
        return;

    exp_ += amount;

    int newLevel = ResolveLevel(exp_);
    if (newLevel <= level_)
        return;

    level_ = newLevel;
    markDirty();

    // 레벨 상승 이벤트 발행 (레벨 달성 퀘스트, 컨텐츠 해금 등에서 구독)
    if (auto* broker = game_object->GetComponent<PlayerEventBroker>())
        broker->publish(EventLevelUp{ characterId_, level_ });
}

int PlayerLevel::ResolveLevel(int exp) const
{
    // Level 테이블에서 required_exp <= exp 를 만족하는 가장 높은 레벨을 찾는다.
    int resolved = 1; // 레벨 1(required_exp=0)은 항상 만족
    for (const auto& [id, lv] : ResourceLoader::Instance().GetLevels())
    {
        if (lv == nullptr)
            continue;
        if (lv->required_exp <= exp && lv->level > resolved)
            resolved = lv->level;
    }
    return resolved;
}

void PlayerLevel::Load(std::any data)
{
    const auto& load_data = std::any_cast<const PlayerLoadData&>(data);

    characterId_ = static_cast<int>(load_data.player.id);
    name_ = load_data.player.name;
    level_ = load_data.player.level > 0 ? load_data.player.level : 1;

    // exp 컬럼이 아직 DB 스키마에 없으므로, 로드 시 현재 레벨의 누적 필요 경험치로 복원한다.
    const gamedata::Level* lv = ResourceLoader::Instance().GetLevel(level_);
    exp_ = lv ? lv->required_exp : 0;
}

void PlayerLevel::Save(std::any data)
{
    auto* save_data = std::any_cast<PlayerSaveData*>(data);

    PlayerVO vo;
    vo.id = characterId_;
    vo.name = name_;
    vo.level = level_;
    save_data->player = std::move(vo);
}
