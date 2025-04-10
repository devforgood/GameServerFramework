#include "SqlClient.h"  
#include <iostream>  
#include <stdexcept> 
#include <mariadb/conncpp.hpp>

void SqlClient::test() {  
    try {
        // MariaDB ���� ����
        sql::SQLString url("jdbc:mariadb://localhost:3306/testdb");
        sql::Properties properties({
            {"user", "root"},
            {"password", "1234"}
            });

        // ����̹� �� ���� ����
        sql::Driver* driver = sql::mariadb::get_driver_instance();
        std::unique_ptr<sql::Connection> conn(driver->connect(url, properties));

        // ���� Ȯ��
        if (conn->isValid()) {
            std::cout << "MariaDB�� ���������� ����Ǿ����ϴ�!" << std::endl;
        }

        // ���� ����
        std::unique_ptr<sql::Statement> stmt(conn->createStatement());
        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery("SELECT id, name FROM users"));

        // ��� ���
        while (res->next()) {
            std::cout << "ID: " << res->getInt("id")
                << ", Name: " << res->getString("name") << std::endl;
        }
    }
    catch (const sql::SQLException& e) {
        std::cerr << "SQL ����: " << e.what() << std::endl;
    }
    catch (const std::exception& e) {
        std::cerr << "����: " << e.what() << std::endl;
    }
}