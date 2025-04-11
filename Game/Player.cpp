#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include <string>
#include <mariadb/conncpp.hpp>



class DMUser : public IResultParser {
public:
	int user_id;
	std::string user_name;

	void parse(sql::ResultSet* resultSet) override {
		// 결과를 파싱하는 로직을 구현합니다.
		while (resultSet->next()) {
			user_id = resultSet->getInt("id");
			user_name = resultSet->getString("name");
		}
	}
};


void Player::set_session(std::shared_ptr<game_session> session)
{
	session_ = session;
}

void Player::set_server(game_server* server)
{
	server_ = server;
}


void Player::async_db_query() {
    int user_id = 1;
    int query_id = 2;
    std::cout << "[User " << user_id << "] Handling DB Query #" << query_id
        << " on post " << std::this_thread::get_id() << std::endl;

	auto session = session_.lock();
	if (!session) {
		std::cerr << "Session expired!" << std::endl;
		return;
	}
	auto io_context = server_->get_io_context();

    boost::asio::post(session->strand_, [user_id, query_id, io_context]() {
        std::cout << "[User " << user_id << "] Handling DB Query #" << query_id
            << " on Thread " << std::this_thread::get_id() << std::endl;

		std::vector<std::string> params = { "1" };
		std::shared_ptr<DMUser> user = std::make_shared<DMUser>();

		SqlClientManager::getInstance().sqlClientPtr->select("SELECT * FROM users WHERE id = ?", params, *user);

		std::cout << "[User " << user_id << "] User ID: " << user->user_id
			<< ", User Name: " << user->user_name << std::endl;

        std::cout << "[User " << user_id << "] Finished DB Query #" << query_id << std::endl;


		boost::asio::post(*io_context, [user]() {
			std::cout << "[User " << user->user_id << "] User Name: " << user->user_name
				<< " on Thread " << std::this_thread::get_id() << std::endl;
			});
        });
}