#include "BotPacket.h"

#include <cstring>

namespace bot::packet
{
	namespace
	{
		Frame FinishFrame(flatbuffers::FlatBufferBuilder& builder,
			syncnet::GameMessages type, flatbuffers::Offset<void> payload, int message_id)
		{
			auto message = syncnet::CreateGameMessage(builder, type, payload, message_id);
			builder.Finish(message);
			return EncodeFrame(builder);
		}
	}

	Frame EncodeFrame(const flatbuffers::FlatBufferBuilder& builder)
	{
		const uint16_t body_length = static_cast<uint16_t>(builder.GetSize());

		Frame frame(kHeaderLength + body_length);
		std::memcpy(frame.data(), &body_length, kHeaderLength);
		std::memcpy(frame.data() + kHeaderLength, builder.GetBufferPointer(), body_length);
		return frame;
	}

	bool ExtractFrames(const char* data, size_t size,
		const std::function<void(const char*, size_t)>& on_message,
		size_t& consumed)
	{
		consumed = 0;

		while (size - consumed >= kHeaderLength)
		{
			uint16_t body_length = 0;
			std::memcpy(&body_length, data + consumed, kHeaderLength);

			if (body_length == 0)
				return false;

			if (size - consumed < kHeaderLength + body_length)
				break;

			on_message(data + consumed + kHeaderLength, body_length);
			consumed += kHeaderLength + body_length;
		}

		return true;
	}

	const syncnet::GameMessage* ParseMessage(const char* body, size_t size)
	{
		flatbuffers::Verifier verifier(reinterpret_cast<const uint8_t*>(body), size);
		if (!syncnet::VerifyGameMessageBuffer(verifier))
			return nullptr;

		return syncnet::GetGameMessage(body);
	}

	Frame Login(int message_id, const std::string& user_id,
		const std::string& auth_token, const std::string& reconnect_token)
	{
		flatbuffers::FlatBufferBuilder builder(512);

		// 문자열은 테이블을 열기 전에 만들어야 한다(flatbuffers 제약).
		// 빈 값은 필드를 아예 넣지 않는다 — 서버는 없는 필드를 빈 문자열로 읽는다.
		auto user_id_offset = builder.CreateString(user_id);
		auto token_offset = auth_token.empty()
			? flatbuffers::Offset<flatbuffers::String>() : builder.CreateString(auth_token);
		auto uuid_offset = reconnect_token.empty()
			? flatbuffers::Offset<flatbuffers::String>() : builder.CreateString(reconnect_token);

		auto login = syncnet::CreateLogin(builder, user_id_offset, 0, nullptr, 0,
			uuid_offset, token_offset);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_Login,
			login.Union(), message_id);
	}

	Frame AddAgent(int message_id, const syncnet::Vec3& pos)
	{
		flatbuffers::FlatBufferBuilder builder(256);
		auto add_agent = syncnet::CreateAddAgent(builder,
			syncnet::GameObjectType::GameObjectType_Character, &pos, 0);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_AddAgent,
			add_agent.Union(), message_id);
	}

	Frame SetMoveTarget(int actor_id, const syncnet::Vec3& pos)
	{
		flatbuffers::FlatBufferBuilder builder(256);
		auto move = syncnet::CreateSetMoveTarget(builder, actor_id, &pos);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_SetMoveTarget,
			move.Union(), 0);
	}

	Frame UseSkill(int actor_id, int skill_id, int target_id,
		const syncnet::Vec3& target_pos, int64_t timestamp_ms)
	{
		flatbuffers::FlatBufferBuilder builder(256);
		auto skill = syncnet::CreateUseSkill(builder, actor_id, skill_id, target_id,
			&target_pos, nullptr, timestamp_ms, 1);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_UseSkill,
			skill.Union(), 0);
	}

	Frame Ping(int message_id, int seq)
	{
		flatbuffers::FlatBufferBuilder builder(128);
		auto ping = syncnet::CreatePing(builder, seq);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_Ping,
			ping.Union(), message_id);
	}

	Frame EnterGate(int message_id, int gate_id)
	{
		flatbuffers::FlatBufferBuilder builder(128);
		auto gate = syncnet::CreateEnterGate(builder, 0, gate_id, nullptr, 0);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_EnterGate,
			gate.Union(), message_id);
	}

	Frame Interact(int message_id, int target_id)
	{
		flatbuffers::FlatBufferBuilder builder(128);
		auto interact = syncnet::CreateInteract(builder, target_id);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_Interact,
			interact.Union(), message_id);
	}

	Frame DialogSelect(int message_id, int node_id, int choice_index)
	{
		flatbuffers::FlatBufferBuilder builder(128);
		auto select = syncnet::CreateDialogSelect(builder, node_id, choice_index);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_DialogSelect,
			select.Union(), message_id);
	}

	Frame QuestComplete(int message_id, int quest_id, int reward_choice)
	{
		flatbuffers::FlatBufferBuilder builder(128);
		auto complete = syncnet::CreateQuestComplete(builder, quest_id, reward_choice);

		return FinishFrame(builder, syncnet::GameMessages::GameMessages_QuestComplete,
			complete.Union(), message_id);
	}
}
