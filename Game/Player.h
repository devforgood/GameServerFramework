#pragma once
#include <string>
#include <memory>
#include <boost/uuid/uuid.hpp>
#include <boost/uuid/uuid_generators.hpp>
#include "syncnet_generated.h"
#include "SendMessage.h"
#include "GameObject.h"

class game_session;
class game_server;
class Character;
struct PlayerData;
class Player : public GameObject, public std::enable_shared_from_this<Player>
{
private:
	long player_id_;
	std::string name_;
	int level_;
	boost::uuids::uuid uuid_;


	std::shared_ptr<Character> character_;
	std::weak_ptr<game_session> session_;
	game_server* server_;

	float playerLazySaveAcc_;

public:
	Player();
	~Player();

	void set_name(std::string name)
	{
		name_ = name;
	}

	void set_level(int level)
	{
		level_ = level;
	}
	void set_session(std::shared_ptr<game_session> session);
	void set_server(game_server* server);

	std::shared_ptr<game_session> get_session() { return session_.lock(); }
	game_server* get_server() { return server_; }

	long player_id() { return player_id_; }
	std::string name() { return name_; }

	void possess(std::shared_ptr<Character> character);

	void send(std::shared_ptr<send_message>& msg);
	void close();

	std::shared_ptr<Character> & character() { return character_; }

	bool switch_session(std::shared_ptr<Player> player);

	template<typename CreateFunc, typename... Args>
	void send(
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
		this->send(builder_ptr);
	}
	
	void on_loaded_data(PlayerData data);

	virtual void update(float dt) override;

	std::optional<boost::asio::strand<boost::asio::thread_pool::executor_type>> get_strand();

	void save_player_data();
};

