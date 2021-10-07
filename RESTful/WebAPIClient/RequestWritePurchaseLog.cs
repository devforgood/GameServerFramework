using System;
using System.Collections.Generic;
using System.Text;

namespace WebAPIClient
{
    public class RequestWritePurchaseLog
    {
        public string currency { get; set; } // 통화 코드

        public decimal price { get; set; } // VAT 포함 상품 금액

        public string os { get; set; } //  OS 유형

        public string country { get; set; }

        public string market { get; set; }

        public string marketOrderId { get; set; }

        public string marketProductId { get; set; }

        public long marketPurchaseTime { get; set; }

        public string purchaseToken { get; set; }

    }
}
