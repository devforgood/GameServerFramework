#include "SqlClientManager.h"
#include "SqlClient.h" // SqlClient ��� ���� ����

void SqlClientManager::init()
{
	// SqlClient ��ü�� �����Ͽ� thread_specific_ptr�� �����մϴ�.
	sqlClientPtr.reset(new SqlClient());
}
