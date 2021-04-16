using core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace GmTool
{
    public class Ranking
    {
        static readonly TimeSpan expired_time = new TimeSpan(90, 0, 0, 0);
        public const string ABSLeaderboardId = "AccountBattleScore";
        public const string CBSLeaderboardId = "CharacterBattleScore";

        public static async Task ProcessRankingReward()
        {
            long time = DateTime.UtcNow.AddMinutes(-10).ToEpochTime();
            await _ProcessRankingReward(ABSLeaderboardId, time);
            await _ProcessRankingReward(CBSLeaderboardId, time);
            foreach (var gameMode in ACDC.GameModeData)
            {
                if (string.IsNullOrEmpty(gameMode.Value.LeaderboardId))
                    continue;

                await _ProcessRankingReward(gameMode.Value.LeaderboardId, time);
            }
        }

        public static async Task<getInfo.response.msg> GetInfo(string leaderboardId)
        {
            var msg = new getInfo.request.msg();
            msg.leaderboardId = leaderboardId;
            msg.seasonSeq = 0;

            var response = await WebAPIClient.Web.request("", "/leaderboard/getInfo", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
                return null;
            return JsonConvert.DeserializeObject<getInfo.response.msg>(response);
        }

        static async Task _ProcessRankingReward(string leaderboardId, long time)
        {
            if (string.IsNullOrEmpty(leaderboardId))
                return;
            
            var leaderboard = await GetInfo(leaderboardId);
            if (leaderboard != null)
            {
                string key = $"ranking_reward:{leaderboardId}:{leaderboard.seasonSeq}";
                bool isUpdated = false;
                await using (var myLock = await RedLock.CreateLockAsync(key))
                {
                    var ret = (bool)await Cache.Instance.GetDatabase().StringGetAsync(key);
                    if (ret)
                        return;

                    if (leaderboard.seasonEndTime < time)
                    {
                        await GiftReward(leaderboardId);
                        isUpdated = true;
                    }
                }
                if(isUpdated)
                {
                    await Cache.Instance.GetDatabase().StringSetAsync(key, true, expired_time);
                }
            }
        }

        static async Task GiftReward(string leaderboardId, int fromRank = 1, int toRank = 3000)
        {
            var msg = new rank.getList.request.msg();
            msg.leaderboardId = leaderboardId;
            msg.seasonSeq = 0;
            msg.fromRank = fromRank;
            msg.toRank = fromRank + toRank;
            string response = await WebAPIClient.Web.request("", "/leaderboard/rank/getList", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                return;
            }

            var responseMsg = JsonConvert.DeserializeObject<rank.getList.response.msg>(response);
            foreach(var score in responseMsg.scores)
            {
                int rank = score.rank;

                foreach(var reward in ACDC.RankingRewardData)
                {
                    if(leaderboardId == reward.Value.RankingID && reward.Value.RankingStart <= rank && rank <= reward.Value.RankingEnd)
                    {
                        string playerId = score.playerId.Split(':')[0];
                        List<send.request.MailItem> items = new List<send.request.MailItem>();
                        items.Add(new send.request.MailItem() { itemCode = reward.Value.GameItemID.ToString(), quantity = reward.Value.RewardCount});
                        if(false == await MailBox.SendMail(playerId, "Ranking reward", "reward", items))
                        {

                        }
                        break;
                    }
                }
            }

            if(msg.toRank < responseMsg.cardinality)
            {
                await GiftReward(leaderboardId, fromRank + toRank);
            }
        }
    }
}
