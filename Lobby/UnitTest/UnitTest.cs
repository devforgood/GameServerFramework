using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class UnitTest
    {
        static readonly HttpClient client = new HttpClient();


        public static void Test()
        {
            TestMail().Wait();

            TestString();

            var r111 = TestRanking().Result;
            return;

            {
                var values = new StackExchange.Redis.SortedSetEntry[]
                {
                    new StackExchange.Redis.SortedSetEntry("a:1", 0),
                    new StackExchange.Redis.SortedSetEntry("b:2", 0),
                    new StackExchange.Redis.SortedSetEntry("c:3", 0),
                    new StackExchange.Redis.SortedSetEntry("d:4", 0),
                    new StackExchange.Redis.SortedSetEntry("e:5", 0),
                    new StackExchange.Redis.SortedSetEntry("f:6", 0),
                    new StackExchange.Redis.SortedSetEntry("g:7", 0),
                };

                Cache.Instance.GetDatabase().SortedSetAdd("myzset", values);

                var redis_ret = Cache.Instance.GetDatabase().SortedSetRangeByValue("myzset", "c:", "+", StackExchange.Redis.Exclude.Start, 0, 1, StackExchange.Redis.CommandFlags.None);
                //var redis_ret = Cache.Instance.GetDatabase().SortedSetRangeByValue("myzset", "c:", "c:");


            }




            {
                var r21 = PlayerLog.GetPlayerInfo("990252821976346", 6029, 6029).Result;

                var r22 = WebAPIClient.Web.writeLog("990252821976346", "/log/writeActionLog", JsonConvert.SerializeObject(new ActionLog(r21) { category = "캐릭터", action = "획득", label = "1" }));

            }

            {
                var ret_a = core.MathHelpers.GetResetTime("00:00:00", new DateTime(2020, 11, 13, 9, 10, 3));
                var ret_b = core.MathHelpers.GetResetTime("00:00:00", new DateTime(2020, 11, 14, 0, 0, 0));

                if(ret_a != ret_b)
                {

                }
            }


            var first = new DateTime(2020, 9, 6);
            var second = new DateTime(2020, 9, 7);

            var dif2 = core.MathHelpers.weekDiff(first, second);
            first = new DateTime(2020, 9, 8);
            second = new DateTime(2020, 9, 7);
            dif2 = core.MathHelpers.weekDiff(first, second);

            using (var context = new Lobby.Models.GameContext(0))
            {
                try
                {
                    var rows = context.shop.ToList();

                    foreach(var row in rows)
                    {
                        row.Clear();
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }


            Task.Run(async () =>
            {

                await using (var user = await UserCache.GetUser(0, 4888, true, true))
                {
                    user.medal_charge_time = DateTime.UtcNow;
                    user.IsDirty = true;
                    //UserQuery.UpdateUserMedalCharge(user).Wait();
                }
            });



            return;

            var list = BannedWordQuery.GetBannedWord().Result;


            //bool isLetter = false;
            if(Char.IsLetterOrDigit("test한글", 0))
            {
                //isLetter = true;
            }


            string letter = "test한글&\n";
            var sret = letter.IsLetterOrDigit();
            letter = "test한글111";
            sret = letter.IsLetterOrDigit();


            //var r33 = UserQuery.UpdateUser(2672, "test15").Result;

            Task.Run(async () =>
            {
                //await LobbyService.LoadUserInfo(2672);
                //await LobbyService.LoadUserInfo(2672);

                var characters = await CharacterCache.Instance.GetEntities(0, 2672, true);

                await CharacterCache.Instance.RemoveEntity(0, characters[0]);

                await CharacterCache.Instance.RemoveEntities(0, 2672);

                characters = await CharacterCache.Instance.GetEntities(0, 2672, false);
            });

            Task.Run(async () =>
            {
                await using (var mylock = await RedLock.CreateLockAsync("lock:character:1"))
                {
                    var ret = mylock.IsAcquired;
                }
            });

            Task.Run(async () =>
            {
                await using (var mylock = await RedLock.CreateLockAsync("lock:character:1"))
                {
                    var ret = mylock.IsAcquired;
                }
            });

            var r = WebAPIClient.Web.validate("990252821976346", "fwPla7fQ8ty9+DZT/lD//opW3OMPm7j2xvs2KuD+uYr9sjDBcwhG6af5snAmhRvRlplBuo/unVh4Ugt9HD7BWi2WfhT1va61xImzER4+UZzS7WO986OqafxFUTWyLV/k5UWPbS3ijhByFvqFR9j1kYk15clNysZGWi92KZTFr2BzzZ3rCKzcT6oVZjBFc3TqObRQBTI/40qwze1NQA7qReTr6OzO45hUebMuluFiHac=").Result;
            var r2 = WebAPIClient.Web.getInfo("990252821976346").Result;
            var r3 = WebAPIClient.Web.request("990252821976346", "/player/memberKey/get").Result;

            TestHttpClient().ConfigureAwait(false);


            //var charac = CharacterCache.InsertCharacter(new Models.Character() { user_no = 4 }).Result;


            var ret = CharacterCache.Instance.GetEntities(0, 2019, true).Result;
            var ret2 = CharacterCache.Instance.GetEntities(0, 2019, true).Result;

            //WebAPIClient.Web.test();
            TestRankSeq();

            Lobby.Models.CommonContext.test();
            Lobby.Models.GameContext.test();
            Lobby.Models.LogContext.test();


            Task.Run(async () => await TestCache());

        }

        public static void QueryListUp()
        {

            Task.Run(async () =>
            {
                try
                {
                    //await MemberQuery.GetMember("G310f55c2d9ad4c068a80fb0bcdc3d96e");
                    //await MemberQuery.AddMember(new Models.Member() { player_id = "test", last_play_time = DateTime.UtcNow });
                    //await MemberQuery.UpdateMember(5996, 1);
                    //await MemberQuery.UpdateMember(5996, "test234dfasd");

                    //await UserQuery.GetUser(4888);
                    //await UserQuery.UpdateUser(4888, 0);
                    //await UserQuery.AddUser(new Models.User() { });
                    //await UserQuery.UpdateUser(new Models.User() {user_no = 4888, battle_coin = 1});

                    //await new CharacterQuery().Gets(5335);
                    //await new CharacterQuery().Get(2682);
                    //await new CharacterQuery().Insert(new Models.Character() { user_no = 5335, character_type = 7});
                    //await new CharacterQuery().Update(new Models.Character() { character_no = 2782, user_no = 5335, character_level = 1});
                    //await CharacterQuery.Remove(new Models.Character() { character_no = 2782, user_no = 5335, character_level = 1 });

                    //await new ShopQuery().Gets(5335);
                    //await new ShopQuery().Get(15);
                    //await new ShopQuery().Insert(new Models.Shop() { user_no = 5335, shop_id = 1, occ_time = DateTime.UtcNow, shop_item_id = 0, quantity = 0, purchase_count = 0 });
                    //await new ShopQuery().Update(new Models.Shop() { shop_no= 183, user_no = 5335, shop_id = 1, occ_time = DateTime.UtcNow, shop_item_id = 0, quantity = 0, purchase_count = 1 });
                    //await ShopQuery.Remove(new Models.Shop() { shop_no= 183, user_no = 5335, shop_id = 1, occ_time = DateTime.UtcNow, shop_item_id = 0, quantity = 0, purchase_count = 1 });

                    //await new MissionQuery().Gets(5335);
                    //await new MissionQuery().Get(26);
                    //await new MissionQuery().Insert(new Models.Mission() { user_no = 5335, mission_base_id = 2, occ_time = DateTime.UtcNow });
                    //await new MissionQuery().Update(new Models.Mission() { user_no = 5335, mission_base_id = 2, occ_time = DateTime.UtcNow, mission_no = 42 });
                    //await MissionQuery.Remove(new Models.Mission() { user_no = 5335, mission_base_id = 2, occ_time = DateTime.UtcNow, mission_no = 42 });

                    //History.Info(1, 1, HistoryLogAction.Login, HistoryLogReason.None, 0, 0, "", "");


                    await BannedWordQuery.GetBannedWord();


                }
                catch (Exception ex)
                {
                    Log.Information(ex.ToString());
                }
            });
        }

        static async Task TestHttpClient()
        {
            // Call asynchronous network methods in a try/catch block to handle exceptions.
            try
            {
                HttpResponseMessage response = await client.GetAsync("http://www.contoso.com/");
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                Console.WriteLine(responseBody);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
            }
        }

        public static void TestRankSeq()
        {
            var seq = new RankSequencer() { rank = 5, min_rank = 1, max_rank = 13 };
            foreach(var s in seq)
            {
                Log.Information($"{s}");
            }
            var seq2 = new RankSequencer() { rank = 0, min_rank = 0, max_rank = 0 };
            foreach (var s in seq2)
            {
                Log.Information($"{s}");
            }
        }

        public static async Task TestCache()
        {
            var u1 = new WaitingUser() { map_id = 1, rank = 1, };
            var u2 = new WaitingUser() { map_id = 1, rank = 1, };
            var u3 = new WaitingUser() { map_id = 1, rank = 1, };
            var u4 = new WaitingUser() { map_id = 1, rank = 1, };
            var u5 = new WaitingUser() { map_id = 1, rank = 1, };

            await WaitingList.AddWaitingUser(u1, 1);
            await Task.Delay(10 * 1000);
            await WaitingList.AddWaitingUser(u2, 2);
            await WaitingList.AddWaitingUser(u3, 3);
            await WaitingList.AddWaitingUser(u4, 4);
            await WaitingList.AddWaitingUser(u5, 4);

            var ret1 = await WaitingList.GetWaitingUser(1, 1, 3);


            var ret2 = await WaitingList.GetWaitingUser(1, 1, 3);


            var ret3 = await WaitingList.RemoveWaitingUser(3);

            await WaitingList.RemoveWaitingUser(1, 1, 2);

        }


        static async Task<bool> TestRanking()
        {
            Random rnd = new Random();

            //zincrby: 지정 멤버에 일정 점수를 더한다(실습에서는 하나씩 세기위해 1씩 더합니다)
            //zscore: 지정 멤버의 현재 점수를 구한다
            //zrevrank: 지정 멤버의 랭킹을 조회한다(점수가 높을수록 1위에 가깝다)
            //zrevrangebyscore: 상위 랭킹 멤버들을 조회한다

            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "denny", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Albert", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Andrew", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Cecil", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Duncan", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Enoch", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Eugene", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Geoffrey", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Lawrence", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Michael", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Oscar", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Philip", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Samuel", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Thomas", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Wallace", rnd.Next(1000));
            await Cache.Instance.GetDatabase().SortedSetIncrementAsync("myzset2", "Richard", rnd.Next(1000));

            var ret = await Cache.Instance.GetDatabase().SortedSetScoreAsync("myzset2", "denny");
            var ret1 = await Cache.Instance.GetDatabase().SortedSetScoreAsync("myzset2", "angel");

            var ret2 = await Cache.Instance.GetDatabase().SortedSetRankAsync("myzset2", "denny", StackExchange.Redis.Order.Descending);
            var ret3 = await Cache.Instance.GetDatabase().SortedSetRankAsync("myzset2", "angel", StackExchange.Redis.Order.Descending);


            var ret4 = await Cache.Instance.GetDatabase().SortedSetRangeByScoreAsync("myzset2", double.NegativeInfinity, double.PositiveInfinity, StackExchange.Redis.Exclude.None
                , StackExchange.Redis.Order.Descending, 0, -1);

            await Modules.LeaderBoard.LearderBoard.NextLeaderBoardSeason("myzset2");
            await Modules.LeaderBoard.LearderBoard.Accumulate("myzset2", "angel", 10, "{test:1, test2:10}");
            await Modules.LeaderBoard.LearderBoard.GetRankList("myzset2", 0, 10);
            await Modules.LeaderBoard.LearderBoard.GetRankList("myzset2", 10, 10);

            var ret5 = await Cache.Instance.GetDatabase().SortedSetRangeByScoreAsync("myzset2", double.NegativeInfinity, double.PositiveInfinity, StackExchange.Redis.Exclude.None
    , StackExchange.Redis.Order.Descending, 0, 10);

            var ret6 = await Cache.Instance.GetDatabase().SortedSetRangeByScoreAsync("myzset2", double.NegativeInfinity, double.PositiveInfinity, StackExchange.Redis.Exclude.None
, StackExchange.Redis.Order.Descending, 10, 10);


            return true;
        }

        static void TestString()
        {
            char[] delimiterChars = { ' ', ',', '.', ':', '\t'};

            string text = @"one\ttwo three:four,five six
seven";
            System.Console.WriteLine($"Original text: '{text}'");

            string[] words = text.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            System.Console.WriteLine($"{words.Length} words in text:");

        }

        static async Task TestMail()
        {
            var count = 3;
            int nextPageKey = 1;



            List<MailState> states = null;
            states = new List<MailState>();
            states.Add((MailState)System.Enum.Parse(typeof(MailState), "Send"));
            states.Add((MailState)System.Enum.Parse(typeof(MailState), "Read"));
            states.Add((MailState)System.Enum.Parse(typeof(MailState), "Delete"));

            var ret = await MailBox.GetMails(1, 1, count, nextPageKey, states);

            var ret2 = await MailBox.GetMails(1, 1, 3, 0, null);
            var ret3 = await MailBox.GetMails(1, 1, 3, 2, null);
            if(ret3 == string.Empty)
            {

            }
        }

    }
}
