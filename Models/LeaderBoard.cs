using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Lobby.Models
{
    public class LeaderBoard
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long leader_board_no { get; set; }
        public string leader_board_name { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Select a correct license")]
        public SeasonPeriod season_period { get; set; } // enum SeasonPeriod

        [Range(0, int.MaxValue, ErrorMessage = "Select a correct license")]
        public ScoreUpdateType score_update_type { get; set; } // enum ScoreUpdateType

        [Range(0, int.MaxValue, ErrorMessage = "Select a correct license")]
        public StackExchange.Redis.Order score_order { get; set; } // enum  StackExchange.Redis.Order

        public DateTime submit_time { get; set; } // 등록 시간

        
    }
}
