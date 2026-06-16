#include "DbThreadMonitor.h"
#include "LogHelper.h"

#include "spdlog/sinks/null_sink.h"

namespace
{
	inline double ToMs(std::chrono::nanoseconds ns)
	{
		return std::chrono::duration_cast<std::chrono::duration<double, std::milli>>(ns).count();
	}

	// "net" 로거를 안전하게 가져온다. InitLog() 가 호출되지 않은 환경(단위 테스트 등)
	// 에서는 spdlog::get("net") 가 nullptr 이므로, LOG 매크로(= *(spdlog::get("net")))
	// 를 그대로 쓰면 널 역참조로 크래시한다. 그런 경우 출력 없는 null 로거로 대체한다.
	spdlog::logger& NetLog()
	{
		static const std::shared_ptr<spdlog::logger> fallback =
			std::make_shared<spdlog::logger>(
				"dbmon-null", std::make_shared<spdlog::sinks::null_sink_mt>());

		const std::shared_ptr<spdlog::logger> net = spdlog::get("net");
		return net ? *net : *fallback;
	}
}

DbThreadMonitor& DbThreadMonitor::Instance()
{
	static DbThreadMonitor instance;
	return instance;
}

uint64_t DbThreadMonitor::BeginEnqueue(std::string name, long playerId)
{
	const uint64_t id = nextId_.fetch_add(1, std::memory_order_relaxed);

	TaskRecord rec;
	rec.id = id;
	rec.name = std::move(name);
	rec.playerId = playerId;
	rec.enqueueTime = Clock::now();

	{
		std::lock_guard<std::mutex> lock(mutex_);
		inflight_.emplace(id, std::move(rec));
	}
	return id;
}

void DbThreadMonitor::OnStart(uint64_t taskId)
{
	std::lock_guard<std::mutex> lock(mutex_);
	auto it = inflight_.find(taskId);
	if (it == inflight_.end())
		return;

	TaskRecord& rec = it->second;
	rec.startTime = Clock::now();
	rec.threadId = std::this_thread::get_id();
	rec.started = true;
}

void DbThreadMonitor::OnFinish(uint64_t taskId)
{
	std::string  name;
	long         playerId = 0;
	size_t       threadHash = 0;
	double       waitMs = 0.0;
	double       execMs = 0.0;
	bool         slow = false;

	{
		std::lock_guard<std::mutex> lock(mutex_);
		auto it = inflight_.find(taskId);
		if (it == inflight_.end())
			return;

		const TaskRecord& rec = it->second;
		const Clock::time_point now = Clock::now();

		const auto waitNs = rec.startTime - rec.enqueueTime;
		const auto execNs = now - rec.startTime;

		name = rec.name;
		playerId = rec.playerId;
		threadHash = std::hash<std::thread::id>{}(rec.threadId);
		waitMs = ToMs(waitNs);
		execMs = ToMs(execNs);
		slow = execNs >= std::chrono::duration_cast<std::chrono::nanoseconds>(slowThreshold_);

		// 스레드별 누적 사용 시간 / 처리 건수 갱신.
		ThreadStat& stat = threadStats_[threadHash];
		stat.taskCount += 1;
		stat.busyTime += std::chrono::duration_cast<std::chrono::nanoseconds>(execNs);

		inflight_.erase(it);
	}

	completedCount_.fetch_add(1, std::memory_order_relaxed);
	if (slow)
		slowCount_.fetch_add(1, std::memory_order_relaxed);

	// 락 밖에서 로깅.
	if (slow)
	{
		NetLog().warn("[DBMON] SLOW task '{}' player={} thread={} wait={:.1f}ms exec={:.1f}ms",
			name, playerId, threadHash, waitMs, execMs);
	}
	else
	{
		NetLog().debug("[DBMON] task '{}' player={} thread={} wait={:.1f}ms exec={:.1f}ms",
			name, playerId, threadHash, waitMs, execMs);
	}
}

