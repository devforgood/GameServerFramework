#pragma once
#include <memory>

class send_message;

// BT 디버그 뷰어 동기화 직렬화기.
// BTDebugManager 에 쌓인 스냅샷(트리 정의 + 런타임 프레임)을 TreeDebugSync 메시지로
// 직렬화한다. 맵 틱(Map::SendTreeDebugSync)이 이 메시지를 받아 브로드캐스트만 담당한다.
namespace BTDebugSync
{
	// 보낼 내용이 없으면(스냅샷 비었음 / ENABLE_BT_DEBUG 미정의) nullptr 를 반환한다.
	std::shared_ptr<send_message> BuildMessage();
}
