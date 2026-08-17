#pragma once

#include <chrono>

//---------------------------------------------------------------------------------------
// 레이트리밋용 토큰 버킷.
//
// 초당 rate 개씩 토큰이 차고, 최대 capacity 개까지 쌓인다. 요청 하나가 토큰 하나를 쓴다.
// capacity 가 순간 몰림(버스트) 허용치이고, rate 가 지속 허용치다.
//
// 시간을 인자로 받는 순수 계산이라 단위 테스트에서 가짜 시계로 검증할 수 있다.
// (실제 사용처는 GameSession::ConsumePacketToken)
//---------------------------------------------------------------------------------------
class TokenBucket
{
public:
	using Clock = std::chrono::steady_clock;

	TokenBucket() = default;

	// rate <= 0 이면 제한 없음(Consume 이 항상 true).
	void Configure(double ratePerSecond, double capacity, Clock::time_point now)
	{
		rate_ = ratePerSecond;
		capacity_ = capacity > 0.0 ? capacity : ratePerSecond;
		tokens_ = capacity_;   // 가득 찬 상태로 시작한다(접속 직후 로그인 등 짧은 몰림 허용)
		lastRefill_ = now;
	}

	// 토큰 하나를 쓴다. 남은 토큰이 없으면 false(=한도 초과).
	bool Consume(Clock::time_point now)
	{
		if (rate_ <= 0.0)
			return true;

		const double elapsed = std::chrono::duration<double>(now - lastRefill_).count();
		if (elapsed > 0.0)
		{
			lastRefill_ = now;
			tokens_ += elapsed * rate_;
			if (tokens_ > capacity_)
				tokens_ = capacity_;
		}

		if (tokens_ < 1.0)
			return false;

		tokens_ -= 1.0;
		return true;
	}

	double TokensForTest() const { return tokens_; }

private:
	double rate_ = 0.0;
	double capacity_ = 0.0;
	double tokens_ = 0.0;
	Clock::time_point lastRefill_{};
};
