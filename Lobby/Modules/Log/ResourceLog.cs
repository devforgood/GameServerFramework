using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class ResourceLog : PlayerLog
    {
        public string paid { get; set; }
        public string rCurrency { get; set; }
        public int delta { get; set; }
        public int paidDelta { get; set; }
        public int amount { get; set; }
        public string modType { get; set; }
        public long modTime { get; set; }
        public string reason { get; set; }
        public string subReason { get; set; }
        public string resourceAttr1 { get; set; }
        public string resourceAttr2 { get; set; }
        public string resourceAttr3 { get; set; }
        public string resourceAttr4 { get; set; }
        public string memo { get; set; }

        public ResourceLog(PlayerLog player = null) : base(player)
        {

        }
    }
}
