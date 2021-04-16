using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    class Channel
    {
        private static TimeSpan channel_reserve_expire = new TimeSpan(0, 1, 0);
        public static bool HasServerFilter = Convert.ToBoolean(ServerConfiguration.Instance.config["HasServerFilter"]);

        public static async Task<(bool, string, byte, string, string)> GetAvailableServer(int mapId)
        {
            var db = Cache.Instance.GetDatabase();
            var entry = await db.HashGetAllAsync($"channel_info:{mapId}");
            for (int i = 0; i < entry.Length; ++i)
            {
                var ch = JsonConvert.DeserializeObject<ServerCommon.Channel>(entry[i].Value);

                // ip filter
                if(HasServerFilter)
                {
                    if (ch.server_addr != ServerConfiguration.Instance.config["ServerFilter"])
                        continue;
                }

                var channel_state = await db.StringGetAsync(ch.channel_id);
                Log.Information($"channel state {ch.channel_id}, {channel_state.HasValue}, {channel_state}");

                if (channel_state.HasValue == true && ((ServerCommon.ChannelState)((int)channel_state)) == ServerCommon.ChannelState.CHL_READY)
                {
                    // 해당 체널에 예약이 성공했을 경우만 처리
                    if (await db.StringSetAsync($"{ch.channel_id}:reserve", 0, channel_reserve_expire, When.NotExists) == true)
                    {
                        Log.Information($"GetAvailableServer {ch.channel_id}, { channel_state}, {entry[i].Name}");
                        return (true, ch.server_addr, ch.world_id, entry[i].Name, ch.channel_id);
                    }
                }
            }
            return (false, "", 0, "", "");
        }
    }
}
