using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Shop
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long shop_no { get; set; }
        public long user_no { get; set; }
        public int shop_id { get; set; }
        public DateTime occ_time { get; set; }

        public int shop_item_id { get; set; }
        public int quantity { get; set; }
        public int purchase_count { get; set; }
    }
}
