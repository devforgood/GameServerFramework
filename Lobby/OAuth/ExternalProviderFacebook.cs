using Facebook;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby.OAuth
{
    public class ExternalProviderFacebook
    {
        public static string FacebookAppId;
        public static string FacebookSecretKey;

        public async Task<bool> CheckAuth(string platformId, string platformToken)
        {
            try
            {
                var client = new FacebookClient();
                dynamic result = await client.GetTaskAsync("/oauth/access_token", new
                {
                    grant_type = "client_credentials",
                    client_id = FacebookAppId,
                    client_secret = FacebookSecretKey,
                });

                client = new FacebookClient((string)result.access_token);
                dynamic debugResult = await client.GetTaskAsync("/debug_token", new
                {
                    input_token = platformToken
                });
 
                return debugResult.data.is_valid && debugResult.data.user_id == platformId;
            }
            catch (Exception e)
            {
                Log.Error(e, "Facebook TokenCheck failed");
                return false;
            }
        }
    }
}
