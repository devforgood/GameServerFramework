using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using GameService;

namespace Lobby
{

    // redis 
    //zincrby: 지정 멤버에 일정 점수를 더한다(실습에서는 하나씩 세기위해 1씩 더합니다)
    //zscore: 지정 멤버의 현재 점수를 구한다
    //zrevrank: 지정 멤버의 랭킹을 조회한다(점수가 높을수록 1위에 가깝다)
    //zrevrangebyscore: 상위 랭킹 멤버들을 조회한다



    public class Ranking
    {
        public const string ABSLeaderBoardId = "AccountBattleScore";
        public const string CBSLeaderBoardId = "CharacterBattleScore";

        public static async Task<(long, long, long)> Update(Session session, long season_no, int point, bool isWin)
        {
            long abs = 0, cbs = 0, wc = 0;
            if (point != 0)
            {
                abs = await ABSAccumulate(session, season_no, point);
                cbs = await CBSAccumulate(session, season_no, point);
            }
            if (isWin)
            {
               wc = await WCAccumulate(session, season_no);
            }
            return (abs, cbs, wc);
        }

        public static async Task<getInfo.response.msg> GetInfo(LeaderboardInfo leaderboard)
        {
            var msg = new getInfo.request.msg();
            msg.leaderboardId = leaderboard.Id;
            msg.seasonSeq = leaderboard.SeasonSeq;

            string response = await WebAPIClient.Web.request("", "/leaderboard/getInfo", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                return null;
            }
            var responseMsg = JsonConvert.DeserializeObject<getInfo.response.msg>(response);
            return responseMsg;
        }

        public static async Task<bool> PutProperty(string player_id, string subKey, Dictionary<string, object> properties)
        {
            var msg = new putProperty.request.msg();
            msg.property = properties;
            msg.subKey = subKey;

            string response = await WebAPIClient.Web.request(player_id, "/leaderboard/putProperty", JsonConvert.SerializeObject(msg));
            if(response == string.Empty)
            {
                return false;
            }
            return true;
        }

        public static async Task<rank.get.response.msg> GetRank(string player_id, LeaderboardInfo leaderboard, int characterId)
        {
            var msg = new rank.get.request.msg();
            msg.leaderboardId = leaderboard.Id;
            msg.seasonSeq = leaderboard.SeasonSeq;
            msg.subkey = characterId.ToString();

            string response = await WebAPIClient.Web.request(player_id, "/leaderboard/rank/get", JsonConvert.SerializeObject(msg));
            if(response == string.Empty)
            {
                return null;
            }

            var responseMsg = JsonConvert.DeserializeObject<rank.get.response.msg>(response);
            return responseMsg;
        }

        public static async Task<rank.getList.response.msg> GetRankList(string player_id, LeaderboardInfo leaderboard, int from, int to, bool readMy, int characterId, bool withoutProperty)
        {
            var msg = new rank.getList.request.msg();
            msg.leaderboardId = leaderboard.Id;
            msg.seasonSeq = leaderboard.SeasonSeq;
            msg.fromRank = from;
            msg.toRank = to;
            msg.withoutProperty = withoutProperty;
            msg.subkey = characterId.ToString();
            msg.playerId = readMy ? player_id : string.Empty;
            msg.cacheProperty = new rank.getList.request.CacheProperty() { ttlSec = 10 };

            string response = await WebAPIClient.Web.request("", "/leaderboard/rank/getList", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                return null;
            }

            var responseMsg = JsonConvert.DeserializeObject<rank.getList.response.msg>(response);
            return responseMsg;
        }

        public static async Task<score.getList.response.msg> GetScoreList(LeaderboardInfo leaderboard, bool withoutProperty, List<string> player_ids)
        {
            var msg = new score.getList.request.msg();
            msg.leaderboardId = leaderboard.Id;
            msg.seasonSeq = leaderboard.SeasonSeq;
            msg.playerIds = player_ids;

            string response = await WebAPIClient.Web.request("", "/leaderboard/score/getList", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                return null;
            }

            var responseMsg = JsonConvert.DeserializeObject<score.getList.response.msg>(response);
            return responseMsg;
        }

        public static async Task<long> Accumulate(string player_id, score.accumulate.request.msg msg)
        {
            //zincrby: 지정 멤버에 일정 점수를 더한다(실습에서는 하나씩 세기위해 1씩 더합니다)

            //Cache.Instance.GetDatabase().SortedSetIncrementAsync("")



            string response = await WebAPIClient.Web.request(player_id, "/leaderboard/score/accumulate", JsonConvert.SerializeObject(msg));
            if (response == string.Empty)
            {
                return 0;
            }
            var responseMsg = JsonConvert.DeserializeObject<score.accumulate.response.msg>(response);
            return responseMsg.afterScore;
        }

        static async Task<long> ABSAccumulate(Session session, long season_no, int point)
        {
            var msg = new score.accumulate.request.msg();
            msg.leaderboardId = ABSLeaderBoardId;
            msg.delta = point;

            return await Accumulate(session.player_id, msg);
        }

        static async Task<long> CBSAccumulate(Session session, long season_no, int point)
        {
            var msg = new score.accumulate.request.msg();
            msg.leaderboardId = CBSLeaderBoardId;
            msg.delta = point;
            msg.subkey = session.character_type.ToString();

            return await Accumulate(session.player_id, msg);
        }

        static async Task<long> WCAccumulate(Session session, long season_no)
        {
            var msg = new score.accumulate.request.msg();
            msg.leaderboardId = ACDC.GameModeData[ACDC.MapData[session.map_id].GameMode].LeaderboardId;
            msg.delta = 1;
            msg.subkey = session.character_type.ToString();

            return await Accumulate(session.player_id, msg);
        }
    }
}
