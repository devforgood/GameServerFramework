#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "Character.h"
#include <string>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"
#include "LogHelper.h"


// todo :  Player ID 디비에서 관리 개선 필요
static long next_player_id = 1;

Player::Player()
{
	uuid_ = boost::uuids::random_generator()();
	player_id_ = next_player_id++;
}

Player::~Player() {
}


void Player::set_session(std::shared_ptr<game_session> session)
{
	session_ = session;
}

void Player::set_server(game_server* server)
{
	server_ = server;
}


void Player::possess(std::shared_ptr<Character> character)
{
	character_ = character;
	character_->set_player_id(player_id_);
}

void Player::async_db_query() {
    int player_id = 1;
    int query_id = 2;
    std::cout << "[Player " << player_id << "] Handling DB Query #" << query_id
        << " on post " << std::this_thread::get_id() << std::endl;

	auto session = session_.lock();
	if (!session) {
		std::cerr << "Session expired!" << std::endl;
		return;
	}
	auto io_context = server_->get_io_context();
	std::weak_ptr<Player> weak_player = shared_from_this();

    boost::asio::post(session->strand_, [weak_player, player_id, query_id, io_context]() {
        std::cout << "[Player " << player_id << "] Handling DB Query #" << query_id
            << " on Thread " << std::this_thread::get_id() << std::endl;


		PlayerDAO player_dao(SqlClientManager::getInstance().sqlClientPtr->getConnection());
		PlayerVO player_vo;
		player_dao.Select(player_id, player_vo);
		std::cout << "[Player " << player_id << "] Player ID: " << player_vo.id
			<< ", Player Name: " << player_vo.name
			<< ", Player Level: " << player_vo.level << std::endl;

        std::cout << "[Player " << player_id << "] Finished DB Query #" << query_id << std::endl;


		boost::asio::post(*io_context, [player_vo, weak_player]() {
			if (auto player = weak_player.lock()) {
				std::cout << "[Player " << player_vo.id << "] Player Name: " << player_vo.name
					<< " on Thread " << std::this_thread::get_id() << std::endl;
				player->name_ = player_vo.name;
			}
		});
    });
}

void Player::send(std::shared_ptr<send_message>& msg)
{
	auto session = session_.lock();
	if (session)
	{
		session->send(msg);
	}
	else
	{
		LOG.error("Session expired! in send");
	}
}

void Player::close()
{
	auto session = session_.lock();
	if (session)
	{
		session->close();
	}
	else
	{
		LOG.error("Session expired! in close");
	}
}

bool Player::switch_session(std::shared_ptr<Player> player)
{
	if (this == player.get())
	{
		LOG.error("Player is same! in switch_session");
		return false;
	}

	// 기존 세션 정리
	close();

	auto session = player->session_.lock();
	if (!session)
	{
		LOG.error("Session expired! in switch_session");
		return false;
	}

	session->set_player(shared_from_this());
	LOG.info("Switching session for player {} to new session", player_id_);
	return true;
}