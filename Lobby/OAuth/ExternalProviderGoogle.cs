using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.OAuth
{
    public class ExternalProviderGoogle
    {
        public static string GoogleTokenExchangeURL = "https://accounts.google.com/o/oauth2/token";
        public static string GoogleAuthUrl = "https://www.googleapis.com/games/v1/applications/xxxxxx/verify";
        public static string GoogleClientId = "xxxxxx";
        public static string GoogleClientSecret = "xxxxxx";
        public static string GooglePublicKey = "xxxxxx";


        string accessToken = string.Empty;


        public async Task<bool> CheckAuth(string platformId, string platformToken)
        {
            if (await GetAccessToken(platformId, platformToken) == false)
                return false;

            return await GoogleIdCheck(platformId, accessToken);
        }

        public async Task<bool> GetAccessToken(string venderUID, string venderToken)
        {
            try
            {
                string data = $"grant_type=authorization_code&code={venderToken}&client_id={GoogleClientId}&client_secret={GoogleClientSecret}";

                var utfenc = new UTF8Encoding();
                byte[] buffer = utfenc.GetBytes(data);

                WebRequest request = WebRequest.CreateHttp(GoogleTokenExchangeURL);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.ContentLength = buffer.Length;

                using (Stream strm = request.GetRequestStream())
                {
                    await strm.WriteAsync(buffer, 0, buffer.Length);
                    strm.Close();
                }

                var response = await request.GetResponseAsync();
                string resultString = string.Empty;

                using (var responseStream = new StreamReader(response.GetResponseStream()))
                {
                    resultString = await responseStream.ReadToEndAsync();
                }

                var jsonData = JObject.Parse(resultString);
                accessToken = (jsonData["access_token"] != null) ? (string)jsonData["access_token"] : string.Empty;
            }
            catch (Exception e)
            {
                Log.Error(e, "Google GetAccessToekn failed");
                return false;

            }
            return true;
        }

        public async Task<bool> GoogleIdCheck(string venderUID, string accessToken)
        {

            bool isSuccess = true;
            WebResponse response = null;
            try
            {
                WebRequest request = WebRequest.CreateHttp(GoogleAuthUrl);
                request.Method = "GET";
                request.Headers.Add("Authorization", "OAuth " + accessToken);
                response = await request.GetResponseAsync();
            }
            catch (WebException e)
            {
                isSuccess = false;
                response = e.Response;
            }

            string resultString = string.Empty;
            try
            {
                using (var responseStream = response.GetResponseStream())
                {
                    var recv_buffer = new byte[8192];

                    var total_read = 0;
                    var read_pos = 0;
                    do
                    {
                        read_pos = await responseStream.ReadAsync(recv_buffer, read_pos, 512);
                        if (0 >= read_pos)
                            break;

                        total_read += read_pos;
                    }
                    while (0 < read_pos && total_read < recv_buffer.Length);

                    resultString = Encoding.UTF8.GetString(recv_buffer, 0, total_read);

                }

                if (isSuccess)
                {
                    // 성공했을 경우 player_id (USN) 파싱해서 일치하는지 확인한다
                    var jsonData = JObject.Parse(resultString);

                    var player_id = (jsonData["player_id"] != null) ? (string)jsonData["player_id"] : string.Empty;
                    if (false == string.IsNullOrEmpty(player_id) && venderUID == player_id)
                    {
                        //Logger.Info($"Google+ TokenCheck success, usn:{venderUID}, token:{accessToken}");
                        return true;
                    }

                    resultString = $"miss match client:{venderUID} != server:{player_id}";
                }


            }
            catch (Exception exception)
            {
                resultString = exception.ToString();
            }

            Log.Error($"Google TokenCheck failed, cause:{resultString}, usn:{venderUID} token:{accessToken}");
            return false;
        }
    }
}
