#include "dao.h"

PlayerDAO::PlayerDAO(sql::Connection* conn)
    : conn_(conn) {}

void PlayerDAO::Insert(std::string name, int level) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("INSERT INTO player (name, level) VALUES (?, ?)")
    );

    stmt->setString(1, name);
    stmt->setInt(2, level);

    stmt->execute();
}

void PlayerDAO::Update(int id, std::string name, int level) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("UPDATE player SET name = ?, level = ? WHERE id = ?")
    );

    stmt->setString(1, name);
    stmt->setInt(2, level);
    stmt->setInt(3, id);

    stmt->execute();
}

void PlayerDAO::Delete(int id) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("DELETE FROM player WHERE id = ?")
    );

    stmt->setInt(1, id);
    stmt->execute();
}

// ----------------------------------------

ItemDAO::ItemDAO(sql::Connection* conn)
    : conn_(conn) {}

void ItemDAO::Insert(std::string name, int level) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("INSERT INTO item (name, level) VALUES (?, ?)")
    );

    stmt->setString(1, name);
    stmt->setInt(2, level);

    stmt->execute();
}

void ItemDAO::Update(int id, std::string name, int level) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("UPDATE item SET name = ?, level = ? WHERE id = ?")
    );

    stmt->setString(1, name);
    stmt->setInt(2, level);
    stmt->setInt(3, id);

    stmt->execute();
}

void ItemDAO::Delete(int id) {
    std::unique_ptr<sql::PreparedStatement> stmt(
        conn_->prepareStatement("DELETE FROM item WHERE id = ?")
    );

    stmt->setInt(1, id);
    stmt->execute();
}