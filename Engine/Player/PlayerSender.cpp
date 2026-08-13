#include "PlayerSender.h"
#include "Player.h"

void PlayerSender::Send(std::shared_ptr<send_message>& msg) const
{
	if (sink_)
	{
		sink_(msg);
		return;
	}

	if (owner_ != nullptr)
		owner_->Send(msg);
}
