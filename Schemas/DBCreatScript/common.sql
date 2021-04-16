drop table if exists `member`;
CREATE TABLE `member` (
  `member_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `user_no` bigint(20) DEFAULT NULL,
  `player_id` varchar(45) DEFAULT NULL COMMENT '플레이어 아이디\\nkakaogame SDK에서 넘어오는 값',
  `game_token` varchar(100) DEFAULT NULL COMMENT '게임서버 자체 로그인을 위한 토큰',
  `language_code` varchar(30) DEFAULT NULL COMMENT '언어 코드',
  `nation_code` varchar(30) DEFAULT NULL COMMENT '국가 코드',
  `os_version` varchar(80) DEFAULT NULL COMMENT '운영체제 버전',
  `device_model_name` varchar(50) DEFAULT NULL COMMENT '디바이스 모델명',
  `last_play_time` datetime DEFAULT NULL COMMENT '최근 플레이한 시간',
  `idp` varchar(45) DEFAULT NULL COMMENT 'identity provider',
  `user_name` varchar(100) DEFAULT NULL,
  `create_time` datetime DEFAULT NULL COMMENT '생성 시간',
  PRIMARY KEY (`member_no`),
  UNIQUE KEY `player_id_UNIQUE` (`player_id`),
  UNIQUE KEY `user_name_UNIQUE` (`user_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COMMENT='가입, 탈퇴, 로그인에 필요한 정보 저장\n샤딩 고려';

drop table if exists `banned_word`;
CREATE TABLE `banned_word` (
  `ID` int(11) NOT NULL AUTO_INCREMENT,
  `Word` text COLLATE utf8mb4_general_ci,
  PRIMARY KEY (`ID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_general_ci;

drop table if exists `system_mail`;
CREATE TABLE `system_mail` (
  `system_mail_no` bigint(20) NOT NULL AUTO_INCREMENT,
  `sender` varchar(45) DEFAULT NULL,
  `send_reason` varchar(45) DEFAULT NULL,
  `title_en` varchar(45) DEFAULT NULL,
  `title_ko` varchar(45) DEFAULT NULL,
  `body_en` varchar(255) DEFAULT NULL,
  `body_ko` varchar(255) DEFAULT NULL,
  `item_id` int(11) DEFAULT NULL,
  `item_count` int(11) DEFAULT NULL,
  `expiry_days` int(11) DEFAULT NULL,
  `submit_time` datetime DEFAULT NULL,
  PRIMARY KEY (`system_mail_no`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
