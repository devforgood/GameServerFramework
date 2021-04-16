using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Lobby.Models
{
    public class LeaderBoardReward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long leader_board_reward_no { get; set; }
        public long leader_board_no { get; set; }
        public int item_id { get; set; }
        public int item_count { get; set; }
    }
}
