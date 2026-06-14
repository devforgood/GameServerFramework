#pragma once
#include "Component.h"
#include "EventMessage.h"
#include <string>

// 플레이어의 레벨과 누적 경험치를 관리하는 컴포넌트.
// 몬스터 처치 이벤트(EventActorDead)를 구독하여 경험치를 획득하고,
// Level 테이블(누적 필요 경험치 -> 레벨 매핑)을 기준으로 레벨 업을 시도한다.
class PlayerLevel : public ComponentBase<PlayerLevel>
{
public:
    virtual void Start() override;
    virtual void Load(std::any data) override;
    virtual void Save(std::any data) override;

    // 이벤트 핸들러: 몬스터 처치 시 경험치 획득
    void OnEventActorDead(const EventActorDead& message);

    // 경험치를 추가하고, 임계치를 넘으면 레벨 업을 적용한다.
    void GainExp(int amount);

    int GetLevel() const { return level_; }
    int GetExp() const { return exp_; }

private:
    // 주어진 누적 경험치에 해당하는 레벨(required_exp <= exp 중 최고 레벨)을 Level 테이블에서 찾는다.
    int ResolveLevel(int exp) const;

    // 몬스터 1마리 처치당 획득 경험치. 추후 몬스터 데이터의 보상 경험치로 대체 가능.
    static constexpr int kExpPerKill = 50;

    int level_ = 1;
    int exp_ = 0;

    // 저장 시 PlayerVO 전체를 재구성하기 위해 보관 (name 컬럼을 빈 값으로 덮어쓰지 않도록)
    int characterId_ = 0;
    std::string name_;
};
