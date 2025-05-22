#pragma once
#include "syncnet_generated.h"
#include <boost/asio.hpp>
#include "Message.h"

class send_message : public flatbuffers::FlatBufferBuilder
{
private:
	int size;

public:
	send_message() : flatbuffers::FlatBufferBuilder(1024)
	{

	}

	std::vector<boost::asio::const_buffer> to_buffers()
	{
		std::vector<boost::asio::const_buffer> buffers;
		size = this->GetSize();
		buffers.push_back(boost::asio::buffer(&size, game_message::header_length));
		buffers.push_back(boost::asio::buffer(this->GetBufferPointer(), this->GetSize()));
		return buffers;
	}

	flatbuffers::FlatBufferBuilder & get_builder()
	{
		return *this;
	}
};