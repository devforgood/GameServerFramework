using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Login
{
    public class Member
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long member_no { get; set; }

        public string player_id { get; set; }

        public int selected_character { get; set; }

        public string characters { get; set; }

        public int coin { get; set; }

    }
}
