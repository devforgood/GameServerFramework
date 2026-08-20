#include "BotSession.h"

#include "BotLog.h"

namespace bot
{
	namespace
	{
		constexpr size_t kReadChunkSize = 16 * 1024;

		// 수신 버퍼 상한. 정상 서버는 이만큼 밀어 넣지 않는다 — 넘으면 프레임 동기가
		// 깨졌거나 봇이 처리를 못 따라가는 것이므로 연결을 끊어 지표에 드러낸다.
		constexpr size_t kMaxReadBuffer = 1 * 1024 * 1024;
	}

	BotSession::BotSession(boost::asio::io_context& io_context, std::string host, uint16_t port,
		BotStats* stats)
		: io_context_(io_context)
		, resolver_(io_context)
		, socket_(io_context)
		, host_(std::move(host))
		, port_(port)
		, stats_(stats)
	{
		read_chunk_.resize(kReadChunkSize);
		read_buffer_.reserve(kReadChunkSize);
	}

	BotSession::~BotSession()
	{
		boost::system::error_code ignored;
		socket_.close(ignored);
	}

	void BotSession::Connect(ConnectHandler handler)
	{
		// 이전 연결의 잔재를 정리한다. 세대를 올려 두면 늦게 도착한 핸들러가
		// 새 연결 상태를 건드리지 않는다.
		boost::system::error_code ignored;
		socket_.close(ignored);
		socket_ = tcp::socket(io_context_);

		++generation_;
		const uint64_t generation = generation_;

		read_buffer_.clear();
		write_queue_.clear();
		write_in_progress_ = false;
		connected_ = false;

		if (stats_ != nullptr)
			++stats_->connect_attempts;

		boost::system::error_code address_error;
		const auto address = boost::asio::ip::make_address(host_, address_error);
		if (address_error)
		{
			// 호스트가 IP 가 아니면 이름 해석을 거친다.
			resolver_.async_resolve(host_, std::to_string(port_),
				[this, generation, handler](const boost::system::error_code& error,
					tcp::resolver::results_type results)
				{
					if (generation != generation_)
						return;

					if (error)
					{
						if (stats_ != nullptr)
							++stats_->connect_failures;
						handler(false, error.message());
						return;
					}

					boost::asio::async_connect(socket_, results,
						[this, generation, handler](const boost::system::error_code& connect_error,
							const tcp::endpoint&)
						{
							if (generation != generation_)
								return;

							if (connect_error)
							{
								if (stats_ != nullptr)
									++stats_->connect_failures;
								handler(false, connect_error.message());
								return;
							}

							boost::system::error_code option_error;
							socket_.set_option(tcp::no_delay(true), option_error);
							connected_ = true;
							StartRead(generation);
							handler(true, std::string());
						});
				});
			return;
		}

		socket_.async_connect(tcp::endpoint(address, port_),
			[this, generation, handler](const boost::system::error_code& error)
			{
				if (generation != generation_)
					return;

				if (error)
				{
					if (stats_ != nullptr)
						++stats_->connect_failures;
					handler(false, error.message());
					return;
				}

				// 봇 패킷은 작고 잦다. Nagle 이 붙으면 측정하려는 왕복 지연에
				// 최대 40ms 가 얹혀 결과가 서버 성능이 아니라 커널 설정을 반영한다.
				boost::system::error_code option_error;
				socket_.set_option(tcp::no_delay(true), option_error);
				connected_ = true;
				StartRead(generation);
				handler(true, std::string());
			});
	}

	void BotSession::Send(packet::Frame frame)
	{
		if (!connected_)
			return;

		if (stats_ != nullptr)
		{
			++stats_->packets_sent;
			stats_->bytes_sent += frame.size();
		}

		write_queue_.push_back(std::move(frame));
		if (!write_in_progress_)
			DoWrite(generation_);
	}

	void BotSession::DoWrite(uint64_t generation)
	{
		if (write_queue_.empty() || !connected_)
		{
			write_in_progress_ = false;
			return;
		}

		write_in_progress_ = true;
		boost::asio::async_write(socket_,
			boost::asio::buffer(write_queue_.front().data(), write_queue_.front().size()),
			[this, generation](const boost::system::error_code& error, size_t bytes)
			{
				OnWrite(generation, error, bytes);
			});
	}

	void BotSession::OnWrite(uint64_t generation, const boost::system::error_code& error, size_t)
	{
		if (generation != generation_)
			return;

		if (error)
		{
			write_in_progress_ = false;
			HandleFailure(generation, "write: " + error.message());
			return;
		}

		write_queue_.pop_front();
		DoWrite(generation);
	}

	void BotSession::StartRead(uint64_t generation)
	{
		socket_.async_read_some(boost::asio::buffer(read_chunk_.data(), read_chunk_.size()),
			[this, generation](const boost::system::error_code& error, size_t bytes)
			{
				OnRead(generation, error, bytes);
			});
	}

	void BotSession::OnRead(uint64_t generation, const boost::system::error_code& error, size_t bytes)
	{
		if (generation != generation_)
			return;

		if (error)
		{
			HandleFailure(generation, "read: " + error.message());
			return;
		}

		if (stats_ != nullptr)
			stats_->bytes_recv += bytes;

		read_buffer_.insert(read_buffer_.end(), read_chunk_.begin(), read_chunk_.begin() + bytes);

		if (read_buffer_.size() > kMaxReadBuffer)
		{
			HandleFailure(generation, "read buffer overflow");
			return;
		}

		ProcessReadBuffer();

		// 처리 중 연결이 닫혔을 수 있다(핸들러가 Close 를 부를 수 있다).
		if (generation == generation_ && connected_)
			StartRead(generation);
	}

	void BotSession::ProcessReadBuffer()
	{
		size_t consumed = 0;
		const bool ok = packet::ExtractFrames(read_buffer_.data(), read_buffer_.size(),
			[this](const char* body, size_t size)
			{
				if (stats_ != nullptr)
					++stats_->packets_recv;

				const syncnet::GameMessage* message = packet::ParseMessage(body, size);
				if (message == nullptr)
				{
					log::Printf(LogLevel::Warn, "bot: 검증 실패한 메시지 수신 (%zu bytes)", size);
					return;
				}

				if (on_message_)
					on_message_(message);
			}, consumed);

		if (consumed > 0)
			read_buffer_.erase(read_buffer_.begin(), read_buffer_.begin() + consumed);

		if (!ok)
			HandleFailure(generation_, "invalid frame length");
	}

	void BotSession::HandleFailure(uint64_t generation, const std::string& reason)
	{
		if (generation != generation_ || !connected_)
			return;

		connected_ = false;
		++generation_;   // 남아 있는 핸들러를 전부 무효화한다.

		boost::system::error_code ignored;
		socket_.close(ignored);

		if (stats_ != nullptr)
			++stats_->disconnects;

		if (on_close_)
			on_close_(reason);
	}

	void BotSession::Close(const std::string& reason)
	{
		if (!connected_)
			return;

		connected_ = false;
		++generation_;

		boost::system::error_code ignored;
		socket_.shutdown(tcp::socket::shutdown_both, ignored);
		socket_.close(ignored);

		write_queue_.clear();
		write_in_progress_ = false;

		if (on_close_)
			on_close_(reason);
	}
}
