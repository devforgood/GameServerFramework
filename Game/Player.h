#pragma once

class Player
{
private:
	std::string name_;
	int level_;
	int agent_id_;

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

	std::string name() { return name_; }
	int agent_id() { return agent_id_; }

	void possess(int agent_id)
	{
		agent_id_ = agent_id;
	}
};

