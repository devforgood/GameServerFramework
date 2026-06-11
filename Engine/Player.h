#pragma once
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

	void Possess(std::shared_ptr<Character> character);

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

	virtual void update(float dt) override;

	std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> GetStrand();

	void SavePlayerData();
};

