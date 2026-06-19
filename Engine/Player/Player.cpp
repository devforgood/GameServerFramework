#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "Character.h"
#include <string>
#include <mariadb/conncpp.hpp>
#include "./SQL/generated/dao.h"
#include "LogHelper.h"
#include "PlayerRepository.h"
#include "PlayerLoadData.h"
#include "PlayerQuest.h"
#include "PlayerItem.h"
#include "PlayerSkill.h"
#include "PlayerLevel.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "PlayerEventBrokerProxy.h"

// todo :  Player ID 디비에서 관리 개선 필요
static long next_player_id = 1;

Player::Player()
{
	uuid_ = boost::uuids::random_generator()();
	playerId_ = next_player_id++;

	playerLazySaveAcc_ = 0.0f;

	this->AddComponent<PlayerEventBroker>();
	this->AddComponent<PlayerQuest>();
	this->AddComponent<PlayerItem>();
	this->AddComponent<PlayerSkill>();
	this->AddComponent<PlayerLevel>();
}

Player::~Player() 
{
}


void Player::SetSession(std::shared_ptr<GameSession> session)
{
	session_ = session;
}

void Player::SetServer(GameServer* server)
{
	server_ = server;
}


void Player::Possess(std::shared_ptr<Character> character)
{
	character_ = character;
	character_->SetPlayerId(playerId_);

	// Player가 소유한 PlayerEventBroker를 Character가 동일하게 사용할 수 있도록
	// 프록시 컴포넌트를 부착하고, 브로커의 소유자(Player)를 약하게 참조하게 한다.
	auto* proxy = character_->AddComponent<PlayerEventBrokerProxy>();
	proxy->SetBrokerOwner(weak_from_this());
}

void Player::Send(std::shared_ptr<send_message>& msg)
{
	auto session = session_.lock();
	if (session)
	{
		session->Send(msg);
	}
	else
	{
		LOG.error("Session expired! in send");
	}
}

void Player::Close()
{
	auto session = session_.lock();
	if (session)
	{
		session->Close();
	}
	else
	{
		LOG.error("Session expired! in close");
	}
}

bool Player::SwitchSession(std::shared_ptr<Player> player)
{
	if (this == player.get())
	{
		LOG.error("Player is same! in switch_session");
		return false;
	}

	// 기존 세션 정리
	Close();

	auto session = player->session_.lock();
	if (!session)
	{
		LOG.error("Session expired! in switch_session");
		return false;
	}

	session->SetPlayer(shared_from_this());
	LOG.info("Switching session for player {} to new session", playerId_);
	return true;
}

void Player::OnLoadedData(const PlayerLoadData & data)
{
	LOG.info("Player {} loaded data: name={}, items={}, skills={}", playerId_, data.player.name, data.items.size(), data.skills.size());
	SetName(data.player.name);
	SetLevel(data.player.level);

	ForEachComponent([&data](Component& component)
	{
		component.Load(data);
	});
}

void Player::Update(float dt)
{
	GameObject::Update(dt);


	// 1분마다 플레이어 데이터를 디비에 저장하는 로직
	playerLazySaveAcc_ += dt;
	if (playerLazySaveAcc_ >= 60.0f)
	{
		SavePlayerData();
		playerLazySaveAcc_ -= 60.0f;
	}
}

std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> Player::GetStrand()
{
	if (auto session = session_.lock())
	{
		return session->strand_;
	}

	return std::nullopt;
}

void Player::SavePlayerData()
{
	// 각 컴포넌트에서 플레이어 데이터를 수집
	auto save_data = std::make_shared<PlayerSaveData>();

	ForEachComponent([&save_data](Component& component)
	{
		if (component.IsDirty())
		{
			component.Save(save_data.get());
			component.ClearDirty();
		}
	});

	// 변경된 데이터만 비동기로 전달
	PlayerRepository::AsyncSave(shared_from_this(), save_data);
}