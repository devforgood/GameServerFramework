using Newtonsoft.Json;
using Serilog;
using Server;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ServerCommon
{
    public class ChannelUpdater
    {

        /// <summary>
        /// Global instance of ChannelUpdater
        /// </summary>
        public static ChannelUpdater Instance = new ChannelUpdater();

        public TimeSpan channel_info_expire = new TimeSpan(0, 1, 0);

        public ServerCommon.Channel[] channel_list;
        int mapId;
        bool init = false;

        public void Init(byte channel_count, string server_addr, int map_id)
        {
            mapId = map_id;

            channel_list = new ServerCommon.Channel[channel_count];


            var db = Cache.Instance.GetDatabase();

            for (byte i = 0; i < channel_count; ++i)
            {
                channel_list[i] = new ServerCommon.Channel
                {
                    channel_id = $"channel:{db.StringIncrement("channel_instance_id")}",
                    channel_state = ServerCommon.ChannelState.CHL_READY,
                    server_addr = server_addr,
                    world_id = i,
                    submit_time = DateTime.UtcNow,
                    map_id = map_id
                };

                db.HashSet($"channel_info:{mapId}", $"{server_addr}:{i}", JsonConvert.SerializeObject(channel_list[i]));
            }

            //server_info.server_addr = server_addr;
            //server_info.server_name = server_name;
            //server_info.server_id = server_name + ":" + Convert.ToString(db.StringIncrement(ServerCommon.ServerInfoRedisKey.server_instance_id));

            //db.HashSet("server_info", server_info.server_addr, JsonConvert.SerializeObject(server_info));

            init = true;
        }


        public async Task Update()
        {
            try
            {
                // channel info update
                var db = Cache.Instance.GetDatabase();
                for (byte i = 0; i < channel_list.Length; ++i)
                {
                    channel_list[i].submit_time = DateTime.UtcNow;
                    await db.StringSetAsync(channel_list[i].channel_id, (int)channel_list[i].channel_state, channel_info_expire);
                    await db.HashSetAsync($"channel_info:{mapId}", $"{ channel_list[i].server_addr}:{channel_list[i].world_id}", JsonConvert.SerializeObject(channel_list[i]));
                }

                // server info update
                //db.StringSet(server_info.server_id, NetworkManagerServer.sInstance.GetPlayerCount(), server_info_expire);
            }
            catch (Exception ex)
            {
                Log.Information(ex.ToString());
            }
        }


        public bool AddUser(byte world_id)
        {
            if (init == false)
                return true;

            if (channel_list.Length <= world_id)
                return false;

            ++channel_list[world_id].user_count;

            if (channel_list[world_id].user_count > 0)
            {
                if (channel_list[world_id].channel_state == ServerCommon.ChannelState.CHL_READY)
                    channel_list[world_id].channel_state = ServerCommon.ChannelState.CHL_BUSY;
            }

            return true;
        }

        public int DelUser(byte world_id)
        {
            if (init == false)
                return 0;

            --channel_list[world_id].user_count;

            if (channel_list[world_id].user_count == 0)
            {
                if (channel_list[world_id].channel_state == ServerCommon.ChannelState.CHL_BUSY)
                    channel_list[world_id].channel_state = ServerCommon.ChannelState.CHL_READY;
            }

            return channel_list[world_id].user_count;
        }

        public void Suspend()
        {
            for (int i = 0; i < channel_list.Length; ++i)
            {
                channel_list[i].channel_state = ServerCommon.ChannelState.CHL_SUSPEND;
            }
        }

        public void Resume()
        {
            for (int i = 0; i < channel_list.Length; ++i)
            {
                if (channel_list[i].channel_state == ServerCommon.ChannelState.CHL_SUSPEND)
                    channel_list[i].channel_state = ServerCommon.ChannelState.CHL_READY;
            }
        }
    }
}
