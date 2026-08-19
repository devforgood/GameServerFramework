#pragma once

#include <chrono>

//
// DbMonitorTicker
// ------------------------------------------------------------------
// DB 스레드 풀(DbThreadMonitor) 의 주기적인 감시/리포트를 담당하는 타이머.
// ServerManager::RunWorker 의 프레임 루프에 흩어져 있던 시각 계산을 한곳에 모은 것이다.
//
//  - 1초마다  CheckStuckTasks()   : 멈춘(데드락/슬로우) 작업 감지
//  - 10초마다 ReportThreadUsage() : 스레드별 누적 사용 시간 리포트
//
// 감시 대상이 전역 공유 자원이므로 워커가 여러 개여도 primary 워커 하나만 이 티커를 돈다.
//
// 성능 메모: 프레임 루프(약 60Hz)에서 매번 불리지만, 아직 마감이 오지 않은 대부분의
// 프레임은 time_point 비교 한 번으로 끝난다(헤더에 인라인). 실제 작업이 필요한
// 프레임에서만 비인라인 Fire() 로 빠지므로 핫 패스에 코드가 끼어들지 않는다.
//
class DbMonitorTicker
{
public:
	using Clock = std::chrono::steady_clock;

	// 멈춘(데드락/슬로우) 작업 감시 주기.
	static constexpr std::chrono::milliseconds kCheckInterval{ 1000 };
	// 스레드별 사용 시간 리포트 주기.
	static constexpr std::chrono::milliseconds kReportInterval{ 10000 };

	explicit DbMonitorTicker(Clock::time_point now)
		: nextCheck_(now + kCheckInterval)
		, nextReport_(now + kReportInterval)
		, nextDue_(now + kCheckInterval)
	{
	}

	// 프레임마다 호출한다. 도래한 주기가 없으면 비교 한 번만 하고 돌아간다.
	void Tick(Clock::time_point now)
	{
		if (now < nextDue_)
			return;

		Fire(now);
	}

	// 다음 마감 시각(테스트/디버깅용).
	Clock::time_point NextDue() const { return nextDue_; }

private:
	// 도래한 주기를 실행하고 다음 마감 시각을 다시 계산한다.
	// 초당 한 번 수준으로만 불리므로 인라인하지 않는다.
	void Fire(Clock::time_point now);

	Clock::time_point nextCheck_;
	Clock::time_point nextReport_;
	// min(nextCheck_, nextReport_). 프레임당 비교를 1회로 줄이기 위한 캐시.
	Clock::time_point nextDue_;
};
