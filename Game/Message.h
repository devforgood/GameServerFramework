#pragma once

class game_message
{
public:
	enum { header_length = 2 };
	enum { max_body_length = 512 };

	game_message()
		: body_length_(0)
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
		return header_length + body_length_;
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
		return body_length_;
	}

	void body_length(uint16_t new_length)
	{
		body_length_ = new_length;
		if (body_length_ > max_body_length)
			body_length_ = max_body_length;
	}

	bool decode_header()
	{
		//char header[header_length + 1] = "";
		//strncat_s(header, data_, header_length);
		//body_length_ = std::atoi(header);

		body_length_ = *(reinterpret_cast<uint16_t*>(data_));
		if (body_length_ > max_body_length)
		{
			body_length_ = 0;
			return false;
		}
		return true;
	}

	void encode_header()
	{
		//char header[header_length + 1] = "";
		//printf_s(header, "%4d", static_cast<int>(body_length_));
		//std::memcpy(data_, header, header_length);

		*(reinterpret_cast<uint16_t*>(data_)) = static_cast<uint16_t>(body_length_);

	}

private:
	char data_[header_length + max_body_length];
	uint16_t body_length_;
};