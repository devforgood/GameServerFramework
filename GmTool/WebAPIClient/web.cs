using Lobby;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace GmTool.WebAPIClient
{
    public class Web
    {
        private static readonly HttpClient client = new HttpClient();

        public static string url;
        public static string version;
        public static string appId;
        public static string appSecret;
        public static string Authorization;

        static string ServerURL => $"{url}{version}";

        public static void test()
        {
            var repositories = ProcessRepositories().Result;

            foreach (var repo in repositories)
            {
                Console.WriteLine(repo.Name);
                Console.WriteLine(repo.Description);
                Console.WriteLine(repo.GitHubHomeUrl);
                Console.WriteLine(repo.Homepage);
                Console.WriteLine(repo.Watchers);
                Console.WriteLine(repo.LastPush);
                Console.WriteLine();
            }
        }

        private static async Task<List<Repository>> ProcessRepositories()
        {
            var serializer = new DataContractJsonSerializer(typeof(List<Repository>));

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");

            var streamTask = client.GetStreamAsync("https://api.github.com/orgs/dotnet/repos");
            var repositories = serializer.ReadObject(await streamTask) as List<Repository>;
            return repositories;
        }





        /*public static async Task<PlayerLog> getInfo(string playerId)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                RequestGetInfo body = new RequestGetInfo();
                body.fields = new string[]
                {
                    "os",
                    "country",
                    "lang",
                    "market"
                };
                var str = JsonConvert.SerializeObject(body);

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("KakaoAK", Authorization);

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"{ServerURL}/player/get");
                msg.Content = new StringContent(str, Encoding.UTF8, "application/json");
                msg.Headers.Add("appId", appId);
                msg.Headers.Add("appSecret", appSecret);
                msg.Headers.Add("playerId", playerId);
                //msg.Headers.Add("playerId", "1824950129");

                HttpResponseMessage response = await client.SendAsync(msg);

                // Get the response
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error($"getInfo error :{responseString}");
                    return null;
                }

                var playerInfo = JsonConvert.DeserializeObject<PlayerInfoLog>(responseString);
                Log.Information($"validate ok :{responseString}");

                return playerInfo.player;
            }
            catch (HttpRequestException e)
            {
                Log.Error($"HttpRequestException :{e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception :{e.Message}");
            }
            return null;
        }*/

        public static async Task<string> request(string playerId, string api, string body = "{}")
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                Log.Information($"{playerId}, {api} : {body}");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("KakaoAK", Authorization);

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"{ServerURL}{api}");
                msg.Content = new StringContent(body, Encoding.UTF8, "application/json");
                msg.Headers.Add("appId", appId);
                msg.Headers.Add("appSecret", appSecret);
                if(!string.IsNullOrEmpty(playerId))
                    msg.Headers.Add("playerId", playerId);

                HttpResponseMessage response = await client.SendAsync(msg);

                // Get the response
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error($"{api} error :{responseString}");
                    return "";
                }

                Log.Information($"{api} : {responseString}");

                return responseString;
            }
            catch (HttpRequestException e)
            {
                Log.Error($"HttpRequestException :{e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception :{e.Message}");
            }
            return string.Empty;
        }

        public static async Task<(bool, ResponseValidate)> validate(string playerId, string accessToken)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                RequestValidate body = new RequestValidate();
                body.zat = accessToken;
                //body.zat = "fwPla7fQ8ty9+DZT/lD//kgAL/cRgup58vFF4yRW1Asg3Z4uk4Vtfr8byU/umYOaX066r6lvr/U+LJfZ65qmwDtMRdzJwanAfTCm9Jz/Ml+wHhJTLz9UgbVROJ0qpdMDeADPAbGrdsIKxP8Ayol+1E41YsIeZGkd6hMZVi6ZyYe5J+IikEl4EIGDOxycCCaW8jitxK0/LWY0dG83mZTNlQ==";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("KakaoAK", Authorization);

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"{ServerURL}/zat/validate");
                msg.Content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                msg.Headers.Add("appId", appId);
                msg.Headers.Add("appSecret", appSecret);
                msg.Headers.Add("playerId", playerId);
                //msg.Headers.Add("playerId", "1824950129");

                HttpResponseMessage response = await client.SendAsync(msg);

                // Get the response
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error($"validate error :{responseString}");
                    return (false, null);
                }

                var responseValidate = JsonConvert.DeserializeObject<ResponseValidate>(responseString);
                Log.Information($"validate ok :{responseString}");

                return (true, responseValidate);
            }
            catch (HttpRequestException e)
            {
                Log.Error($"HttpRequestException :{e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception :{e.Message}");
            }
            return (false, null);
        }


        public static async Task<bool> writeLog(string playerId, string api, string body)
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                Log.Information($"{playerId}, {api} : {body}");

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("KakaoAK", Authorization);

                HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, $"{ServerURL}{api}");
                msg.Content = new StringContent(body, Encoding.UTF8, "application/json");
                msg.Headers.Add("appId", appId);
                msg.Headers.Add("appSecret", appSecret);
                msg.Headers.Add("playerId", playerId);

                HttpResponseMessage response = await client.SendAsync(msg);

                // Get the response
                var responseString = await response.Content.ReadAsStringAsync();
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    Log.Error($"write log error :{responseString}");
                    return false;
                }

                return true;
            }
            catch (HttpRequestException e)
            {
                Log.Error($"HttpRequestException :{e.Message}");
            }
            catch (Exception e)
            {
                Log.Error($"Exception :{e.Message}");
            }
            return false;
        }
    }
}
