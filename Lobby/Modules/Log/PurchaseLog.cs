using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class PurchaseLog : PlayerLog
    {
        public string currency { get; set; }
        public decimal price { get; set; }
        public string marketOrderId { get; set; }
        public string marketProductId { get; set; }
        public long marketPurchaseTime { get; set; }
        public Dictionary<string, object> marketPurchaseData { get; set; }
        public long purchasePt { get; set; }
        public long purchaseCount { get; set; }
        public string purchaseToken { get; set; }

        public PurchaseLog(PlayerLog player = null) : base(player)
        {

        }
    }
}
