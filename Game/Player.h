#pragma once
#include <string>
#include <memory>

class game_session;
class game_server;
class Player
{
private:
	std::string name_;
	int level_;
	int agent_id_;

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

	std::string name() { return name_; }
	int agent_id() { return agent_id_; }

	void possess(int agent_id)
	{
		agent_id_ = agent_id;
	}

	void async_db_query();
};

