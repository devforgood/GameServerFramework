#pragma once
#include <memory>
#include "Component.h"
#include "GameObject.h"
#include "PlayerEventBroker.h"

// Player가 소유한 PlayerEventBroker를 Character 등 다른 GameObject에서
// 동일하게 사용할 수 있도록 전달하는 프록시 컴포넌트.
// Possess 시점에 Player가 자신의 PlayerEventBroker를 가리키도록 부착하며,
// publish/subscribe 호출을 원본 브로커로 그대로 포워딩한다.
class PlayerEventBrokerProxy : public ComponentBase<PlayerEventBrokerProxy>
{
private:
    // 브로커는 Player(GameObject)가 컴포넌트로 소유한다. 프록시는 그 소유자를 약하게만
    // 참조하여, Player가 살아있을 때만 브로커를 조회한다. Player가 먼저 파괴되면
    // weak_ptr이 만료되어 모든 호출이 안전하게 무시된다.
    std::weak_ptr<GameObject> brokerOwner_;

public:
    void SetBrokerOwner(std::weak_ptr<GameObject> owner) { brokerOwner_ = std::move(owner); }

    PlayerEventBroker* GetBroker() const
    {
        if (auto owner = brokerOwner_.lock())
        {
            return owner->GetComponent<PlayerEventBroker>();
        }
        return nullptr;
    }

    bool IsValid() const { return GetBroker() != nullptr; }

    void publish(const EventMessage& message)
    {
        if (auto* broker = GetBroker())
        {
            broker->publish(message);
        }
    }

    template<typename TObject, auto Method>
    void subscribe(TObject* object)
    {
        if (auto* broker = GetBroker())
        {
            broker->template subscribe<TObject, Method>(object);
        }
    }

    template<typename TObject, typename TEvent, auto Method>
    void subscribe(TObject* object)
    {
        if (auto* broker = GetBroker())
        {
            broker->template subscribe<TObject, TEvent, Method>(object);
        }
    }
};
