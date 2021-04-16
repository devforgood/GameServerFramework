using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.send.request
{

    public class msg
    {
        public string receiverId { get; set; }
        public Message message { get; set; }
        public MailItem[] items { get; set; }
    }

    public class Message
    {
        public string messageBoxId { get; set; }
        public string title { get; set; }
        public string body { get; set; }
    }

    public class MailItem
    {
        public string itemCode { get; set; }
        public long quantity { get; set; }
        public long validityTime { get; set; }
    }

}
