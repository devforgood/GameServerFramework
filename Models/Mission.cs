using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Mission
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long mission_no { get; set; }

        public long user_no { get; set; }

        public int mission_base_id { get; set; }

        public DateTime occ_time { get; set; }

        public bool mission_reward { get; set; }

        public int mission_id_1 { get; set; }

        public int mission_progress_1 { get; set; }

        public bool mission_reward_1 { get; set; }

        public int mission_id_2 { get; set; }

        public int mission_progress_2 { get; set; }

        public bool mission_reward_2 { get; set; }

        public int mission_id_3 { get; set; }

        public int mission_progress_3 { get; set; }

        public bool mission_reward_3 { get; set; }

        public int mission_id_4 { get; set; }

        public int mission_progress_4 { get; set; }

        public bool mission_reward_4 { get; set; }

        public int mission_id_5 { get; set; }

        public int mission_progress_5 { get; set; }

        public bool mission_reward_5 { get; set; }


    }
}
