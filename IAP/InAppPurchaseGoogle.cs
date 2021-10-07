using Google.Apis.AndroidPublisher.v3;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace IAP
{
    public class InAppPurchaseGoogle : InAppPurchase
    {
        //The package name of the application for which this subscription was purchased (for example, 'com.some.thing'). More...
        public string PackageName = "adv.kakaogames.zbhunter";
        public string ApplicationName = "adv.kakaogames.zbhunter";


        public override async Task<bool> VerifyReceipt(string token, string productId)
        {
            try
            {
                string fileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "GoogleCredential.json");

                GoogleCredential credential;
                using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
                {
                    credential = (await GoogleCredential.FromStreamAsync(stream, System.Threading.CancellationToken.None))
                        .CreateScoped(AndroidPublisherService.Scope.Androidpublisher);
                }

                var service = new AndroidPublisherService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    //ApplicationName = ApplicationName,
                });

                var request = service.Purchases.Products.Get(PackageName, productId, token);
                var purchase_state = await request.ExecuteAsync();

                ProductId = purchase_state.ProductId;
                marketPurchaseTime = (long)purchase_state.PurchaseTimeMillis;
                marketOrderId = purchase_state.OrderId;

                //var request2 = service.Inappproducts.Get(PackageName, productId);
                //var state = await request2.ExecuteAsync();
                //var prices = state.Prices;



            }
            catch(Exception ex)
            {
                Log.Error(ex.ToString());
                return false;
            }
            return true;
        }
    }
}
