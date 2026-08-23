#pragma once
#include "syncnet_generated.h"
#include <array>
#include <boost/asio.hpp>
#include "GameMessage.h"

class send_message : public flatbuffers::FlatBufferBuilder
{
private:
	int size;

public:
	send_message() : flatbuffers::FlatBufferBuilder(1024)
	{

	}

	// 헤더 + 본문, 언제나 정확히 두 조각이다. 벡터로 돌려주면 패킷 하나 나갈 때마다
	// 힙 할당이 하나 붙으므로 고정 크기 배열로 돌려준다(async_write 는 버퍼 시퀀스만
	// 요구하므로 그대로 받는다).
	//
	// size 를 지역 변수가 아니라 멤버로 두는 이유: async_write 가 끝날 때까지 이
	// 버퍼가 가리키는 메모리가 살아 있어야 한다. send_message 는 완료될 때까지
	// writeMsgs_ 가 붙들고 있다.
	std::array<boost::asio::const_buffer, 2> to_buffers()
	{
		size = this->GetSize();
		return {
			boost::asio::buffer(&size, GameMessage::header_length),
			boost::asio::buffer(this->GetBufferPointer(), this->GetSize())
		};
	}

	flatbuffers::FlatBufferBuilder & get_builder()
	{
		return *this;
	}
};