std::vector<DbThreadMonitor::StuckTaskInfo> DbThreadMonitor::CollectStuckTasks() const
{
	const Clock::time_point now = Clock::now();
	const auto stuckNs = std::chrono::duration_cast<std::chrono::nanoseconds>(stuckThreshold_);

	std::vector<StuckTaskInfo> result;
	std::lock_guard<std::mutex> lock(mutex_);
	for (const auto& kv : inflight_)
	{
		const TaskRecord& rec = kv.second;
		if (!rec.started)
			continue; // 큐에서 대기만 하는 작업은 데드락 대상이 아님.

		const auto runningNs = now - rec.startTime;
		if (runningNs >= stuckNs)
		{
			StuckTaskInfo info;
			info.id = rec.id;
			info.name = rec.name;
			info.playerId = rec.playerId;
			info.threadHash = std::hash<std::thread::id>{}(rec.threadId);
			info.runningMs = ToMs(runningNs);
			result.push_back(std::move(info));
		}
	}
	return result;
}

void DbThreadMonitor::CheckStuckTasks()
{
	const Clock::time_point now = Clock::now();
	const auto stuckNs = std::chrono::duration_cast<std::chrono::nanoseconds>(stuckThreshold_);

	std::lock_guard<std::mutex> lock(mutex_);
	for (auto& kv : inflight_)
	{
		TaskRecord& rec = kv.second;
		if (!rec.started)
			continue;

		const auto runningNs = now - rec.startTime;
		if (runningNs >= stuckNs && !rec.stuckWarned)
		{
			rec.stuckWarned = true;
			NetLog().warn("[DBMON] STUCK task '{}' player={} thread={} running={:.1f}ms "
				"(deadlock/slow-query suspect)",
				rec.name, rec.playerId,
				std::hash<std::thread::id>{}(rec.threadId),
				ToMs(runningNs));
		}
	}
}

void DbThreadMonitor::ReportThreadUsage()
{
	std::lock_guard<std::mutex> lock(mutex_);

	size_t waiting = 0;
	size_t running = 0;
	for (const auto& kv : inflight_)
	{
		if (kv.second.started) ++running;
		else                   ++waiting;
	}

	NetLog().info("[DBMON] usage: threads={} inflight(running={}, queued={})",
		threadStats_.size(), running, waiting);

	for (const auto& kv : threadStats_)
	{
		const ThreadStat& stat = kv.second;
		const double busyMs = ToMs(stat.busyTime);
		const double avgMs = stat.taskCount ? busyMs / static_cast<double>(stat.taskCount) : 0.0;
		NetLog().info("[DBMON]   thread={} tasks={} busy={:.1f}ms avg={:.1f}ms",
			kv.first, stat.taskCount, busyMs, avgMs);
	}
}

size_t DbThreadMonitor::InflightCount() const
{
	std::lock_guard<std::mutex> lock(mutex_);
	return inflight_.size();
}

size_t DbThreadMonitor::RunningCount() const
{
	std::lock_guard<std::mutex> lock(mutex_);
	size_t running = 0;
	for (const auto& kv : inflight_)
		if (kv.second.started) ++running;
	return running;
}

size_t DbThreadMonitor::QueuedCount() const
{
	std::lock_guard<std::mutex> lock(mutex_);
	size_t queued = 0;
	for (const auto& kv : inflight_)
		if (!kv.second.started) ++queued;
	return queued;
}

std::unordered_map<size_t, DbThreadMonitor::ThreadStat> DbThreadMonitor::SnapshotThreadStats() const
{
	std::lock_guard<std::mutex> lock(mutex_);
	return threadStats_;
}

void DbThreadMonitor::Reset()
{
	std::lock_guard<std::mutex> lock(mutex_);
	inflight_.clear();
	threadStats_.clear();
	nextId_.store(1, std::memory_order_relaxed);
	completedCount_.store(0, std::memory_order_relaxed);
	slowCount_.store(0, std::memory_order_relaxed);
	slowThreshold_ = std::chrono::milliseconds(200);
	stuckThreshold_ = std::chrono::milliseconds(3000);
}
