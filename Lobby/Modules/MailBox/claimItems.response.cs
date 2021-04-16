using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.claimItems.response
{
    public class msg
    {
        public Result[] results { get; set; }
    }

    public class Result
    {
        public string messageId { get; set; }
        public int status { get; set; }
        public string receiverId { get; set; }
        public string senderId { get; set; }
        public Resourcemap resourceMap { get; set; }
        public Item[] items { get; set; }
    }

    public class Resourcemap
    {
    }

    public class Item
    {
        public string appId { get; set; }
        public string itemCode { get; set; }
        public long quantity { get; set; }
        public string itemId { get; set; }
        public long validityTime { get; set; }
        public string senderId { get; set; }
    }
}
