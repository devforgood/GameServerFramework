#pragma once
#include <cstdint>
#include <string>
#include <memory>
#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include "syncnet_generated.h"
#include "SendMessage.h"
#include "GameObject.h"

class GameSession;
class GameServer;
class Character;
struct PlayerLoadData;
class Player : public GameObject, public std::enable_shared_from_this<Player>
{
private:
	long playerId_;
	std::string name_;
	int level_;
	boost::uuids::uuid uuid_;


	std::shared_ptr<Character> character_;
	std::weak_ptr<GameSession> session_;
	GameServer* server_;

	float playerLazySaveAcc_;

	// 게이트 이동 쿨타임: 마지막으로 게이트 이동에 성공한 시각(ms, epoch). 0 = 이동 이력 없음.
	// Character 는 맵 이동 때마다 재생성되므로, 이동에도 유지되는 Player 에 보관한다.
	uint64_t lastGateMoveMs_ = 0;

public:
	Player();
	~Player();

	void SetName(std::string name)
	{
		name_ = name;
	}

	void SetLevel(int level)
	{
		level_ = level;
	}
	void SetSession(std::shared_ptr<GameSession> session);
	void SetServer(GameServer* server);

	std::shared_ptr<GameSession> GetSession() { return session_.lock(); }
	GameServer* GetServer() { return server_; }

	long GetPlayerId() { return playerId_; }
	std::string GetName() { return name_; }

	// 게이트 이동 쿨타임 검사/갱신. nowMs/cooldownMs 는 ms 단위.
	bool IsGateOnCooldown(uint64_t nowMs, uint64_t cooldownMs) const
	{
		return lastGateMoveMs_ != 0 && (nowMs - lastGateMoveMs_) < cooldownMs;
	}
	void MarkGateMoved(uint64_t nowMs) { lastGateMoveMs_ = nowMs; }

	void Possess(std::shared_ptr<Character> character);
	void UnPossess();

	void Send(std::shared_ptr<send_message>& msg);
	void Close();

	std::shared_ptr<Character> & GetCharacter() { return character_; }

	bool SwitchSession(std::shared_ptr<Player> player);

	template<typename CreateFunc, typename... Args>
	void Send(
		CreateFunc createFunc,
		syncnet::GameMessages msgType,
		int32_t id,
		syncnet::StatusCode result,
		Args&&... args)
	{
		auto builder_ptr = std::make_shared<send_message>();
		auto msgOffset = createFunc(*builder_ptr, std::forward<Args>(args)...);
		auto send_msg = syncnet::CreateGameMessage(
			*builder_ptr,
			msgType,
			msgOffset.Union(),
			id,
			result
		);
		builder_ptr->Finish(send_msg);
		this->Send(builder_ptr);
	}
	
	void OnLoadedData(const PlayerLoadData & data);

	virtual void Update(float dt) override;

	std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> GetStrand();

	void SavePlayerData();
};

