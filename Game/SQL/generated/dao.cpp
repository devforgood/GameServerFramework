#include "dao.h"

PlayerDAO::PlayerDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerDAO::Insert() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO player (name, level) VALUES (?, ?)")
        );

        stmt->setString(1, name);
        stmt->setInt(2, level);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerDAO::Update() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE player "
                "SET name = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, name);
        stmt->setInt(param_idx++, level);
        
        stmt->setInt(param_idx++, id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void PlayerDAO::Delete() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM player WHERE id = ?")
        );

        stmt->setInt(1, id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool PlayerDAO::Select(int id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, name, level FROM player WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            this->id = res->getInt("id");
            this->name = res->getString("name");
            this->level = res->getInt("level");
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

void ItemDAO::Insert() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO item (player_id, level) VALUES (?, ?)")
        );

        stmt->setInt(1, player_id);
        stmt->setInt(2, level);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void ItemDAO::Update() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE item "
                "SET player_id = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, player_id);
        stmt->setInt(param_idx++, level);
        
        stmt->setInt(param_idx++, id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void ItemDAO::Delete() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM item WHERE id = ?")
        );

        stmt->setInt(1, id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool ItemDAO::Select(int id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, level FROM item WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            this->id = res->getInt("id");
            this->player_id = res->getInt("player_id");
            this->level = res->getInt("level");
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

std::vector<ItemDAO> ItemDAO::SelectByIndex(int player_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, level FROM item WHERE player_id = ?")
        );

        stmt->setInt(1, player_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<ItemDAO> results;
        while (res->next()) {
            ItemDAO obj(conn_);
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
    return std::vector<ItemDAO>();
}


// ----------------------------------------

SkillDAO::SkillDAO(sql::Connection* conn)
    : conn_(conn) {}

void SkillDAO::Insert() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO skill (player_id, skill_id, level) VALUES (?, ?, ?)")
        );

        stmt->setInt(1, player_id);
        stmt->setInt(2, skill_id);
        stmt->setInt(3, level);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void SkillDAO::Update() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE skill "
                "SET player_id = ?, skill_id = ?, level = ? "
                "WHERE id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, player_id);
        stmt->setInt(param_idx++, skill_id);
        stmt->setInt(param_idx++, level);
        
        stmt->setInt(param_idx++, id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void SkillDAO::Delete() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM skill WHERE id = ?")
        );

        stmt->setInt(1, id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool SkillDAO::Select(int id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, skill_id, level FROM skill WHERE id = ?")
        );

        stmt->setInt(1, id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            this->id = res->getInt("id");
            this->player_id = res->getInt("player_id");
            this->skill_id = res->getInt("skill_id");
            this->level = res->getInt("level");
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

std::vector<SkillDAO> SkillDAO::SelectByIndex(int player_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT id, player_id, skill_id, level FROM skill WHERE player_id = ?")
        );

        stmt->setInt(1, player_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<SkillDAO> results;
        while (res->next()) {
            SkillDAO obj(conn_);
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
    return std::vector<SkillDAO>();
}


// ----------------------------------------

QuestActiveDAO::QuestActiveDAO(sql::Connection* conn)
    : conn_(conn) {}

void QuestActiveDAO::Insert() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO quest_active (character_id, quest_id, state, progress1, progress2, progress3, accept_time) VALUES (?, ?, ?, ?, ?, ?, ?)")
        );

        stmt->setInt(1, character_id);
        stmt->setInt(2, quest_id);
        stmt->setInt(3, state);
        stmt->setInt(4, progress1);
        stmt->setInt(5, progress2);
        stmt->setInt(6, progress3);
        stmt->setString(7, accept_time);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestActiveDAO::Update() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE quest_active "
                "SET state = ?, progress1 = ?, progress2 = ?, progress3 = ?, accept_time = ? "
                "WHERE character_id = ? AND quest_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setInt(param_idx++, state);
        stmt->setInt(param_idx++, progress1);
        stmt->setInt(param_idx++, progress2);
        stmt->setInt(param_idx++, progress3);
        stmt->setString(param_idx++, accept_time);
        
        stmt->setInt(param_idx++, character_id);
        stmt->setInt(param_idx++, quest_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestActiveDAO::Delete() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM quest_active WHERE character_id = ? AND quest_id = ?")
        );

        stmt->setInt(1, character_id);
        stmt->setInt(2, quest_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool QuestActiveDAO::Select(int character_id, int quest_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ? AND quest_id = ?")
        );

        stmt->setInt(1, character_id);
        stmt->setInt(2, quest_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            this->character_id = res->getInt("character_id");
            this->quest_id = res->getInt("quest_id");
            this->state = res->getInt("state");
            this->progress1 = res->getInt("progress1");
            this->progress2 = res->getInt("progress2");
            this->progress3 = res->getInt("progress3");
            this->accept_time = res->getString("accept_time");
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

std::vector<QuestActiveDAO> QuestActiveDAO::SelectByIndex(int character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, quest_id, state, progress1, progress2, progress3, accept_time FROM quest_active WHERE character_id = ?")
        );

        stmt->setInt(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        std::vector<QuestActiveDAO> results;
        while (res->next()) {
            QuestActiveDAO obj(conn_);
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
    return std::vector<QuestActiveDAO>();
}


// ----------------------------------------

QuestStateDAO::QuestStateDAO(sql::Connection* conn)
    : conn_(conn) {}

void QuestStateDAO::Insert() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("INSERT INTO quest_state (character_id, flags) VALUES (?, ?)")
        );

        stmt->setInt(1, character_id);
        stmt->setString(2, flags);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestStateDAO::Update() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement(
                "UPDATE quest_state "
                "SET flags = ? "
                "WHERE character_id = ?"
            )
        );

        int param_idx = 1;
        stmt->setString(param_idx++, flags);
        
        stmt->setInt(param_idx++, character_id);

        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

void QuestStateDAO::Delete() {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("DELETE FROM quest_state WHERE character_id = ?")
        );

        stmt->setInt(1, character_id);
        stmt->execute();
    }
    catch (const sql::SQLException& e) {
        throw std::runtime_error(std::string("SQL error: ") + e.what());
    }
    catch (const std::exception& e) {
        throw std::runtime_error(std::string("error: ") + e.what());
    }
}

bool QuestStateDAO::Select(int character_id) {
    try {
        std::unique_ptr<sql::PreparedStatement> stmt(
            conn_->prepareStatement("SELECT character_id, flags FROM quest_state WHERE character_id = ?")
        );

        stmt->setInt(1, character_id);

        std::unique_ptr<sql::ResultSet> res(stmt->executeQuery());
        if (res->next()) {
            this->character_id = res->getInt("character_id");
            this->flags = res->getString("flags");
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

