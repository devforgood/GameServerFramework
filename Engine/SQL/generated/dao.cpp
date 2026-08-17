#include "dao.h"

PlayerDAO::PlayerDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerDAO::Insert(const PlayerVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player (name, level, exp) VALUES (?, ?, ?)")
        );

        stmt->setString(1, vo.name);
        stmt->setInt(2, vo.level);
        stmt->setInt64(3, vo.exp);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerDAO::Update(const PlayerVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player "
                "SET name = ?, level = ?, exp = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, vo.name);
        stmt->setInt(param_idx++, vo.level);
        stmt->setInt64(param_idx++, vo.exp);
        
        stmt->setInt64(param_idx++, vo.id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerDAO::Delete(const PlayerVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player WHERE id = ?")
        );

        stmt->setInt64(1, vo.id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerDAO::Select(long long id, PlayerVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, name, level, exp FROM player WHERE id = ?")
        );

        stmt->setInt64(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.id = res->getInt64("id");
            out_vo.name = res->getString("name");
            out_vo.level = res->getInt("level");
            out_vo.exp = res->getInt64("exp");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}



// ----------------------------------------

PlayerLocationDAO::PlayerLocationDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerLocationDAO::Insert(const PlayerLocationVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player_location (character_id, map_id, x, y, z) VALUES (?, ?, ?, ?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.map_id);
        stmt->setDouble(3, vo.x);
        stmt->setDouble(4, vo.y);
        stmt->setDouble(5, vo.z);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerLocationDAO::Update(const PlayerLocationVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player_location "
                "SET map_id = ?, x = ?, y = ?, z = ? "
                "WHERE character_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.map_id);
        stmt->setDouble(param_idx++, vo.x);
        stmt->setDouble(param_idx++, vo.y);
        stmt->setDouble(param_idx++, vo.z);
        
        stmt->setInt64(param_idx++, vo.character_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerLocationDAO::Delete(const PlayerLocationVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player_location WHERE character_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerLocationDAO::Select(long long character_id, PlayerLocationVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, map_id, x, y, z FROM player_location WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.map_id = res->getInt("map_id");
            out_vo.x = res->getDouble("x");
            out_vo.y = res->getDouble("y");
            out_vo.z = res->getDouble("z");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}



// ----------------------------------------

PlayerItemDAO::PlayerItemDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerItemDAO::Insert(const PlayerItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player_item (character_id, item_id, count) VALUES (?, ?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.item_id);
        stmt->setInt(3, vo.count);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerItemDAO::Update(const PlayerItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player_item "
                "SET count = ? "
                "WHERE character_id = ? AND item_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.count);
        
        stmt->setInt64(param_idx++, vo.character_id);
        stmt->setInt(param_idx++, vo.item_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerItemDAO::Delete(const PlayerItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player_item WHERE character_id = ? AND item_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.item_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerItemDAO::Select(long long character_id, int item_id, PlayerItemVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, item_id, count FROM player_item WHERE character_id = ? AND item_id = ?")
        );

        stmt->setInt64(1, character_id);
        stmt->setInt(2, item_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.item_id = res->getInt("item_id");
            out_vo.count = res->getInt("count");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}

std::vector<PlayerItemVO> PlayerItemDAO::SelectByIndex(long long character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, item_id, count FROM player_item WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<PlayerItemVO> results;
        while (res->next()) {
            PlayerItemVO obj;
            obj.character_id = res->getInt64("character_id");
            obj.item_id = res->getInt("item_id");
            obj.count = res->getInt("count");
            results.push_back(obj);
        }
        return results;
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return std::vector<PlayerItemVO>();
}


// ----------------------------------------

PlayerSkillDAO::PlayerSkillDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerSkillDAO::Insert(const PlayerSkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player_skill (character_id, skill_id, level) VALUES (?, ?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.skill_id);
        stmt->setInt(3, vo.level);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerSkillDAO::Update(const PlayerSkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player_skill "
                "SET level = ? "
                "WHERE character_id = ? AND skill_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.level);
        
        stmt->setInt64(param_idx++, vo.character_id);
        stmt->setInt(param_idx++, vo.skill_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerSkillDAO::Delete(const PlayerSkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player_skill WHERE character_id = ? AND skill_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.skill_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerSkillDAO::Select(long long character_id, int skill_id, PlayerSkillVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, skill_id, level FROM player_skill WHERE character_id = ? AND skill_id = ?")
        );

        stmt->setInt64(1, character_id);
        stmt->setInt(2, skill_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.skill_id = res->getInt("skill_id");
            out_vo.level = res->getInt("level");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}

std::vector<PlayerSkillVO> PlayerSkillDAO::SelectByIndex(long long character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, skill_id, level FROM player_skill WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<PlayerSkillVO> results;
        while (res->next()) {
            PlayerSkillVO obj;
            obj.character_id = res->getInt64("character_id");
            obj.skill_id = res->getInt("skill_id");
            obj.level = res->getInt("level");
            results.push_back(obj);
        }
        return results;
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return std::vector<PlayerSkillVO>();
}


// ----------------------------------------

PlayerWalletDAO::PlayerWalletDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerWalletDAO::Insert(const PlayerWalletVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player_wallet (character_id, gold) VALUES (?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt64(2, vo.gold);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerWalletDAO::Update(const PlayerWalletVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player_wallet "
                "SET gold = ? "
                "WHERE character_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt64(param_idx++, vo.gold);
        
        stmt->setInt64(param_idx++, vo.character_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerWalletDAO::Delete(const PlayerWalletVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player_wallet WHERE character_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerWalletDAO::Select(long long character_id, PlayerWalletVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, gold FROM player_wallet WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.gold = res->getInt64("gold");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}



// ----------------------------------------

QuestActiveDAO::QuestActiveDAO(sql::Connection* conn)
    : conn_(conn) {}

void QuestActiveDAO::Insert(const QuestActiveVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO quest_active (character_id, quest_id, state, stage, progress1, progress2, progress3, accept_time) VALUES (?, ?, ?, ?, ?, ?, ?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.quest_id);
        stmt->setInt(3, vo.state);
        stmt->setInt(4, vo.stage);
        stmt->setInt(5, vo.progress1);
        stmt->setInt(6, vo.progress2);
        stmt->setInt(7, vo.progress3);
        stmt->setString(8, toMySQLDateTime(vo.accept_time));

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestActiveDAO::Update(const QuestActiveVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE quest_active "
                "SET state = ?, stage = ?, progress1 = ?, progress2 = ?, progress3 = ?, accept_time = ? "
                "WHERE character_id = ? AND quest_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.state);
        stmt->setInt(param_idx++, vo.stage);
        stmt->setInt(param_idx++, vo.progress1);
        stmt->setInt(param_idx++, vo.progress2);
        stmt->setInt(param_idx++, vo.progress3);
        stmt->setString(param_idx++, toMySQLDateTime(vo.accept_time));
        
        stmt->setInt64(param_idx++, vo.character_id);
        stmt->setInt(param_idx++, vo.quest_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestActiveDAO::Delete(const QuestActiveVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM quest_active WHERE character_id = ? AND quest_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setInt(2, vo.quest_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool QuestActiveDAO::Select(long long character_id, int quest_id, QuestActiveVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, stage, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ? AND quest_id = ?")
        );

        stmt->setInt64(1, character_id);
        stmt->setInt(2, quest_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.quest_id = res->getInt("quest_id");
            out_vo.state = res->getInt("state");
            out_vo.stage = res->getInt("stage");
            out_vo.progress1 = res->getInt("progress1");
            out_vo.progress2 = res->getInt("progress2");
            out_vo.progress3 = res->getInt("progress3");
            out_vo.accept_time = fromMySQLDateTime(res->getString("accept_time"));
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}

std::vector<QuestActiveVO> QuestActiveDAO::SelectByIndex(long long character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, stage, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<QuestActiveVO> results;
        while (res->next()) {
            QuestActiveVO obj;
            obj.character_id = res->getInt64("character_id");
            obj.quest_id = res->getInt("quest_id");
            obj.state = res->getInt("state");
            obj.stage = res->getInt("stage");
            obj.progress1 = res->getInt("progress1");
            obj.progress2 = res->getInt("progress2");
            obj.progress3 = res->getInt("progress3");
            obj.accept_time = fromMySQLDateTime(res->getString("accept_time"));
            results.push_back(obj);
        }
        return results;
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return std::vector<QuestActiveVO>();
}


// ----------------------------------------

QuestStateDAO::QuestStateDAO(sql::Connection* conn)
    : conn_(conn) {}

void QuestStateDAO::Insert(const QuestStateVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO quest_state (character_id, flags) VALUES (?, ?)")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->setString(2, vo.flags);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestStateDAO::Update(const QuestStateVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE quest_state "
                "SET flags = ? "
                "WHERE character_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, vo.flags);
        
        stmt->setInt64(param_idx++, vo.character_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestStateDAO::Delete(const QuestStateVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM quest_state WHERE character_id = ?")
        );

        stmt->setInt64(1, vo.character_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool QuestStateDAO::Select(long long character_id, QuestStateVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, flags FROM quest_state WHERE character_id = ?")
        );

        stmt->setInt64(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt64("character_id");
            out_vo.flags = res->getString("flags");
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}



// ----------------------------------------

SessionTokenDAO::SessionTokenDAO(sql::Connection* conn)
    : conn_(conn) {}

void SessionTokenDAO::Insert(const SessionTokenVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO session_token (token, user_id, player_id, issued_at) VALUES (?, ?, ?, ?)")
        );

        stmt->setString(1, vo.token);
        stmt->setString(2, vo.user_id);
        stmt->setInt64(3, vo.player_id);
        stmt->setString(4, toMySQLDateTime(vo.issued_at));

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void SessionTokenDAO::Update(const SessionTokenVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE session_token "
                "SET user_id = ?, player_id = ?, issued_at = ? "
                "WHERE token = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, vo.user_id);
        stmt->setInt64(param_idx++, vo.player_id);
        stmt->setString(param_idx++, toMySQLDateTime(vo.issued_at));
        
        stmt->setString(param_idx++, vo.token);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void SessionTokenDAO::Delete(const SessionTokenVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM session_token WHERE token = ?")
        );

        stmt->setString(1, vo.token);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool SessionTokenDAO::Select(std::string token, SessionTokenVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT token, user_id, player_id, issued_at FROM session_token WHERE token = ?")
        );

        stmt->setString(1, token);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.token = res->getString("token");
            out_vo.user_id = res->getString("user_id");
            out_vo.player_id = res->getInt64("player_id");
            out_vo.issued_at = fromMySQLDateTime(res->getString("issued_at"));
        } else {
            return false;
        }
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return true;
}

std::vector<SessionTokenVO> SessionTokenDAO::SelectByIndex(std::string user_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT token, user_id, player_id, issued_at FROM session_token WHERE user_id = ?")
        );

        stmt->setString(1, user_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<SessionTokenVO> results;
        while (res->next()) {
            SessionTokenVO obj;
            obj.token = res->getString("token");
            obj.user_id = res->getString("user_id");
            obj.player_id = res->getInt64("player_id");
            obj.issued_at = fromMySQLDateTime(res->getString("issued_at"));
            results.push_back(obj);
        }
        return results;
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
    return std::vector<SessionTokenVO>();
}
