using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class PlayerInfoLog
    {
        public PlayerLog player { get; set; }
    }

    public class PlayerLog
    {
        private static readonly TimeSpan player_expire = new TimeSpan(1, 0, 0);

        public long grade { get; set; }
        public string country { get; set; }
        public string os { get; set; }
        //public bool deviceLogin { get; set; }
        //public string appStatus { get; set; }
        public string market { get; set; }
        //public long regTime { get; set; }
        //public long modTime { get; set; }
        //public string appId { get; set; }
        public string lang { get; set; }
        //public object customProperty { get; set; }
        //public string playerId { get; set; }
        //public string idpCode { get; set; }

        public PlayerLog(PlayerLog other)
        {
            if (other != null)
            {
                Copy(other);
            }
        }

        public void Copy(PlayerLog other)
        {
            grade = other.grade;
            country = other.country;
            os = other.os;
            market = other.market;
            lang = other.lang;
        }

        public static async Task<PlayerLog> GetPlayerInfo(string player_id, long member_no, long user_no)
        {
            player_id = "990252821976346"; // todo : 테스트용 코드 차후 삭제 필요

            PlayerLog player = null;
            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"player_info:{player_id}");
            if (ret.HasValue == true)
            {
                await Cache.Instance.GetDatabase().KeyExpireAsync($"player_info:{player_id}", player_expire);
                player = JsonConvert.DeserializeObject<PlayerLog>(ret);
            }
            else
            {
                player = await WebAPIClient.Web.getInfo(player_id);
                if(player == null)
                {
                    Log.Error($"GetPlayerInfo {player_id}");
                    return null;
                }
                await Cache.Instance.GetDatabase().StringSetAsync($"player_info:{player_id}", JsonConvert.SerializeObject(player), player_expire);
            }

            var user = await UserCache.GetUser(member_no, user_no, true);
            if(user.user_grade == 0)
                player.grade = 1;
            else
                player.grade = user.user_grade;

            return player;
        }

        public static async Task<PlayerLog> GetPlayerInfo(Session session)
        {
            return await GetPlayerInfo(session.player_id, session.member_no, session.user_no);
        }
    }
}
