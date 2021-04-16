using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long user_no { get; set; }
        public long character_no { get; set; }

        public int play_point { get; set; }

        public int user_grade { get; set; }

        public int battle_score { get; set; }

        public int gem { get; set; }

        public int coin { get; set; }

        public int battle_coin { get; set; }

        public int medal { get; set; }

        public int upgrade_stone { get; set; }

        public int medal_charge { get; set; }

        public DateTime? medal_charge_time { get; set; }

        public byte map_id { get; set; }
    }
}
