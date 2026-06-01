CREATE TABLE IF NOT EXISTS player (
    id BIGINT AUTO_INCREMENT NOT NULL,    name VARCHAR(50) NOT NULL,    level INT DEFAULT 1,    UNIQUE KEY (name),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS item (
    id BIGINT AUTO_INCREMENT NOT NULL,    player_id BIGINT NOT NULL,    level INT DEFAULT 1,    INDEX (player_id),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS skill (
    id BIGINT AUTO_INCREMENT NOT NULL,    player_id BIGINT NOT NULL,    skill_id BIGINT NOT NULL,    level INT DEFAULT 1,    UNIQUE KEY (player_id, skill_id),    INDEX (player_id),    PRIMARY KEY (id)
);

CREATE TABLE IF NOT EXISTS quest (
    id BIGINT AUTO_INCREMENT NOT NULL,    player_id BIGINT NOT NULL,    quest_id BIGINT NOT NULL,    state INT DEFAULT 0,    objective_count INT DEFAULT 0,    UNIQUE KEY (player_id, quest_id),    INDEX (player_id),    PRIMARY KEY (id)
);