#pragma once
#include "Component.h"
#include <functional>
#include <memory>

class Player;
class send_message;

// 컴포넌트가 클라이언트로 메시지를 내보내는 통로.
//
// 각 컴포넌트는 "무엇이 바뀌었는가"를 자기가 가장 잘 안다. 그것을 밖으로 꺼내 남이
// 직렬화하게 하면, 내부 상태를 오직 그 목적으로만 public 으로 열어 두게 된다
// (PlayerQuest 의 DrainSync 가 그랬다). 전송 통로만 주면 각자 자기 것을 보낼 수 있다.
//
// 이 컴포넌트는 Player 자신에게 붙으므로 소유자를 약하게 참조할 필요가 없다 —
// Player 가 소유한 컴포넌트가 Player 보다 오래 살 수는 없다. (Character 처럼 다른
// GameObject 에 붙는 PlayerEventBrokerProxy 는 그렇지 않아서 weak_ptr 을 쓴다.)
class PlayerSender : public ComponentBase<PlayerSender>
{
public:
	using SendFn = std::function<void(std::shared_ptr<send_message>&)>;

	// Player 가 자기 생성자에서 자신을 넘긴다.
	void BindOwner(Player* owner) { owner_ = owner; }

	// 소유 Player. 다른 플레이어의 캐릭터(actor id, 맵)처럼 컴포넌트 층에서는 볼 수 없는
	// 것을 찾아야 할 때만 쓴다. 없을 수 있다(테스트).
	Player* GetOwner() const { return owner_; }

	// 세션 대신 메시지를 가로챈다. 실제 세션 없이 와이어 포맷을 검증하기 위한 것으로,
	// 설정하면 소유자에게는 보내지 않는다.
	void SetSink(SendFn sink) { sink_ = std::move(sink); }

	// 보낼 곳이 없으면 조용히 버린다. 아직 세션에 붙지 않은 플레이어나 테스트용
	// GameObject 에서도 컴포넌트가 평소대로 돌 수 있어야 한다.
	void Send(std::shared_ptr<send_message>& msg) const;

private:
	Player* owner_ = nullptr;
	SendFn sink_;
};
