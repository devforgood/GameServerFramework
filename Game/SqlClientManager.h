#pragma once
#include <boost/core/noncopyable.hpp>
#include <boost/thread/tss.hpp>

class SqlClient; // Forward declaration of SqlClient
class SqlClientManager : private boost::noncopyable 
{
public:
    static SqlClientManager& getInstance() {
        static SqlClientManager instance;
        return instance;
    }

    void init();

private:
    SqlClientManager() = default;
    ~SqlClientManager() = default;

public:
	boost::thread_specific_ptr<SqlClient> sqlClientPtr; // Thread-specific pointer to SqlClient
};

