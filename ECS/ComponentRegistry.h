#pragma once
#include "Components.h"

namespace engine
{
    // 게임에서 사용하는 모든 컴포넌트 타입을 EntityManager 에 등록한다.
    // Components.h 는 순수 타입 정의만 두고, "무엇을 등록하는가"의 목록은 이 파일이 소유한다.
    // 새 컴포넌트를 추가하면 여기에 함께 등록해, 사용하는 쪽(Map 등)이 목록을 알 필요가 없게 한다.
    inline void RegisterGameComponents(EntityManager& entityManager)
    {
        entityManager.RegisterComponent<PositionComponent>();
        entityManager.RegisterComponent<VelocityComponent>();
        entityManager.RegisterComponent<HealthComponent>();
        entityManager.RegisterComponent<PhysicsComponent>();
        entityManager.RegisterComponent<AIComponent>();
        entityManager.RegisterComponent<CollisionComponent>();
        entityManager.RegisterComponent<InputComponent>();
        entityManager.RegisterComponent<AnimationComponent>();
        entityManager.RegisterComponent<TimerComponent>();
        entityManager.RegisterComponent<ParticleComponent>();
        entityManager.RegisterComponent<NetworkComponent>();
        entityManager.RegisterComponent<StateComponent>();
    }
}
