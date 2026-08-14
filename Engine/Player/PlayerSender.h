#pragma once
#include "Component.h"
#include <functional>
#include <memory>

class send_message;

// 컴포넌트가 클라이언트로 메시지를 내보내는 통로.
//
// 각 컴포넌트는 "무엇이 바뀌었는가"를 자기가 가장 잘 안다. 그것을 밖으로 꺼내 남이
// 직렬화하게 하면, 내부 상태를 오직 그 목적으로만 public 으로 열어 두게 된다
// (PlayerQuest 의 DrainSync 가 그랬다). 전송 통로만 주면 각자 자기 것을 보낼 수 있다.
//
// 여기서 아는 것은 "메시지를 보낸다"뿐이다. Player 를 들고 있으면 컴포넌트들이 이것을
// 통해 Player 의 다른 기능까지 꺼내 쓰게 되고, 그러면 통로가 아니라 우회로가 된다.
class PlayerSender : public ComponentBase<PlayerSender>
{
public:
	using SendFn = std::function<void(std::shared_ptr<send_message>&)>;

	// 보낼 곳을 연결한다. 서버에서는 Player 가 자기 세션으로 넘기는 함수를 넘기고,
	// 테스트에서는 메시지를 그대로 받아 두는 함수를 넘긴다.
	void Bind(SendFn send) { send_ = std::move(send); }

	// 연결되지 않았으면 조용히 버린다. 아직 세션에 붙지 않은 플레이어나 전송이 필요 없는
	// 테스트에서도 컴포넌트가 평소대로 돌 수 있어야 한다.
	void Send(std::shared_ptr<send_message>& msg) const
	{
		if (send_)
			send_(msg);
	}

private:
	SendFn send_;
};
