using Newtonsoft.Json;
using Server;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon
{
    public class ChannelLoader
    {
        private static TimeSpan channel_reserve_expire = new TimeSpan(0, 1, 0);
        private static int MaxUserCount = 8;

        public static async Task<(bool, string, byte, string, string)> GetAvailableServer(int mapId)
        {
            var db = Cache.Instance.GetDatabase();
            var entry = await db.HashGetAllAsync($"channel_info:{mapId}");
            for (int i = 0; i < entry.Length; ++i)
            {
                var ch = JsonConvert.DeserializeObject<ServerCommon.Channel>(entry[i].Value);

                // 해당 채널 활성화 유무 체크
                var channel_state = await db.StringGetAsync(ch.channel_id);
                if (channel_state.HasValue == true && ((ServerCommon.ChannelState)((int)channel_state)) != ServerCommon.ChannelState.CHL_SUSPEND
                    && ch.user_count < MaxUserCount)
                {
                    // 입장중인 유저의 카운트가 채널 입장이 완료 될때까지 갱신전이므로
                    // 채널 유저 카운트를 키로 예약을 걸어놓는다.
                    for (int UserNum = ch.user_count; UserNum < MaxUserCount; ++UserNum)
                    {
                        // 해당 채널에 예약이 성공했을 경우만 처리
                        if (await db.StringSetAsync($"{ch.channel_id}:reserve:{UserNum}", 0, channel_reserve_expire, When.NotExists) == true)
                        {
                            return (true, ch.server_addr, ch.world_id, entry[i].Name, ch.channel_id);
                        }
                    }
                }
            }
            return (false, "", 0, "", "");
        }
    }
}
