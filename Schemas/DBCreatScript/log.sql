drop table if exists `history_log`;
CREATE TABLE `history_log` (
  `idx` int(11) NOT NULL AUTO_INCREMENT,
  `submit_time` datetime NOT NULL,
  `user_no` bigint(20) NOT NULL DEFAULT '0',
  `character_no` bigint(20) DEFAULT '0',
  `action` tinyint(4) NOT NULL DEFAULT '0',
  `reason` tinyint(4) NOT NULL DEFAULT '0',
  `param1` int(11) DEFAULT NULL,
  `param2` int(11) DEFAULT NULL,
  `param3` varchar(255) DEFAULT NULL,
  `param4` varchar(255) DEFAULT NULL,
  PRIMARY KEY (`idx`,`submit_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='user history log'
/*!50500 PARTITION BY RANGE  COLUMNS(submit_time)
(PARTITION p20200331 VALUES LESS THAN ('2020-03-31') ENGINE = InnoDB,
 PARTITION p20200401 VALUES LESS THAN ('2020-04-01') ENGINE = InnoDB,
 PARTITION p20200402 VALUES LESS THAN ('2020-04-02') ENGINE = InnoDB,
 PARTITION pmaxvalue VALUES LESS THAN (MAXVALUE) ENGINE = InnoDB) */;


drop table if exists `match_log`;
CREATE TABLE `match_log` (
  `idx` int(11) NOT NULL AUTO_INCREMENT,
  `submit_time` datetime(3) NOT NULL,
  `match_id` bigint(20) NOT NULL,
  `map_id` int(11) DEFAULT '0',
  `leave_player` tinyint(4) DEFAULT '0',
  `result` tinyint(4) DEFAULT '0',
  `clear` tinyint(4) DEFAULT '0',
  `fall_death` int(11) DEFAULT '0',
  `attacked_death` int(11) DEFAULT '0',
  `train_death` int(11) DEFAULT '0',
  `other_death` int(11) DEFAULT '0',
  `normal_item` int(11) DEFAULT '0',
  `tactic_item` int(11) DEFAULT '0',
  `play_time` int(11) DEFAULT '0',
  `win_medal` int(11) DEFAULT '0',
  `lose_medal` int(11) DEFAULT '0',
  `draw_medal` int(11) DEFAULT '0',
  `mvp_medal` int(11) DEFAULT '0',
  `rankup_medal` int(11) DEFAULT '0',
  PRIMARY KEY (`idx`,`submit_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='match log'
/*!50500 PARTITION BY RANGE  COLUMNS(submit_time)
(PARTITION p20200331 VALUES LESS THAN ('2020-03-31') ENGINE = InnoDB,
 PARTITION p20200401 VALUES LESS THAN ('2020-04-01') ENGINE = InnoDB,
 PARTITION p20200402 VALUES LESS THAN ('2020-04-02') ENGINE = InnoDB,
 PARTITION pmaxvalue VALUES LESS THAN (MAXVALUE) ENGINE = InnoDB) */;
