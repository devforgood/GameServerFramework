using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lobby.Models
{
    public class HistoryLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long idx { get; set; }
        public DateTime submit_time { get; set; }
        public long user_no { get; set; }
        public long character_no { get; set; }
        public byte action { get; set; }
        public byte reason { get; set; }
        public int? param1 { get; set; }
        public int? param2 { get; set; }
        public string param3 { get; set; }
        public string param4 { get; set; }

    }
}
