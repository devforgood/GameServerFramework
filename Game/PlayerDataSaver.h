#pragma once
#include <memory>

class Player;
struct PlayerSaveData;

class PlayerDataSaver
{
public:
	static void AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerSaveData> data);
};

