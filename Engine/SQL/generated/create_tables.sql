CREATE TABLE IF NOT EXISTS `player`
(
    `id`                 BIGINT          NOT NULL AUTO_INCREMENT,
    `name`               VARCHAR(50)     NOT NULL,
    `level`              INT             DEFAULT 1,
    PRIMARY KEY (`id`),
    UNIQUE KEY (`name`)
);

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `player` ADD COLUMN IF NOT EXISTS `name`               VARCHAR(50)     NOT NULL AFTER `id`;

ALTER TABLE `player` ADD COLUMN IF NOT EXISTS `level`              INT             DEFAULT 1 AFTER `name`;



CREATE TABLE IF NOT EXISTS `player_item`
(
    `character_id`       BIGINT          NOT NULL,
    `item_id`            INT             NOT NULL,
    `count`              INT             NOT NULL DEFAULT 0,
    PRIMARY KEY (`character_id`, `item_id`),
    INDEX (`character_id`)
);

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `player_item` ADD COLUMN IF NOT EXISTS `character_id`       BIGINT          NOT NULL;

ALTER TABLE `player_item` ADD COLUMN IF NOT EXISTS `item_id`            INT             NOT NULL AFTER `character_id`;

ALTER TABLE `player_item` ADD COLUMN IF NOT EXISTS `count`              INT             NOT NULL DEFAULT 0 AFTER `item_id`;



CREATE TABLE IF NOT EXISTS `player_skill`
(
    `character_id`       BIGINT          NOT NULL,
    `skill_id`           INT             NOT NULL,
    `level`              INT             NOT NULL DEFAULT 1,
    PRIMARY KEY (`character_id`, `skill_id`),
    INDEX (`character_id`)
);

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `player_skill` ADD COLUMN IF NOT EXISTS `character_id`       BIGINT          NOT NULL;

ALTER TABLE `player_skill` ADD COLUMN IF NOT EXISTS `skill_id`           INT             NOT NULL AFTER `character_id`;

ALTER TABLE `player_skill` ADD COLUMN IF NOT EXISTS `level`              INT             NOT NULL DEFAULT 1 AFTER `skill_id`;



CREATE TABLE IF NOT EXISTS `player_wallet`
(
    `character_id`       BIGINT          NOT NULL,
    `gold`               BIGINT          NOT NULL DEFAULT 0,
    PRIMARY KEY (`character_id`)
);

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `player_wallet` ADD COLUMN IF NOT EXISTS `character_id`       BIGINT          NOT NULL;

ALTER TABLE `player_wallet` ADD COLUMN IF NOT EXISTS `gold`               BIGINT          NOT NULL DEFAULT 0 AFTER `character_id`;



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

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `character_id`       BIGINT          NOT NULL;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `quest_id`           INT             NOT NULL AFTER `character_id`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `state`              TINYINT         NOT NULL AFTER `quest_id`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `stage`              TINYINT         NOT NULL DEFAULT 1 AFTER `state`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `progress1`          INT             NOT NULL DEFAULT 0 AFTER `stage`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `progress2`          INT             NOT NULL DEFAULT 0 AFTER `progress1`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `progress3`          INT             NOT NULL DEFAULT 0 AFTER `progress2`;

ALTER TABLE `quest_active` ADD COLUMN IF NOT EXISTS `accept_time`        DATETIME        NOT NULL AFTER `progress3`;



CREATE TABLE IF NOT EXISTS `quest_state`
(
    `character_id`       BIGINT          NOT NULL,
    `flags`              BLOB            NOT NULL,
    PRIMARY KEY (`character_id`)
);

-- Reconcile columns added to the schema after this table was first created.
ALTER TABLE `quest_state` ADD COLUMN IF NOT EXISTS `character_id`       BIGINT          NOT NULL;

ALTER TABLE `quest_state` ADD COLUMN IF NOT EXISTS `flags`              BLOB            NOT NULL AFTER `character_id`;

