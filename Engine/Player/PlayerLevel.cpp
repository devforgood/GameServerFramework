#include "PlayerLevel.h"
#include "PlayerLoadData.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "GameObject.h"
#include "GameData/ResourceLoader.h"
#include "PartyPolicy.h"
#include "Actor.h"
#include "Character.h"
#include "LogHelper.h"
#include "Player.h"

void PlayerLevel::Start()
{
    auto eventBroker = game_object->GetComponent<PlayerEventBroker>();
    eventBroker->subscribe<PlayerLevel, EventActorDead, &PlayerLevel::OnEventActorDead>(this);
}

void PlayerLevel::OnEventActorDead(const EventActorDead& message)
{
    // 몬스터 처치 보상으로 경험치를 획득하고 레벨 업을 시도한다.
    // 보상량은 처치 대상의 데이터(monster.json 의 exp)가 정한다. 데이터가 없는 대상
    // (플레이어 등)은 0 이라 아무 일도 일어나지 않는다.
    // 파티로 잡았다면 크레딧을 나눠 가진 인원수만큼 분배된다(혼자면 그대로).
    GainExp(PartyPolicy::Instance().ShareExp(message.reward_exp, message.credit_share));
}

void PlayerLevel::GainExp(int amount)
{
    if (amount <= 0)
        return;

    exp_ += amount;
    markDirty(); // 레벨이 그대로여도 경험치는 저장되어야 한다

    int newLevel = ResolveLevel(exp_);
    if (newLevel <= level_)
        return;

    level_ = newLevel;

    // 새 레벨의 스탯을 즉시 반영한다. 현재 체력은 유지한다 — 레벨업으로 체력이 가득 차면
    // 전투 중 레벨업이 곧 완전 회복이 되어 버린다.
    if (auto* player = dynamic_cast<Player*>(game_object))
    {
        if (auto character = player->GetCharacter())
            ApplyStatsTo(character.get(), /*resetHealth=*/false);
    }

    // 레벨 상승 이벤트 발행 (레벨 달성 퀘스트, 컨텐츠 해금 등에서 구독)
    if (auto* broker = game_object->GetComponent<PlayerEventBroker>())
        broker->publish(EventLevelUp{ static_cast<int>(characterId_), level_ });
}

void PlayerLevel::ApplyStatsTo(Actor* actor, bool resetHealth) const
{
    if (actor == nullptr)
        return;

    const gamedata::Level* data = ResourceLoader::Instance().GetLevel(level_);
    if (data == nullptr)
    {
        LOG.warn("PlayerLevel: level.json 에 레벨 {} 행이 없다. 기본 스탯을 유지한다.", level_);
        return;
    }

    actor->SetCombatStats(data->hp, data->attack, data->defense, resetHealth);
}

int PlayerLevel::ResolveLevel(long long exp) const
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

    characterId_ = load_data.player.id;
    name_ = load_data.player.name;
    level_ = load_data.player.level > 0 ? load_data.player.level : 1;
    exp_ = load_data.player.exp;

    // 레벨 중간 진행도까지 복원한다. exp 가 0 인데 레벨이 1보다 크면 exp 컬럼이 생기기 전에
    // 저장된 행이므로, 그 레벨의 시작 경험치로 맞춰 준다(레벨이 강등되지 않게).
    if (exp_ == 0 && level_ > 1)
    {
        const gamedata::Level* lv = ResourceLoader::Instance().GetLevel(level_);
        exp_ = lv ? lv->required_exp : 0;
    }
}

void PlayerLevel::Save(std::any data)
{
    auto* save_data = std::any_cast<PlayerSaveData*>(data);

    PlayerVO vo;
    vo.id = characterId_;
    vo.name = name_;
    vo.level = level_;
    vo.exp = exp_;
    save_data->player = std::move(vo);
}
