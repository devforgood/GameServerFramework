#pragma once

// 이 파일은 SqlCodeGenerator 가 자동 생성한다. 직접 수정하지 마세요.
// 고칠 곳: SqlCodeGenerator/schema.xml + templates/schema_columns.h.j2
// 다시 만들기: SqlCodeGenerator/build.bat (또는 python generate.py)
//
// CREATE TABLE IF NOT EXISTS 는 이미 있는 테이블에 아무것도 하지 않는다. 스키마에 컬럼이
// 추가되면 기존 DB 는 그대로 남아, 서버가 없는 컬럼을 읽으려다 죽는다. 기동 시 이 목록과
// 실제 컬럼(information_schema)을 대조해 빠진 것만 ADD COLUMN 한다.
//
// SQL 의 ADD COLUMN IF NOT EXISTS 를 쓰지 않는 이유: MariaDB 전용 문법이라 MySQL 서버에서는
// 문장 전체가 문법 오류로 거부된다 — 마이그레이션이 조용히 아무것도 하지 않게 된다.
//
// AUTO_INCREMENT 컬럼은 목록에 없다. 기존 테이블에 나중에 붙일 수 있는 종류가 아니다
// (키여야 하고, 테이블당 하나뿐이다).

struct SchemaColumn
{
	const char* table;
	const char* column;
	const char* definition; // 타입 + NOT NULL / DEFAULT
	const char* after;      // 바로 앞 컬럼. 첫 컬럼이면 nullptr
};

inline constexpr SchemaColumn kSchemaColumns[] =
{
	{ "player", "name", "VARCHAR(50) NOT NULL", "id" },
	{ "player", "level", "INT DEFAULT 1", "name" },
	{ "player", "exp", "BIGINT NOT NULL DEFAULT 0", "level" },
	{ "player_location", "character_id", "BIGINT NOT NULL", nullptr },
	{ "player_location", "map_id", "INT NOT NULL DEFAULT 0", "character_id" },
	{ "player_location", "x", "DOUBLE NOT NULL DEFAULT 0", "map_id" },
	{ "player_location", "y", "DOUBLE NOT NULL DEFAULT 0", "x" },
	{ "player_location", "z", "DOUBLE NOT NULL DEFAULT 0", "y" },
	{ "player_item", "character_id", "BIGINT NOT NULL", nullptr },
	{ "player_item", "item_id", "INT NOT NULL", "character_id" },
	{ "player_item", "count", "INT NOT NULL DEFAULT 0", "item_id" },
	{ "player_skill", "character_id", "BIGINT NOT NULL", nullptr },
	{ "player_skill", "skill_id", "INT NOT NULL", "character_id" },
	{ "player_skill", "level", "INT NOT NULL DEFAULT 1", "skill_id" },
	{ "player_wallet", "character_id", "BIGINT NOT NULL", nullptr },
	{ "player_wallet", "gold", "BIGINT NOT NULL DEFAULT 0", "character_id" },
	{ "quest_active", "character_id", "BIGINT NOT NULL", nullptr },
	{ "quest_active", "quest_id", "INT NOT NULL", "character_id" },
	{ "quest_active", "state", "TINYINT NOT NULL", "quest_id" },
	{ "quest_active", "stage", "TINYINT NOT NULL DEFAULT 1", "state" },
	{ "quest_active", "progress1", "INT NOT NULL DEFAULT 0", "stage" },
	{ "quest_active", "progress2", "INT NOT NULL DEFAULT 0", "progress1" },
	{ "quest_active", "progress3", "INT NOT NULL DEFAULT 0", "progress2" },
	{ "quest_active", "accept_time", "DATETIME NOT NULL", "progress3" },
	{ "quest_state", "character_id", "BIGINT NOT NULL", nullptr },
	{ "quest_state", "flags", "BLOB NOT NULL", "character_id" },
	{ "session_token", "token", "VARCHAR(128) NOT NULL", nullptr },
	{ "session_token", "user_id", "VARCHAR(64) NOT NULL", "token" },
	{ "session_token", "player_id", "BIGINT NOT NULL DEFAULT 0", "user_id" },
	{ "session_token", "issued_at", "DATETIME NOT NULL", "player_id" },
};
