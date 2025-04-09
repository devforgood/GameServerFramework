#include "Player.h"
#include "Server.h"

void Player::set_session(game_session* session)
{
	session_ = session;
}


void Player::async_db_query() {
    int user_id = 1;
    int query_id = 2;
    std::cout << "[User " << user_id << "] Handling DB Query #" << query_id
        << " on post " << std::this_thread::get_id() << std::endl;

    boost::asio::post(session_->strand_, [user_id, query_id]() {
        std::cout << "[User " << user_id << "] Handling DB Query #" << query_id
            << " on Thread " << std::this_thread::get_id() << std::endl;
        std::this_thread::sleep_for(std::chrono::milliseconds(500)); // DB I/O delay
        std::cout << "[User " << user_id << "] Finished DB Query #" << query_id << std::endl;
        });
}