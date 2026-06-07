#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "Character.h"
#include <string>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"
#include "LogHelper.h"
#include "PlayerDataLoader.h"
#include "PlayerData.h"
#include "PlayerQuest.h"
#include "PlayerItem.h"
#include "PlayerSkill.h"
// todo :  Player ID 디비에서 관리 개선 필요
static long next_player_id = 1;

Player::Player()
{
	uuid_ = boost::uuids::random_generator()();
	player_id_ = next_player_id++;

	this->AddComponent<PlayerQuest>();
	this->AddComponent<PlayerItem>();
	this->AddComponent<PlayerSkill>();
}

Player::~Player() 
{
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

void Player::on_loaded_data(PlayerData data)
{
	LOG.info("Player {} loaded data: name={}, items={}, skills={}", player_id_, data.player.name, data.items.size(), data.skills.size());
	set_name(data.player.name);
	set_level(data.player.level);
}

void Player::update(float dt)
{
	GameObject::update(dt);
}

std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> Player::get_strand()
{
	if (auto session = session_.lock())
	{
		return session->strand_;
	}

	return std::nullopt;
}