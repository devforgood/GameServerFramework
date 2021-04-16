SET foreign_key_checks = 0;
drop table if exists user;
SET foreign_key_checks = 1;
CREATE TABLE `user` (
  `user_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `character_no` bigint(20) DEFAULT NULL,
  `play_point` int(11) DEFAULT '0',
  `user_grade` int(11) DEFAULT '0' COMMENT '계정 랭크 레벨',
  `battle_score` int(11) DEFAULT '0' COMMENT '계정 배틀 스코어',
  `gem` int(11) DEFAULT '0',
  `coin` int(11) DEFAULT '0',
  `battle_coin` int(11) DEFAULT '0',
  `medal` int(11) DEFAULT '0',
  `upgrade_stone` int(11) DEFAULT '0',
  `medal_charge` int(11) DEFAULT NULL,
  `medal_charge_time` datetime DEFAULT NULL,
  `map_id` tinyint(3) unsigned NOT NULL DEFAULT '2',
  PRIMARY KEY (`user_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

drop table if exists character_info;
CREATE TABLE `character_info` (
  `character_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `character_type` int(11) DEFAULT NULL,
  `character_level` int(11) DEFAULT NULL,
  `rank_level` int(11) DEFAULT '1' COMMENT '케릭터 랭크 레벨',
  `battle_score` int(11) DEFAULT '0' COMMENT '케릭터 베틀 스코어',
  `piece` int(11) DEFAULT '0',
  PRIMARY KEY (`character_no`),
  KEY `FK_CHARACTER_INFO_idx` (`user_no`),
  CONSTRAINT `FK_CHARACTER_INFO` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

drop table if exists `mission`;
CREATE TABLE `mission` (
  `mission_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `mission_base_id` int(11) DEFAULT NULL COMMENT '미션 종류\n일일 미션, 주간 미션등',
  `occ_time` datetime DEFAULT NULL COMMENT '미션 등록 시간',
  `mission_reward` tinyint(4) DEFAULT NULL,
  `mission_id_1` int(11) DEFAULT NULL COMMENT '미션 아이디',
  `mission_progress_1` int(11) DEFAULT NULL COMMENT '미션 진척 상황',
  `mission_reward_1` tinyint(4) DEFAULT NULL,
  `mission_id_2` int(11) DEFAULT NULL,
  `mission_progress_2` int(11) DEFAULT NULL,
  `mission_reward_2` tinyint(4) DEFAULT NULL,
  `mission_id_3` int(11) DEFAULT NULL,
  `mission_progress_3` int(11) DEFAULT NULL,
  `mission_reward_3` tinyint(4) DEFAULT NULL,
  `mission_id_4` int(11) DEFAULT NULL,
  `mission_progress_4` int(11) DEFAULT NULL,
  `mission_reward_4` tinyint(4) DEFAULT NULL,
  `mission_id_5` int(11) DEFAULT NULL,
  `mission_progress_5` int(11) DEFAULT NULL,
  `mission_reward_5` tinyint(4) DEFAULT NULL,
  PRIMARY KEY (`mission_no`),
  KEY `FK_MISSION_idx` (`user_no`),
  CONSTRAINT `FK_MISSION` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;


drop table if exists `shop`;
CREATE TABLE `shop` (
  `shop_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `shop_id` int(11) DEFAULT NULL,
  `occ_time` datetime DEFAULT NULL,
  `shop_item_id` int(11) DEFAULT NULL,
  `quantity` int(11) DEFAULT NULL,
  `purchase_count` int(11) DEFAULT NULL,
  PRIMARY KEY (`shop_no`),
  KEY `FK_SHOP_idx` (`user_no`),
  CONSTRAINT `FK_SHOP` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

drop table if exists `game_event`;
CREATE TABLE `game_event` (
  `event_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `event_id` int(11) DEFAULT NULL,
  `reward` tinyint(4) DEFAULT NULL,
  `occ_time` datetime DEFAULT NULL,
  PRIMARY KEY (`event_no`),
  KEY `FK_GAME_EVENT_idx` (`user_no`),
  CONSTRAINT `FK_GAME_EVENT` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

drop table if exists `advertisement_reward`;
CREATE TABLE `advertisement_reward` (
  `advertisement_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `advertisement_id` int(11) DEFAULT NULL,
  `reward` int(11) DEFAULT NULL,
  `occ_time` datetime DEFAULT NULL,
  PRIMARY KEY (`advertisement_no`),
  KEY `FK_ADVERTISEMENT_REWARD_idx` (`user_no`),
  CONSTRAINT `FK_ADVERTISEMENT_REWARD` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;

drop table if exists `mailbox`;
CREATE TABLE `mailbox` (
  `mail_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `sender` varchar(45) DEFAULT NULL,
  `title` varchar(45) DEFAULT NULL,
  `body` varchar(255) DEFAULT NULL,
  `item_id` int(11) DEFAULT NULL,
  `item_count` int(11) DEFAULT NULL,
  `expiry_time` datetime DEFAULT NULL,
  `send_time` datetime DEFAULT NULL,
  PRIMARY KEY (`mail_no`),
  KEY `FK_MAILBOX_idx` (`user_no`),
  CONSTRAINT `FK_MAILBOX` FOREIGN KEY (`user_no`) REFERENCES `user` (`user_no`) ON DELETE NO ACTION ON UPDATE NO ACTION
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
