using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Lobby.Models
{
    public class SystemMail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long system_mail_no { get; set; }
        public string sender { get; set; }
        public string send_reason { get; set; }
        public string recipient { get; set; }
        public string title_en { get; set; }
        public string title_ko { get; set; }
        public string body_en { get; set; }
        public string body_ko { get; set; }
        public int item_id { get; set; }
        public int item_count { get; set; }
        public int expiry_days { get; set; }
        public DateTime submit_time { get; set; }
    }
}
