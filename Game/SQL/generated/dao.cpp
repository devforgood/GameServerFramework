#include "dao.h"

PlayerDAO::PlayerDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerDAO::Insert(const PlayerVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player (name, level) VALUES (?, ?)")
        );

        stmt->setString(1, vo.name);
        stmt->setInt(2, vo.level);

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
                "SET name = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, vo.name);
        stmt->setInt(param_idx++, vo.level);
        
        stmt->setInt(param_idx++, vo.id);

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

        stmt->setInt(1, vo.id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerDAO::Select(int id, PlayerVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, name, level FROM player WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.id = res->getInt("id");
            out_vo.name = res->getString("name");
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



// ----------------------------------------

ItemDAO::ItemDAO(sql::Connection* conn)
    : conn_(conn) {}

void ItemDAO::Insert(const ItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO item (player_id, level) VALUES (?, ?)")
        );

        stmt->setInt(1, vo.player_id);
        stmt->setInt(2, vo.level);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void ItemDAO::Update(const ItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE item "
                "SET player_id = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.player_id);
        stmt->setInt(param_idx++, vo.level);
        
        stmt->setInt(param_idx++, vo.id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void ItemDAO::Delete(const ItemVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM item WHERE id = ?")
        );

        stmt->setInt(1, vo.id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool ItemDAO::Select(int id, ItemVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, level FROM item WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.id = res->getInt("id");
            out_vo.player_id = res->getInt("player_id");
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

std::vector<ItemVO> ItemDAO::SelectByIndex(int player_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, level FROM item WHERE player_id = ?")
        );

        stmt->setInt(1, player_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<ItemVO> results;
        while (res->next()) {
            ItemVO obj;
            obj.id = res->getInt("id");
            obj.player_id = res->getInt("player_id");
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
    return std::vector<ItemVO>();
}


// ----------------------------------------

SkillDAO::SkillDAO(sql::Connection* conn)
    : conn_(conn) {}

void SkillDAO::Insert(const SkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO skill (player_id, skill_id, level) VALUES (?, ?, ?)")
        );

        stmt->setInt(1, vo.player_id);
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

void SkillDAO::Update(const SkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE skill "
                "SET player_id = ?, skill_id = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.player_id);
        stmt->setInt(param_idx++, vo.skill_id);
        stmt->setInt(param_idx++, vo.level);
        
        stmt->setInt(param_idx++, vo.id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void SkillDAO::Delete(const SkillVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM skill WHERE id = ?")
        );

        stmt->setInt(1, vo.id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool SkillDAO::Select(int id, SkillVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, skill_id, level FROM skill WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.id = res->getInt("id");
            out_vo.player_id = res->getInt("player_id");
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

std::vector<SkillVO> SkillDAO::SelectByIndex(int player_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, skill_id, level FROM skill WHERE player_id = ?")
        );

        stmt->setInt(1, player_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<SkillVO> results;
        while (res->next()) {
            SkillVO obj;
            obj.id = res->getInt("id");
            obj.player_id = res->getInt("player_id");
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
    return std::vector<SkillVO>();
}


// ----------------------------------------

QuestActiveDAO::QuestActiveDAO(sql::Connection* conn)
    : conn_(conn) {}

void QuestActiveDAO::Insert(const QuestActiveVO& vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO quest_active (character_id, quest_id, state, progress1, progress2, progress3, accept_time) VALUES (?, ?, ?, ?, ?, ?, ?)")
        );

        stmt->setInt(1, vo.character_id);
        stmt->setInt(2, vo.quest_id);
        stmt->setInt(3, vo.state);
        stmt->setInt(4, vo.progress1);
        stmt->setInt(5, vo.progress2);
        stmt->setInt(6, vo.progress3);
        stmt->setString(7, vo.accept_time);

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
                "SET state = ?, progress1 = ?, progress2 = ?, progress3 = ?, accept_time = ? "
                "WHERE character_id = ? AND quest_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, vo.state);
        stmt->setInt(param_idx++, vo.progress1);
        stmt->setInt(param_idx++, vo.progress2);
        stmt->setInt(param_idx++, vo.progress3);
        stmt->setString(param_idx++, vo.accept_time);
        
        stmt->setInt(param_idx++, vo.character_id);
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

        stmt->setInt(1, vo.character_id);
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

bool QuestActiveDAO::Select(int character_id, int quest_id, QuestActiveVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ? AND quest_id = ?")
        );

        stmt->setInt(1, character_id);
        stmt->setInt(2, quest_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt("character_id");
            out_vo.quest_id = res->getInt("quest_id");
            out_vo.state = res->getInt("state");
            out_vo.progress1 = res->getInt("progress1");
            out_vo.progress2 = res->getInt("progress2");
            out_vo.progress3 = res->getInt("progress3");
            out_vo.accept_time = res->getString("accept_time");
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

std::vector<QuestActiveVO> QuestActiveDAO::SelectByIndex(int character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ?")
        );

        stmt->setInt(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<QuestActiveVO> results;
        while (res->next()) {
            QuestActiveVO obj;
            obj.character_id = res->getInt("character_id");
            obj.quest_id = res->getInt("quest_id");
            obj.state = res->getInt("state");
            obj.progress1 = res->getInt("progress1");
            obj.progress2 = res->getInt("progress2");
            obj.progress3 = res->getInt("progress3");
            obj.accept_time = res->getString("accept_time");
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

        stmt->setInt(1, vo.character_id);
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
        
        stmt->setInt(param_idx++, vo.character_id);

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

        stmt->setInt(1, vo.character_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool QuestStateDAO::Select(int character_id, QuestStateVO& out_vo) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, flags FROM quest_state WHERE character_id = ?")
        );

        stmt->setInt(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            out_vo.character_id = res->getInt("character_id");
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

