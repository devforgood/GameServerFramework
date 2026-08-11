CREATE TABLE IF NOT EXISTS `player`
(
    `id`                 BIGINT          NOT NULL AUTO_INCREMENT,
    `name`               VARCHAR(50)     NOT NULL,
    `level`              INT             DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY (`name`)
);

CREATE TABLE IF NOT EXISTS `item`
(
    `id`                 BIGINT          NOT NULL AUTO_INCREMENT,
    `player_id`          BIGINT          NOT NULL,
    `level`              INT             DEFAULT 1,
    PRIMARY KEY (`id`),
    INDEX (`player_id`)
);

CREATE TABLE IF NOT EXISTS `skill`
(
    `id`                 BIGINT          NOT NULL AUTO_INCREMENT,
    `player_id`          BIGINT          NOT NULL,
    `skill_id`           BIGINT          NOT NULL,
    `level`              INT             DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY (`player_id`, `skill_id`),
    INDEX (`player_id`)
);

CREATE TABLE IF NOT EXISTS `quest_active`
(
    `character_id`       BIGINT          NOT NULL,
    `quest_id`           INT             NOT NULL,
    `state`              TINYINT         NOT NULL,
    `stage`              TINYINT         NOT NULL DEFAULT 1,
    `progress1`          INT             NOT NULL DEFAULT 0,
    `progress2`          INT             NOT NULL DEFAULT 0,
    `progress3`          INT             NOT NULL DEFAULT 0,
    `accept_time`        DATETIME        NOT NULL,
    PRIMARY KEY (`character_id`, `quest_id`),
    INDEX (`character_id`)
);

CREATE TABLE IF NOT EXISTS `quest_state`
(
    `character_id`       BIGINT          NOT NULL,
    `flags`              BLOB            NOT NULL,
    PRIMARY KEY (`character_id`)
);