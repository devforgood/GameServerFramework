#pragma once

class GameMessage
{
public:
	enum { header_length = 2 };
	enum { max_body_length = 512 };

	GameMessage()
		: bodyLength_(0)
	{
	}

	const char* data() const
	{
		return data_;
	}

	char* data()
	{
		return data_;
	}

	uint16_t length() const
	{
		return header_length + bodyLength_;
	}

	const char* body() const
	{
		return data_ + header_length;
	}

	char* body()
	{
		return data_ + header_length;
	}

	uint16_t body_length() const
	{
		return bodyLength_;
	}

	void body_length(uint16_t new_length)
	{
		bodyLength_ = new_length;
		if (bodyLength_ > max_body_length)
			bodyLength_ = max_body_length;
	}

	bool decode_header()
	{
		//char header[header_length + 1] = "";
		//strncat_s(header, data_, header_length);
		//bodyLength_ = std::atoi(header);

		bodyLength_ = *(reinterpret_cast<uint16_t*>(data_));
		if (bodyLength_ > max_body_length)
		{
			bodyLength_ = 0;
			return false;
		}
		return true;
	}

	void encode_header()
	{
		//char header[header_length + 1] = "";
		//printf_s(header, "%4d", static_cast<int>(bodyLength_));
		//std::memcpy(data_, header, header_length);

		*(reinterpret_cast<uint16_t*>(data_)) = static_cast<uint16_t>(bodyLength_);

	}

private:
	char data_[header_length + max_body_length];
	uint16_t bodyLength_;
};