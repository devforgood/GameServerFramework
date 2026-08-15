#include "spdlog/spdlog.h"
#include "spdlog/sinks/null_sink.h"
#include <memory>

// 엔진의 LOG 매크로는 `*(spdlog::get("net"))` 이다. "net" 로거가 등록돼 있지 않으면
// 널 역참조로 프로세스가 죽는다(테스트는 SEH 0xC0000005 로 잡힌다).
//
// 테스트 하네스는 InitLog 를 부르지 않으므로 출력 없는 널 로거를 여기서 등록한다.
// main 이 아니라 정적 초기화에 둔 이유는, 어느 main 이 링크되는지에 기대지 않기 위해서다
// (gtest 패키지가 자기 main 을 제공할 수 있다). 개별 테스트가 알아서 등록하게 두면
// 실행 순서에 따라 죽고 살아서, 로그 한 줄을 추가했을 뿐인데 관계없는 테스트가
// 접근 위반으로 터진다 — 실제로 그렇게 한 번 겪었다.
namespace
{
	struct NetLoggerRegistrar
	{
		NetLoggerRegistrar()
		{
			if (spdlog::get("net") != nullptr)
				return;

			auto logger = std::make_shared<spdlog::logger>(
				"net", std::make_shared<spdlog::sinks::null_sink_mt>());
			logger->set_level(spdlog::level::debug);
			spdlog::register_logger(logger);
		}
	};

	// 정적 초기화 시점에 등록된다. spdlog 의 레지스트리는 함수 지역 정적이라
	// 여기서 먼저 불려도 안전하게 만들어진다.
	const NetLoggerRegistrar g_netLoggerRegistrar;
}
