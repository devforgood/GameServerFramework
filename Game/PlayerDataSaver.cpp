#include "PlayerDataSaver.h"
#include "Player.h"
#include "Server.h"
#include "SqlClient.h"
#include "SqlClientManager.h"
#include "PlayerSaveData.h"
#include <iostream>


void PlayerDataSaver::AsyncSave(std::shared_ptr<Player> player, std::shared_ptr<PlayerSaveData> data)
{

    boost::asio::post(player->GetStrand().value(), [data]() {

        // TODO
		// DB에 저장하는 로직
        // 저장 방식 (insert, update, delete) 타입에 따라 쿼리 선택
		// 대상 디비 테이블과 매핑되는 PlayerData의 필드에 따라 쿼리 작성


        
        });
}
