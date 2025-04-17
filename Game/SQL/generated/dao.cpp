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
            conn_->prepareStatement("UPDATE player SET name = ?, level = ? WHERE id = ?")
        );

        stmt->setString(1, name);
        stmt->setInt(2, level);
        stmt->setInt(3, id);

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
            conn_->prepareStatement("UPDATE item SET player_id = ?, level = ? WHERE id = ?")
        );

        stmt->setInt(1, player_id);
        stmt->setInt(2, level);
        stmt->setInt(3, id);

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
