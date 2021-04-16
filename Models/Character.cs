using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Character
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long character_no { get; set; }

        public long user_no { get; set; }

        public int character_type { get; set; }

        public int character_level { get; set; }

        public int rank_level { get; set; }

        public int battle_score { get; set; }

        public int piece { get; set; }
    }
}
