using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Lobby.Models
{
    public class Member
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long member_no { get; set; }

        public long user_no { get; set; }

        public string player_id { get; set; }

        public string game_token { get; set; }

        public string language_code { get; set; }

        public string nation_code { get; set; }

        public string os_version { get; set; }

        [Column(TypeName = "varchar(50)")]
        public string device_model_name { get; set; }


        private DateTime? _last_play_time;

        public DateTime last_play_time
        {
            set => _last_play_time = value;
            get => _last_play_time
                   ?? throw new InvalidOperationException("Uninitialized property: " + nameof(last_play_time));
        }

        public string user_name { get; set; }

        public DateTime? create_time { get; set; }


    }
}
