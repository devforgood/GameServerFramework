using core;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class DebugCommand
    {
        public static async Task<GameService.DebugCommandReply> Execute(Session session, string Cmd, string Param1, string Param2, string Param3, string Param4)
        {
            Task<(bool, string)> result = (Task<(bool, string)>)System.Reflection.Assembly.GetExecutingAssembly().GetType("Lobby.DebugCommand").GetMethod(Cmd).Invoke(null, new object[] { session, Param1, Param2, Param3, Param4 });
            await result;
            if (result.Result.Item1 == false)
            {
                return new GameService.DebugCommandReply() { Code = GameService.ErrorCode.WrongParam };
            }

            return new GameService.DebugCommandReply() { Code = GameService.ErrorCode.Success, Result = result.Result.Item2 };
        }



        public static async Task<(bool, string)> clearSession(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            await Session.ClearSession(session.session_id);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetMedal(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var medal = int.Parse(Param1);
            Models.User user = await UserCache.GetUser(session.member_no, session.user_no, true);
            user.medal = medal;
            await UserCache.SetUser(user);
            await UserQuery.UpdateUser(session.member_no, user);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetMedalCharge(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var medal_charge = int.Parse(Param1);
            Models.User user = await UserCache.GetUser(session.member_no, session.user_no, true);
            user.medal_charge = medal_charge;
            user.medal_charge_time = DateTime.UtcNow;
            await UserCache.SetUser(user);
            await UserQuery.UpdateUser(session.member_no, user);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> GetShop(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var shops = await ShopManager.GetShopLock(session);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> BuyItem(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var ShopItemId = int.Parse(Param1);
            var ShopId = int.Parse(Param2);

            var reply = await ShopManager.BuyItem(session, ShopItemId, ShopId);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> InsertItem(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var ItemId = int.Parse(Param1);
            var ItemCount = int.Parse(Param2);

            await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
            await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
            {
                await Inventory.Insert(session, user, character, ItemId, ItemCount, new LogReason("A_ADMIN"));
            }
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> RemoveCharacter(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var CharacterId = int.Parse(Param1);
            var find_character = await CharacterCache.Instance.GetEntity(session.member_no, session.user_no, CharacterId, true, false, false);
            if (find_character == null || find_character == default(Models.Character))
            {
                Log.Error($"RemoveCharacter cannot find character_id:{CharacterId}, user_name:{session.user_name}");
                return (false, string.Empty);
            }
            else
            {
                await CharacterQuery.Remove(session.member_no, find_character);
                await CharacterCache.Instance.RemoveEntity(session.member_no, find_character);
                var characters = await CharacterCache.Instance.GetEntities(session.member_no, session.user_no, true);
                if (characters.Count > 0)
                {
                    await UserCache.UpdateUserLock(session, session.user_no, characters[0].character_no);
                    await session.UpdateSessionLock(characters[0].character_type, session.map_id, false);
                }
            }
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> InsertCharacter(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var CharacterId = int.Parse(Param1);
            await CharacterManager.InsertCharacter(session.member_no, session.user_no, session.user_name, session.player_id, CharacterId);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> ResetShop(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var shops = await ShopCache.Instance.GetEntities(session.member_no, session.user_no, true);
            foreach (var shop in shops)
            {
                await ShopQuery.Remove(session.member_no, shop);
            }
            await ShopCache.Instance.RemoveEntities(session.member_no, session.user_no);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> ResetMission(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var entities = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
            foreach (var entity in entities)
            {
                await MissionQuery.Remove(session.member_no, entity);
            }
            await MissionCache.Instance.RemoveEntities(session.member_no, session.user_no);
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetMissionProgress(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var MissionId = int.Parse(Param1);
            var MissionProgress = int.Parse(Param2);

            await MissionManager.ProgressAlter(session, MissionId, MissionProgress);

            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetCharacterPowerLevel(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var level = int.Parse(Param1);

            await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, true))
            {
                character.character_level = level;
            }
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SubShopDateTime(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var shopId = int.Parse(Param1);
            var sub_time = int.Parse(Param2);

            var shops = await ShopCache.Instance.GetEntities(session.member_no, session.user_no, true);
            foreach (var shop in shops)
            {
                if (shop.shop_id != shopId)
                    continue;

                shop.occ_time = shop.occ_time.AddSeconds(-1 * sub_time);

                await ShopCache.Instance.UpdateEntity(session.member_no, shop);
            }

            return (true, string.Empty);
        }


        public static async Task<(bool, string)> SetStartPlayerCount(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            RankMatchmaking.StartPlayerCount = int.Parse(Param1);

            return (true, string.Empty);
        }
        public static async Task<(bool, string)> SetCharacterBattleScore(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var cbs = int.Parse(Param1);

            await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, true))
            {
                character.battle_score = cbs;
            }
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetAccountBattleScore(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var abs = int.Parse(Param1);

            await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, true))
            {
                user.battle_score = abs;
            }
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SetMinimumStartPlay(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var minimumStartPlay = new MinimumStartPlay();
            minimumStartPlay.Enable = bool.Parse(Param1);
            minimumStartPlay.PlayerCount = int.Parse(Param2);
            minimumStartPlay.Timeout = int.Parse(Param3);
            int map_id = int.Parse(Param4);

            RankMatchmaking.minimumStartPlayMap[map_id] = minimumStartPlay;
            return (true, string.Empty);
        }

        public static async Task<(bool, string)> SendMail(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var receiverId = Param1;
            var title = Param4;
            var items = new List<send.request.MailItem>();
            items.Add(new send.request.MailItem()
            {
                itemCode = Param2,
                quantity = long.Parse(Param3),
                validityTime = DateTime.UtcNow.AddDays(1).ToEpochTime(),
            });

            await MailBox.SendMail(receiverId, title, title, items);
            return (true, string.Empty);
        }
        public static async Task<(bool, string)> GetMails(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var count = int.Parse(Param1);
            long? nextPageKey = null;
            if(string.Empty != Param2)
                nextPageKey = long.Parse(Param2);

            List<MailState> states = null;
            if (Param3 != string.Empty)
            {
                states = new List<MailState>();
                states.Add((MailState)System.Enum.Parse(typeof(MailState), Param3));
            }

            var ret = await MailBox.GetMails(session.member_no, session.user_no, count, nextPageKey, states);

            return (true, ret);
        }

        public static async Task<(bool, string)> GetMailItems(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var items = new List<string>();
            if (Param1 != string.Empty)
                items.Add(Param1);
            if (Param2 != string.Empty)
                items.Add(Param2);
            if (Param3 != string.Empty)
                items.Add(Param3);

            int characterId = 0;
            if (Param4 != string.Empty)
                characterId =int.Parse(Param4);

            await MailBox.GetMailItems(session, items, characterId);

            return (true, string.Empty);
        }

        public static async Task<(bool, string)> AddRank(Session session, string Param1, string Param2, string Param3, string Param4)
        {
            var ret = await Ranking.Accumulate(session.player_id, new score.accumulate.request.msg()
            {
                leaderboardId = Param1,
                delta = int.Parse(Param2),
                subkey = Param3,
            });

            return (true, ret.ToString());
        }
    }
}