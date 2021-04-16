using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    /// <summary>
    /// 매칭 유저의 고립성(isolation) 
    /// 매치와 유저 관계 보관 및 복구 (재접속시 이전 플레이 유지)
    /// </summary>
    public class MatchUser
    {
        private static TimeSpan match_user_expire = new TimeSpan(0, 5, 0);

        /// <summary>
        /// 매칭 유저를 획득 (선점), 이미 다른 유저가 획득했다면 실패
        /// </summary>
        /// <param name="user_no"></param>
        /// <param name="match_id"></param>
        /// <returns></returns>
        public static async Task<bool> OccupyMatchUser(long user_no, long match_id)
        {
            return await Cache.Instance.GetDatabase().StringSetAsync($"match_user:{user_no}", match_id, match_user_expire, When.NotExists);
        }

        public static async Task<(bool, long, TimeSpan?)> GetMatchUser(long user_no)
        {
            var value = await Cache.Instance.GetDatabase().StringGetAsync($"match_user:{user_no}");
            if (value.HasValue)
            {
                var timeToLive = await Cache.Instance.GetDatabase().KeyTimeToLiveAsync($"match_user:{user_no}");
                return (true, (long)value, timeToLive);
                //return (true, (long)value);
            }
            return (false, 0, null);
        }

        public static async Task RemoveMatchUser(long user_no)
        {
            core.LogHelper.LogInfo($"RemoveMatchUser {user_no}");
            //core.LogHelper.LogCallStack($"RemoveMatchUser {user_no}");
            await Cache.Instance.GetDatabase().KeyDeleteAsync($"match_user:{user_no}");
        }

        /// <summary>
        /// / 매칭 시도를 실패하면 선점한 플레이어를 삭제, 다른 매칭에 검색 되도록
        /// </summary>
        /// <param name="player_list"></param>
        /// <returns></returns>
        public static async Task CancelOccupiedMatchUser(List<long> player_list)
        {
            await Cache.Instance.GetDatabase().KeyDeleteAsync(player_list.Select(key => (RedisKey)$"match_user:{key}").ToArray());
        }

        public static async Task CancelOccupiedMatchUser(Dictionary<string, ServerCommon.PlayerInfo> players)
        {
            var player_list = players.Select(x => x.Value.user_no).ToList();
            await CancelOccupiedMatchUser(player_list);
        }
    }
}
