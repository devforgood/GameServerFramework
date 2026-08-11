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
#include "PlayerWallet.h"
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
	this->AddComponent<PlayerWallet>();
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

void Player::UnPossess()
{
	// 게이트 이동 등으로 캐릭터를 다른 맵에 재생성하기 전, 기존 빙의를 해제한다.
	// character_ 의 마지막 참조가 사라지면 이전 맵에서 제거된 Character 가 파괴된다.
	character_ = nullptr;
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

	// 접속하지 않은 사이에 지난 시간을 정산한다(제한 시간 만료, 일일 리셋 등).
	if (auto* broker = GetComponent<PlayerEventBroker>())
		broker->publish(EventPlayerJoined{ static_cast<int>(playerId_) });

	// 로그인 직후에는 진행 중인 퀘스트 전체를 클라에 내려준다(다음 Update 에서 한 통으로).
	if (auto* quests = GetComponent<PlayerQuest>())
		quests->MarkAllForSync();
}

void Player::Update(float dt)
{
	GameObject::Update(dt);

	SendQuestSync();

	// 1분마다 플레이어 데이터를 디비에 저장하는 로직
	playerLazySaveAcc_ += dt;
	if (playerLazySaveAcc_ >= 60.0f)
	{
		SavePlayerData();
		playerLazySaveAcc_ -= 60.0f;
	}
}

void Player::SendQuestSync()
{
	auto* quests = GetComponent<PlayerQuest>();
	if (quests == nullptr || !quests->HasPendingSync())
		return;

	std::vector<int> changed;
	std::vector<int> removed;
	std::vector<int> completed;
	quests->DrainSync(changed, removed, completed);

	auto builder_ptr = std::make_shared<send_message>();

	std::vector<flatbuffers::Offset<syncnet::QuestInfo>> infos;
	infos.reserve(changed.size());
	for (int quest_id : changed)
	{
		const QuestActiveVO* vo = quests->GetActiveQuest(quest_id);
		if (vo == nullptr)
			continue; // 모아 두는 사이에 사라졌다 — removed 로 이미 나간다

		const int progress[] = { vo->progress1, vo->progress2, vo->progress3 };
		infos.push_back(syncnet::CreateQuestInfo(
			*builder_ptr,
			quest_id,
			static_cast<int8_t>(vo->state),
			vo->stage,
			builder_ptr->CreateVector(progress, 3)));
	}

	if (infos.empty() && removed.empty() && completed.empty())
		return;

	auto payload = syncnet::CreateQuestSync(
		*builder_ptr,
		builder_ptr->CreateVector(infos),
		builder_ptr->CreateVector(removed),
		builder_ptr->CreateVector(completed));

	auto send_msg = syncnet::CreateGameMessage(
		*builder_ptr,
		syncnet::GameMessages::GameMessages_QuestSync,
		payload.Union(),
		0 /* 요청/응답 짝이 아니라 알림이므로 0 */,
		syncnet::StatusCode::StatusCode_Success);
	builder_ptr->Finish(send_msg);

	Send(builder_ptr);
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