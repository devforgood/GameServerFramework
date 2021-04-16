using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace Lobby.Models
{
    public class Mail
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long mail_no { get; set; }
        public long user_no { get; set; }
        public MailState mail_state { get; set; }
        public string sender { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public bool has_item { get; set; }
        public int item_id { get; set; }
        public int item_count { get; set; }
        public DateTime expiry_time { get; set; }
        public DateTime send_time { get; set; }
    }
}
