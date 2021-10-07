using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace IAP
{
    public class InAppPurchaseAppStore : InAppPurchase
    {
        public int statusCode = -1;
        string appleResponseText = null;
        public static bool SendboxReceiptEnable = false;
        public string transactionID;

        string bundle_id = "adv.kakaogames.zbhunter";

        bool is_test_account { get; set; }


        public override async Task<bool> VerifyReceipt(string receipt, string token)
        {
            var requestJson = new JObject();
            JObject responseJson = null;
            string appleRequestText = null;

            try
            {
                requestJson["receipt-data"] = receipt;
                appleRequestText = requestJson.ToString();
            }
            catch (Exception e)
            {
                Log.Error(e, "in VerifyReceipt");
                return false;
            }

            try
            {
                appleResponseText = await HttpRequestText("https://buy.itunes.apple.com/verifyReceipt", appleRequestText, false);
                try
                {
                    responseJson = JObject.Parse(appleResponseText);
                }
                catch (Exception e)
                {
                    throw new Exception("apple response json parse error, " + e.Message);
                }

                statusCode = (int)responseJson["status"];
            }
            catch (WebException e)
            {
                throw new WebException("https://buy.itunes.apple.com/verifyReceipt exception:" + e.Message, e);
            }
            catch (IOException e)
            {
                throw new IOException("https://buy.itunes.apple.com/verifyReceipt exception:" + e.Message, e);
            }
            catch (Exception e)
            {
                Log.Error(e, "in VerifyReceipt");
                return false;
            }

            try
            {
                // Sandbox 영수증일 경우 
                // Sandbox 영수증 옵션이 켜져있을때만 검증 요청
                if (SendboxReceiptEnable && statusCode == 21007)
                {
                    appleResponseText = await HttpRequestText("https://sandbox.itunes.apple.com/verifyReceipt", appleRequestText, false);
                    responseJson = JObject.Parse(appleResponseText);
                    statusCode = (int)responseJson["status"];

                    is_test_account = true;
                }
                else
                {
                    if (0 != statusCode)
                    {
                        return false;
                    }
                    else
                    {
                        is_test_account = false;
                    }
                }
            }
            catch (WebException e)
            {
                throw new WebException("https://sandbox.itunes.apple.com/verifyReceipt exception:" + e.Message, e);
            }
            catch (IOException e)
            {
                throw new IOException("https://sandbox.itunes.apple.com/verifyReceipt exception:" + e.Message, e);
            }
            catch (Exception e)
            {
                Log.Error(e, "in VerifyReceipt");
                return false;
            }

            // 영수증 체크 : 결제 해킹 앱 사용한 케이스(in_app필드 상에 데이터가 없음)
            {

                // 정상적인 영수증 확인
                if (null == responseJson["receipt"]["in_app"])
                {
                    Log.Error("null == resultJson[in_app]" + appleResponseText);
                    return false;
                }

                // 중복처리 요청인지
                if (null == responseJson["receipt"]["in_app"][0]["transaction_id"])
                {
                    Log.Error("null == resultJson[in_app][transaction_id]" + appleResponseText);
                    return false;
                }

                transactionID = responseJson["receipt"]["in_app"][0]["transaction_id"].ToString();
                marketOrderId = transactionID;

                if (null == responseJson["receipt"]["in_app"][0]["product_id"])
                {
                    Log.Error("null == resultJson[in_app][product_id]" + appleResponseText);
                    return false;
                }

                ProductId = responseJson["receipt"]["in_app"][0]["product_id"].ToString();

                // 우리 게임 맞는지 번들 id비교
                if (bundle_id != (string)responseJson["receipt"]["bundle_id"])
                {
                    Log.Error($"{bundle_id} != (string)resultJson[bundle_id]" + appleResponseText);
                    return false;

                }

                if (null == responseJson["receipt"]["in_app"][0]["purchase_date_ms"])
                {
                    Log.Error("null == responseJson[receipt][in_app][0][purchase_date_ms]" + appleResponseText);
                    return false;
                }

                marketPurchaseTime = responseJson["receipt"]["in_app"][0]["purchase_date_ms"].Value<long>();

            }

            if (0 != statusCode)
            {
                Log.Error("Status " + statusCode + ", " + appleResponseText);
                return false;
            }

            return true;
        }

    }
}
