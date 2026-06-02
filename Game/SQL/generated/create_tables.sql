CREATE TABLE IF NOT EXISTS player (
    id BIGINT AUTO_INCREMENT NOT NULL,    name VARCHAR(50) NOT NULL,    level INT DEFAULT 1,    UNIQUE KEY (name),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS item (
    id BIGINT AUTO_INCREMENT NOT NULL,    player_id BIGINT NOT NULL,    level INT DEFAULT 1,    INDEX (player_id),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS skill (
    id BIGINT AUTO_INCREMENT NOT NULL,    player_id BIGINT NOT NULL,    skill_id BIGINT NOT NULL,    level INT DEFAULT 1,    UNIQUE KEY (player_id, skill_id),    INDEX (player_id),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS quest_active (
    character_id BIGINT NOT NULL,    quest_id INT NOT NULL,    state TINYINT NOT NULL,    progress1 INT NOT NULL DEFAULT 0,    progress2 INT NOT NULL DEFAULT 0,    progress3 INT NOT NULL DEFAULT 0,    accept_time DATETIME NOT NULL,    INDEX (character_id)    PRIMARY KEY ()
);

CREATE TABLE IF NOT EXISTS quest_state (
    character_id BIGINT NOT NULL,    flags BLOB NOT NULL,    PRIMARY KEY (character_id)
);