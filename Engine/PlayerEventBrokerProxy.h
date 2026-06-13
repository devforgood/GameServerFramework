#pragma once
#include "Component.h"
#include "PlayerEventBroker.h"

// Player가 소유한 PlayerEventBroker를 Character 등 다른 GameObject에서
// 동일하게 사용할 수 있도록 전달하는 프록시 컴포넌트.
// Possess 시점에 Player가 자신의 PlayerEventBroker를 가리키도록 부착하며,
// publish/subscribe 호출을 원본 브로커로 그대로 포워딩한다.
class PlayerEventBrokerProxy : public ComponentBase<PlayerEventBrokerProxy>
{
private:
    PlayerEventBroker* broker_ = nullptr;

public:
    void SetBroker(PlayerEventBroker* broker) { broker_ = broker; }
    PlayerEventBroker* GetBroker() const { return broker_; }
    bool IsValid() const { return broker_ != nullptr; }

    void publish(const EventMessage& message)
    {
        if (broker_)
        {
            broker_->publish(message);
        }
    }

    template<typename TObject, auto Method>
    void subscribe(TObject* object)
    {
        if (broker_)
        {
            broker_->template subscribe<TObject, Method>(object);
        }
    }

    template<typename TObject, typename TEvent, auto Method>
    void subscribe(TObject* object)
    {
        if (broker_)
        {
            broker_->template subscribe<TObject, TEvent, Method>(object);
        }
    }
};
