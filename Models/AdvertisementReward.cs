using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class AdvertisementReward
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long advertisement_no { get; set; }
        public long user_no { get; set; }
        public int advertisement_id { get; set; }
        public int reward { get; set; }
        public DateTime occ_time { get; set; }
    }
}
