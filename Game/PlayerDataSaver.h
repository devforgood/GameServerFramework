#pragma once
#include <memory>

class Player;
struct PlayerData;

class PlayerDataSaver
{
public:
	static void AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerData> data);
};

