#pragma once

#include <atomic>
#include <chrono>
#include <cstdint>
#include <mutex>
#include <string>
#include <thread>
#include <unordered_map>
#include <vector>

//
// DbThreadMonitor
// ------------------------------------------------------------------
// DB 스레드 풀(dbThreadPool_) 에서 실행되는 작업을 모니터링한다.
//  - 각 작업의 큐 대기 시간(wait) / 실제 실행 시간(exec) 측정
//  - 요청한 메시지(쿼리) 종류와 Player id 기록
//  - 스레드별 누적 사용 시간 / 처리 건수 집계
//  - 일정 시간 이상 끝나지 않은 작업 감시 → 데드락 / 슬로우 쿼리 추적
//
// 사용 패턴:
//   uint64_t token = DbThreadMonitor::Instance().BeginEnqueue("PlayerLoad", playerId);
//   boost::asio::post(strand, [token, ...]() {
//       DbTaskScope _scope(token);   // 시작/종료를 자동 기록
//       ... 실제 DB 작업 ...
//   });
//
class DbThreadMonitor
{
public:
	using Clock = std::chrono::steady_clock;

	// 스레드별 누적 통계.
	struct ThreadStat
	{
		uint64_t                 taskCount = 0;
		std::chrono::nanoseconds busyTime{ 0 };
	};

	// 데드락/슬로우 의심으로 수집된 실행 중 작업의 스냅샷.
	struct StuckTaskInfo
	{
		uint64_t    id = 0;
		std::string name;
		long        playerId = 0;
		size_t      threadHash = 0;
		double      runningMs = 0.0;
	};

	static DbThreadMonitor& Instance();

	// 작업을 스레드 풀에 post 할 때 호출한다. 큐 대기 시간 측정을 위해
	// enqueue 시각을 기록하고 추적용 토큰(id)을 반환한다.
	uint64_t BeginEnqueue(std::string name, long playerId);

	// 작업이 실제 스레드에서 실행을 시작 / 종료할 때 호출 (DbTaskScope 가 자동 호출).
	void OnStart(uint64_t taskId);
	void OnFinish(uint64_t taskId);

	// stuckThreshold_ 이상 실행 중인 작업을 수집한다(로깅 없음 → 단위 테스트/조회용).
	std::vector<StuckTaskInfo> CollectStuckTasks() const;

	// CollectStuckTasks 결과를 경고 로깅한다(중복 경고 방지).
	// 메인 루프에서 주기적으로 호출 → 데드락 / 멈춘 쿼리 조기 감지.
	void CheckStuckTasks();

	// 스레드별 누적 사용 시간 / 처리 건수와 현재 대기/실행 중 작업 수를 로깅한다.
	void ReportThreadUsage();

	// ---- 관측용 메트릭 (모니터링 / 테스트) ----
	size_t   InflightCount()  const; // 큐 대기 + 실행 중인 작업 수
	size_t   RunningCount()   const; // 실행 중인 작업 수
	size_t   QueuedCount()    const; // 아직 시작 안 한(큐 대기) 작업 수
	uint64_t CompletedCount() const { return completedCount_.load(std::memory_order_relaxed); }
	uint64_t SlowCount()      const { return slowCount_.load(std::memory_order_relaxed); }
	std::unordered_map<size_t, ThreadStat> SnapshotThreadStats() const;

	void SetSlowThreshold(std::chrono::milliseconds ms) { slowThreshold_ = ms; }
	void SetStuckThreshold(std::chrono::milliseconds ms) { stuckThreshold_ = ms; }
	std::chrono::milliseconds GetSlowThreshold() const { return slowThreshold_; }
	std::chrono::milliseconds GetStuckThreshold() const { return stuckThreshold_; }

	// 누적 상태/카운터를 모두 초기화한다(임계값은 기본값으로 복원). 테스트/재구성용.
	void Reset();

private:
	DbThreadMonitor() = default;
	DbThreadMonitor(const DbThreadMonitor&) = delete;
	DbThreadMonitor& operator=(const DbThreadMonitor&) = delete;

	// 실행 중(in-flight)인 작업 한 건의 정보.
	struct TaskRecord
	{
		uint64_t           id = 0;
		std::string        name;          // 요청한 메시지 / 쿼리 종류
		long               playerId = 0;
		Clock::time_point  enqueueTime;    // post 된 시각
		Clock::time_point  startTime;      // 스레드에서 실행을 시작한 시각
		std::thread::id    threadId;
		bool               started = false;
		bool               stuckWarned = false; // 데드락 경고 중복 방지
	};

	mutable std::mutex                        mutex_;
	std::atomic<uint64_t>                     nextId_{ 1 };
	std::atomic<uint64_t>                     completedCount_{ 0 };
	std::atomic<uint64_t>                     slowCount_{ 0 };
	std::unordered_map<uint64_t, TaskRecord>  inflight_;
	std::unordered_map<size_t, ThreadStat>    threadStats_; // hash(thread id) -> 통계

	// 이 시간을 넘긴 실행은 슬로우 쿼리로 경고.
	std::chrono::milliseconds slowThreshold_{ 200 };
	// 이 시간을 넘겨도 끝나지 않은 작업은 데드락 의심으로 경고.
	std::chrono::milliseconds stuckThreshold_{ 3000 };
};

//
// DbTaskScope
// post 된 람다 내부에서 스택에 생성한다.
// 생성 시 OnStart, 소멸 시 OnFinish 를 호출하여 예외 발생 시에도 종료가 기록되게 한다.
//
class DbTaskScope
{
public:
	explicit DbTaskScope(uint64_t taskId) : taskId_(taskId)
	{
		DbThreadMonitor::Instance().OnStart(taskId_);
	}
	~DbTaskScope()
	{
		DbThreadMonitor::Instance().OnFinish(taskId_);
	}
	DbTaskScope(const DbTaskScope&) = delete;
	DbTaskScope& operator=(const DbTaskScope&) = delete;

private:
	uint64_t taskId_;
};
