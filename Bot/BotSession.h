#pragma once

#include <cstdint>
#include <deque>
#include <functional>
#include <string>
#include <vector>

#include <boost/asio.hpp>

#include "BotMetrics.h"
#include "BotPacket.h"

namespace bot
{
	using boost::asio::ip::tcp;

	//-----------------------------------------------------------------------------------
	// 봇 한 명의 서버 연결.
	//
	// 스레드 모델: 소유한 io_context 는 워커 스레드 하나만 돌린다. 그래서 이 클래스의
	// 모든 콜백이 그 스레드에서만 실행되고, 큐/버퍼/카운터에 락이 필요 없다.
	// (strand 도 필요 없다 — io_context 당 스레드가 하나면 그 자체가 직렬화다.)
	//
	// 수명: BotClient 가 소유하며, 워커 스레드가 join 된 뒤에 파괴된다. 비동기 핸들러가
	// this 를 잡고 있어도 안전한 이유다. 재접속은 소켓만 새로 만들고 객체는 그대로 쓰며,
	// 이전 연결의 늦은 핸들러는 generation 으로 걸러 낸다.
	//-----------------------------------------------------------------------------------
	class BotSession
	{
	public:
		using ConnectHandler = std::function<void(bool success, const std::string& error)>;
		using MessageHandler = std::function<void(const syncnet::GameMessage* message)>;
		using CloseHandler = std::function<void(const std::string& reason)>;

		BotSession(boost::asio::io_context& io_context, std::string host, uint16_t port,
			BotStats* stats);
		~BotSession();

		BotSession(const BotSession&) = delete;
		BotSession& operator=(const BotSession&) = delete;

		void SetMessageHandler(MessageHandler handler) { on_message_ = std::move(handler); }
		void SetCloseHandler(CloseHandler handler) { on_close_ = std::move(handler); }

		void Connect(ConnectHandler handler);

		// 큐에 넣고 순서대로 보낸다. 연결 전이면 버린다(부하 테스트에서 조용히 쌓이면
		// 끊긴 봇이 살아 있는 것처럼 보인다).
		void Send(packet::Frame frame);

		void Close(const std::string& reason);

		bool IsConnected() const { return connected_; }

		// 큐에 남아 있는 전송 대기 프레임 수. 서버가 읽지 못해 밀리는지 보는 지표다.
		size_t PendingWrites() const { return write_queue_.size(); }

	private:
		void StartRead(uint64_t generation);
		void OnRead(uint64_t generation, const boost::system::error_code& error, size_t bytes);
		void DoWrite(uint64_t generation);
		void OnWrite(uint64_t generation, const boost::system::error_code& error, size_t bytes);
		void HandleFailure(uint64_t generation, const std::string& reason);
		void ProcessReadBuffer();

		boost::asio::io_context& io_context_;
		tcp::resolver resolver_;
		tcp::socket socket_;
		std::string host_;
		uint16_t port_;

		BotStats* stats_;

		MessageHandler on_message_;
		CloseHandler on_close_;

		std::vector<char> read_buffer_;      // 아직 처리하지 않은 수신 바이트
		std::vector<char> read_chunk_;       // async_read_some 목적지
		std::deque<packet::Frame> write_queue_;
		bool write_in_progress_ = false;

		bool connected_ = false;

		// 재접속 세대. 이전 연결에서 남은 핸들러가 새 연결의 상태를 건드리지 못하게 한다.
		uint64_t generation_ = 0;
	};
}
