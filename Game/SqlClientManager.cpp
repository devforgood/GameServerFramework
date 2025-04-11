#include "SqlClientManager.h"
#include "SqlClient.h" // SqlClient 헤더 파일 포함

void SqlClientManager::init()
{
	// SqlClient 객체를 생성하여 thread_specific_ptr에 저장합니다.
	sqlClientPtr.reset(new SqlClient());
}
