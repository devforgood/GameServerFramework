#include "Player.h"
#include "Server.h"
#include <atomic>
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
#include "SkillSet.h"
#include <vector>
#include "PlayerLevel.h"
#include "PlayerWallet.h"
#include "PlayerLocation.h"
#include "PlayerLoot.h"
#include "PlayerParty.h"
#include "PlayerDialog.h"
#include "PlayerSender.h"
#include "Map.h"
#include "PlayerSaveData.h"
#include "PlayerEventBroker.h"
#include "PlayerEventBrokerProxy.h"

// todo :  Player ID 디비에서 관리 개선 필요
//
// 월드 스레드마다 플레이어가 만들어지므로 원자적으로 올린다. 그냥 ++ 로 두면 두 스레드가
// 같은 번호를 받아 갈 수 있고, 이 번호는 파티 색인의 키다 — 겹치면 남의 파티에 붙는다.
static std::atomic<long> next_player_id{ 1 };

Player::Player()
{
	uuid_ = boost::uuids::random_generator()();
	playerId_ = next_player_id.fetch_add(1, std::memory_order_relaxed);

	playerLazySaveAcc_ = 0.0f;

	this->AddComponent<PlayerEventBroker>();
	this->AddComponent<PlayerQuest>();
	this->AddComponent<PlayerItem>();
	this->AddComponent<PlayerSkill>();
	this->AddComponent<PlayerLevel>();
	this->AddComponent<PlayerWallet>();
	this->AddComponent<PlayerLocation>();
	this->AddComponent<PlayerLoot>();

	// 파티는 캐릭터가 아니라 플레이어 단위다(맵을 옮겨도 유지된다). 다른 파티원이 이
	// 플레이어를 찾을 수 있도록 여기서 바로 등록한다 — 로그인/로드를 기다리면 그 사이에
	// 온 초대가 갈 곳을 잃는다.
	this->AddComponent<PlayerParty>()->Bind(playerId_);

	// 대화는 순수하게 플레이어 상태다(지금 어느 노드를 보고 있는가). 캐릭터가 없어도
	// 붙어 있어야 로그인 직후 상호작용이 바로 통한다.
	this->AddComponent<PlayerDialog>();

	// 컴포넌트가 클라로 메시지를 내보내는 통로. 넘기는 것은 "보낸다"는 동작 하나뿐이라
	// 컴포넌트가 이걸 통해 Player 의 다른 기능에 손댈 수 없다. 이 컴포넌트는 Player 가
	// 소유하므로 this 를 캡처해도 안전하다(Player 보다 오래 살 수 없다).
	this->AddComponent<PlayerSender>()->Bind(
		[this](std::shared_ptr<send_message>& msg) { this->Send(msg); });
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

	// 레벨에 맞는 전투 스탯을 새 캐릭터에 싣는다. 이걸 빠뜨리면 캐릭터가 항상
	// Actor 기본값(체력 100/공격 10)으로 남아 레벨이 전투에 아무 영향을 주지 않는다.
	// 맵 이동은 UnPossess -> Possess 로 캐릭터를 다시 만들므로 여기서 매번 다시 적용된다.
	if (auto* level = GetComponent<PlayerLevel>())
		level->ApplyStatsTo(character_.get(), /*resetHealth=*/true);

	// 배운 스킬만 캐릭터에 싣는다(서버 권위). 목록에 없는 스킬은 TryCast 가 SkillNotFound.
	ApplyOwnedSkillsToCharacter();

	// 파티 로스터에 실을 내 캐릭터 위치. 컴포넌트 층에서는 캐릭터를 볼 수 없으므로
	// 여기서 알려 준다. 맵 이동은 UnPossess -> Possess 로 캐릭터를 다시 만들므로
	// 이 두 지점만 챙기면 값이 어긋나지 않는다.
	if (auto* party = GetComponent<PlayerParty>())
	{
		party->SetLocation(character_->GetActorId(),
			character_->GetMap() != nullptr ? character_->GetMap()->GetMapId() : 0);
	}
}

void Player::ApplyOwnedSkillsToCharacter()
{
	if (character_ == nullptr)
		return;

	auto* skills = GetComponent<PlayerSkill>();
	if (skills == nullptr)
		return;

	// 신규 캐릭터에게 기본 스킬을 지급한다(skill.json 의 starter=true).
	// 이게 없으면 첫 접속 캐릭터가 평타조차 쓸 수 없다.
	if (skills->Count() == 0)
	{
		for (int starterId : SkillSet::CollectStarterSkillIds())
			skills->LearnSkill(starterId);
	}

	std::vector<int> owned;
	skills->CollectSkillIds(owned);
	character_->GetSkillSet().InitFromOwned(owned);
}

void Player::UnPossess()
{
	// 게이트 이동 등으로 캐릭터를 다른 맵에 재생성하기 전, 기존 빙의를 해제한다.
	// character_ 의 마지막 참조가 사라지면 이전 맵에서 제거된 Character 가 파괴된다.
	character_ = nullptr;

	if (auto* party = GetComponent<PlayerParty>())
		party->SetLocation(0, 0);
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

	Load(data);

	// 접속하지 않은 사이에 지난 시간을 정산한다(제한 시간 만료, 일일 리셋 등).
	if (auto* broker = GetComponent<PlayerEventBroker>())
		broker->publish(EventPlayerJoined{ static_cast<int>(playerId_) });

	// 로그인 직후에는 진행 중인 퀘스트 전체를 클라에 내려준다(다음 Update 에서 한 통으로).
	if (auto* quests = GetComponent<PlayerQuest>())
		quests->MarkAllForSync();

	// 레벨/경험치도 알려 준다. 클라는 이것으로 게이트의 required_level 을 밟기 전에 본다.
	if (auto* level = GetComponent<PlayerLevel>())
		level->SendToClient();
}

void Player::Update(float dt)
{
	// 컴포넌트가 각자 자기 변경분을 클라에 내보낸다(PlayerQuest/PlayerParty 의 Update).
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
	// 저장 직전에 현재 위치를 찍는다. 컴포넌트는 캐릭터를 모르므로 여기서 넘겨준다.
	// (캐릭터가 없으면 마지막으로 기록된 값이 그대로 남는다 — 사망/로그아웃 후에도 유효)
	if (auto* location = GetComponent<PlayerLocation>())
	{
		if (character_ != nullptr && character_->GetMap() != nullptr)
		{
			const Vector3& pos = character_->GetPosition();
			location->Remember(character_->GetMap()->GetMapId(), pos.x, pos.y, pos.z);
		}
	}

	// 저장 버퍼는 새로 할당하지 않고 플레이어마다 가진 풀에서 빌려 쓴다.
	// 셋 다 DB 에 물려 있으면 이번 주기는 건너뛴다 — 담을 곳이 없는데 Save()
	// 를 부르면 컴포넌트가 dirty 를 지우면서 변경분을 흘려버린다.
	auto save_data = saveBuffers_.Acquire();
	if (save_data == nullptr)
	{
		LOG.warn("Player {} 저장 버퍼가 모두 사용 중이라 이번 저장을 건너뜁니다", playerId_);
		return;
	}

	// 각 컴포넌트에서 플레이어 데이터를 수집
	Save(save_data.get());

	// 변경된 데이터만 비동기로 전달
	PlayerRepository::AsyncSave(shared_from_this(), std::move(save_data));
}