using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class ItemLog : PlayerLog
    {
        public string itemType{ get; set; } 
        public string itemId{ get; set; } 
        public string permanence{ get; set; } 
        public string itemAttr1{ get; set; } 
        public long quantity{ get; set; } 
        public string rCurrency{ get; set; } 
        public long cost{ get; set; } 
        public long paidCost{ get; set; } 
        public string modType{ get; set; } 
        public string reason{ get; set; } 
        public string subReason{ get; set; } 
        public string memo { get; set; }
        public long modTime { get; set; }

        public ItemLog(PlayerLog player = null) : base(player)
        {

        }
    }
}
