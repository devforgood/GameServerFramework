#pragma once

#include <algorithm>
#include <atomic>
#include <condition_variable>
#include <cstddef>
#include <functional>
#include <memory>
#include <mutex>
#include <thread>
#include <vector>

namespace engine
{
	//-------------------------------------------------------------------------------------
	// 프로세스에 하나뿐인 작업자 풀과 그 위의 parallel-for.
	//
	// 하위 계층(ECS)에 두는 이유는 두 가지다.
	//   - Engine 이 ECS 를 참조하므로, 시스템(SystemManager)과 맵(Map) 양쪽에서 쓸 수 있다.
	//   - 풀은 프로세스에 하나여야 한다. 월드 하나 = 스레드 하나라는 구조라 풀을 월드마다
	//     두면 월드 16개 x 4스레드 = 64스레드가 되어 코어보다 많아진다. 게다가 모든 월드가
	//     10Hz 로 같이 틱하므로 병렬 구간도 같이 몰린다 - 하나를 나눠 쓰는 것이 맞다.
	//
	// 호출한 스레드도 함께 일한다. 그래서 보조 스레드가 0개면 그냥 순차 실행이 되고,
	// Start() 를 부르지 않은 테스트/벤치마크는 예전과 완전히 같은 동작을 얻는다.
	//
	// 콜백은 fn(index, workerIndex) 로 불린다. workerIndex 는 [0, Size()] 범위이고
	// 호출 스레드가 0 이다 - 스레드별 스크래치를 색인하는 데 쓰라고 넘긴다.
	// 따라서 스크래치 배열은 항상 Size() + 1 개가 필요하다.
	//-------------------------------------------------------------------------------------
	class WorkerPool
	{
	public:
		static WorkerPool& Instance()
		{
			static WorkerPool pool;
			return pool;
		}

		// 보조 스레드를 띄운다. threadCount 가 0 이하면 풀을 쓰지 않는다(순차 실행).
		// 기동 시 한 번만 부른다. 두 번째 호출은 무시한다.
		void Start(int threadCount)
		{
			std::lock_guard<std::mutex> lock(mutex_);
			if (!threads_.empty() || threadCount <= 0)
				return;

			stop_ = false;
			threads_.reserve(static_cast<size_t>(threadCount));
			for (int i = 0; i < threadCount; ++i)
			{
				// 워커 번호는 1부터. 0 은 호출 스레드 몫이다.
				threads_.emplace_back([this, i] { WorkerLoop(i + 1); });
			}
		}

		void Stop()
		{
			std::vector<std::thread> threads;
			{
				std::lock_guard<std::mutex> lock(mutex_);
				if (threads_.empty())
					return;
				stop_ = true;
				threads.swap(threads_);
			}
			cv_.notify_all();
			for (std::thread& t : threads)
			{
				if (t.joinable())
					t.join();
			}
		}

		// 보조 스레드 수. 스크래치는 이 값 + 1 개가 필요하다.
		int Size() const
		{
			std::lock_guard<std::mutex> lock(mutex_);
			return static_cast<int>(threads_.size());
		}

		// 이 기계에 맞는 기본 보조 스레드 수.
		// 코어를 다 쓰지 않는다 - 워커(월드) 스레드들이 이미 코어를 쓰고 있고,
		// 측정상 4-way(보조 3 + 호출자)에서 2.6배가 나와 그 이상은 이득이 얇다.
		static int AutoThreadCount()
		{
			const unsigned hw = std::thread::hardware_concurrency();
			if (hw == 0)
				return 0;
			return std::clamp(static_cast<int>(hw / 8), 0, 3);
		}

		~WorkerPool() { Stop(); }

		WorkerPool(const WorkerPool&) = delete;
		WorkerPool& operator=(const WorkerPool&) = delete;

		// ParallelFor 가 부른다. 직접 쓸 일은 없다.
		void Run(size_t count, size_t chunk, const std::function<void(size_t, int)>& fn)
		{
			auto job = std::make_shared<Job>();
			job->count = count;
			job->chunk = chunk == 0 ? 1 : chunk;
			job->fn = &fn;

			{
				std::lock_guard<std::mutex> lock(mutex_);
				jobs_.push_back(job);
			}
			cv_.notify_all();

			// 호출 스레드도 청크를 가져다 처리한다(번호 0).
			RunChunks(*job, 0);

			// 마지막 청크를 다른 스레드가 아직 돌고 있을 수 있다. 전부 끝나야 돌아간다 -
			// 이 배리어가 있어야 호출자가 pending 을 비우고 다음 단계로 갈 수 있다.
			{
				std::unique_lock<std::mutex> lock(job->doneMutex);
				job->doneCv.wait(lock, [&job] {
					return job->done.load(std::memory_order_acquire) >= job->count;
				});
			}

			{
				std::lock_guard<std::mutex> lock(mutex_);
				jobs_.erase(std::remove(jobs_.begin(), jobs_.end(), job), jobs_.end());
			}
		}

