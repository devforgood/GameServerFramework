using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lobby.Models
{
    public class MatchLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long idx { get; set; }
        public DateTime submit_time { get; set; }
        public long match_id { get; set; }
        public int map_id { get; set; }
        public int leave_player { get; set; } //게임 중 이탈된 플레이어 수 카운트
        public int result { get; set; } // 승패, 무승부
        public int clear { get; set; } // 기지 파괴 or 타임 아웃

        public int fall_death { get; set; }
        public int attacked_death { get; set; }
        public int train_death { get; set; }
        public int other_death { get; set; }

        public int normal_item { get; set; }
        public int tactic_item { get; set; }

        public int play_time { get; set; }

        public int win_medal { get; set; }
        public int lose_medal { get; set; }
        public int draw_medal { get; set; }
        public int mvp_medal { get; set; }
        public int rankup_medal { get; set; }

    }
}
