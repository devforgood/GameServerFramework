#pragma once
#include <chrono>

class TimeStamp
{
public:
	void update() {
		auto now = std::chrono::system_clock::now();
		timestamp_ = std::chrono::duration_cast<std::chrono::milliseconds>(now.time_since_epoch()).count();
	}

	uint64_t get() const {
		return timestamp_;
	}

	// 서버와 클라이언트 간 시간 차이를 계산하기 위한 메소드
	float getServerClientTimeOffset(uint64_t clientTimestamp) const {
		int64_t diff = (int64_t)timestamp_ - (int64_t)clientTimestamp;
		// 클라이언트 시간이 서버 시간보다 클 수 없으므로 보정이 필요
		if (diff < 0) {
			diff = 0;
		}
		return diff/1000.0f;
	}

private:
	uint64_t timestamp_;
};