	private:
		WorkerPool() = default;

		struct Job
		{
			std::atomic<size_t> next{ 0 };   // 다음에 나눠 줄 첫 인덱스
			std::atomic<size_t> done{ 0 };   // 처리가 끝난 개수
			size_t count = 0;
			size_t chunk = 1;
			const std::function<void(size_t, int)>* fn = nullptr;

			std::mutex doneMutex;
			std::condition_variable doneCv;
		};

		// 남은 청크를 소진할 때까지 가져다 처리한다.
		// 청크를 원자 커서로 나눠 주므로 뷰어마다 부하가 달라도 알아서 균형이 맞는다
		// (정적 분할이면 가장 무거운 조각을 맡은 스레드를 나머지가 기다린다).
		static void RunChunks(Job& job, int workerIndex)
		{
			for (;;)
			{
				const size_t begin = job.next.fetch_add(job.chunk, std::memory_order_relaxed);
				if (begin >= job.count)
					break;

				const size_t end = std::min(begin + job.chunk, job.count);
				for (size_t i = begin; i < end; ++i)
					(*job.fn)(i, workerIndex);

				const size_t finished =
					job.done.fetch_add(end - begin, std::memory_order_acq_rel) + (end - begin);
				if (finished >= job.count)
				{
					// 대기자가 술어를 다시 확인하도록 뮤텍스를 한 번 잡고 깨운다.
					std::lock_guard<std::mutex> lock(job.doneMutex);
					job.doneCv.notify_all();
				}
			}
		}

		bool HasPendingWork() const
		{
			for (const auto& job : jobs_)
			{
				if (job->next.load(std::memory_order_relaxed) < job->count)
					return true;
			}
			return false;
		}

		void WorkerLoop(int workerIndex)
		{
			for (;;)
			{
				std::shared_ptr<Job> job;
				{
					std::unique_lock<std::mutex> lock(mutex_);
					// '남은 청크가 있는 잡' 을 술어로 쓴다. 단순히 !jobs_.empty() 로 두면
					// 청크가 다 나간 뒤 제거되기 전까지 깨어난 채로 돌게 된다.
					cv_.wait(lock, [this] { return stop_ || HasPendingWork(); });
					if (stop_)
						return;

					for (const auto& candidate : jobs_)
					{
						if (candidate->next.load(std::memory_order_relaxed) < candidate->count)
						{
							job = candidate;   // shared_ptr 복사 - 잡의 수명이 보장된다
							break;
						}
					}
				}

				if (job)
					RunChunks(*job, workerIndex);
			}
		}

		mutable std::mutex mutex_;
		std::condition_variable cv_;
		std::vector<std::thread> threads_;
		std::vector<std::shared_ptr<Job>> jobs_;
		bool stop_ = false;
	};

	//-------------------------------------------------------------------------------------
	// [0, count) 를 chunk 단위로 나눠 처리한다. 전부 끝나야 돌아온다(배리어).
	//
	// 풀이 비었거나 일이 청크 하나 분량이면 그 자리에서 순차 실행한다 - 스레드를 깨우는
	// 값이 일보다 비싼 구간을 병렬로 돌리지 않기 위한 것이고, 덕분에 Start() 를 부르지
	// 않은 실행 경로는 예전과 동일하다.
	//-------------------------------------------------------------------------------------
	template <class F>
	void ParallelFor(size_t count, size_t chunk, F&& fn)
	{
		if (count == 0)
			return;

		WorkerPool& pool = WorkerPool::Instance();
		if (count <= chunk || pool.Size() == 0)
		{
			for (size_t i = 0; i < count; ++i)
				fn(i, 0);
			return;
		}

		const std::function<void(size_t, int)> wrapped(std::forward<F>(fn));
		pool.Run(count, chunk, wrapped);
	}
}
