#pragma once
#include <string>
#include <memory>

class game_session;
class game_server;
class send_message;
class Character;
class Player : public std::enable_shared_from_this<Player>
{
private:
	long player_id_;
	std::string name_;
	int level_;

	std::shared_ptr<Character> character_;
	std::weak_ptr<game_session> session_;
	game_server* server_;

public:
	Player()
	{
	}

	~Player() {}


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

	long player_id() { return player_id_; }
	std::string name() { return name_; }

	void possess(std::shared_ptr<Character> character);

	void async_db_query();
	void send(std::shared_ptr<send_message> msg);
};

