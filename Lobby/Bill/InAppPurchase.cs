using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Lobby
{
    public class InAppPurchase
    {
        public string ProductId;


        public static async Task<(bool, InAppPurchase)> IsValidPurchase(GameService.PurchaseInAppRequest request)
        {
            try
            {
                InAppPurchase receiptChecker = null;
                switch (request.Market)
                {
                    case GameService.MarketType.GooglePlay:
                        {
                            var publicKey = ""; // Config.WSConfig.GooglePublicKey;
                            receiptChecker = new InAppPurchaseGoogle(publicKey);
                        }
                        break;
                    case GameService.MarketType.AppStore:
                        {
                            receiptChecker = new InAppPurchaseAppStore();
                        }
                        break;
                }

                var ret = await receiptChecker?.VerifyReceipt(request.Receipt, request.Signature);

                return (ret, receiptChecker);
            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
            }

            return (false, null);
        }


        public virtual async Task<bool> VerifyReceipt(string receipt, string signature)
        {
            return await Task.FromResult(default(bool));
        }


        /// <summary>
        /// WebException : Web요청 200 이외 에러 코드 리턴시
        /// IOException : 서버 Connection 실패시
        /// Exception : 기타 알려지지 않은 익셉션
        /// </summary>
        /// <param name="url">요청 URL</param>
        /// <param name="requestData">POST 요청 넣을 데이터</param>
        /// <param name="isJsonContent">ContentType이 Json인가?</param>
        /// <returns>
        ///     응답 Text
        /// </returns>
        protected async Task<string> HttpRequestText(string url, string requestData, bool isJsonContent)
        {
            HttpWebRequest webReq = null;
            StreamWriter requestStreamWriter = null;
            WebResponse webRes = null;
            StreamReader resReader = null;

            try
            {
                webReq = (HttpWebRequest)WebRequest.Create(url);
                webReq.KeepAlive = false;
                webReq.Method = "POST";
                webReq.ContentType = isJsonContent == true ? "application/json" : "application/x-www-form-urlencoded";
                webReq.ContentLength = requestData.Length;

                requestStreamWriter = new StreamWriter(webReq.GetRequestStream());
                await requestStreamWriter.WriteAsync(requestData);
                requestStreamWriter.Close();

                webRes = webReq.GetResponse();
                resReader = new StreamReader(webRes.GetResponseStream());
                return await resReader.ReadToEndAsync();
            }
            catch (WebException e)
            {
                throw e;
            }
            catch (IOException e)
            {
                throw e;
            }
            catch (Exception e)
            {
                throw new Exception("Unknown exception: " + e.Message, e);
            }
            finally
            {
                if (resReader != null)
                    resReader.Close();

                if (webRes != null)
                    webRes.Close();
            }
        }
    }
}
