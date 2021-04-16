using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmTool
{
    public class DebugCommand
    {
        public static async Task SendMessage(string playerId, string Cmd, string Param1, string Param2, string Param3, string Param4, ILogger logger)
        {
            var msg = new ServerCommon.DebugCommand();
            msg.player_id = playerId;
            msg.cmd = Cmd;
            msg.param1 = Param1;
            msg.param2 = Param2;
            msg.param3 = Param3;
            msg.param4 = Param4;

            logger.LogInformation($" redis {Cache.RedisIpAddress}, {Cache.RedisPort}, {Cache.Instance.GetDatabase().IsConnected(default(RedisKey))}, {Cache.Instance.GetConnection().IsConnected}, v1.0.3 ");
            msg.msg_id = (long)await Cache.Instance.GetDatabase().StringIncrementAsync("lobby_msg_instance_id");
            await Cache.Instance.GetSubscriber().PublishAsync($"lobby", JsonConvert.SerializeObject(msg));
        }

        public static async Task SendMessageToBattle(string playerId, string Cmd, string Param1, string Param2, string Param3, string Param4, ILogger logger)
        {
            var msg = new ServerCommon.InternalMessage();
            msg.message_type = (byte)ServerCommon.InternalMessageType.DebugCommand;
            msg.debug_command = new ServerCommon.DebugCommand();
            msg.debug_command.player_id = playerId;
            msg.debug_command.cmd = Cmd;
            msg.debug_command.param1 = Param1;
            msg.debug_command.param2 = Param2;
            msg.debug_command.param3 = Param3;
            msg.debug_command.param4 = Param4;


            logger.LogInformation($" redis {Cache.RedisIpAddress}, {Cache.RedisPort}, {Cache.Instance.GetDatabase().IsConnected(default(RedisKey))}, {Cache.Instance.GetConnection().IsConnected}, v1.0.3 ");
            var session_id = (string)await Cache.Instance.GetDatabase().StringGetAsync($"player_id:{playerId}");
            var player = await Cache.Instance.GetDatabase().StringGetAsync($"player:{session_id}");
            var player_location = JsonConvert.DeserializeObject<ServerCommon.PlayerLocation>(player);

            msg.world_id = player_location.world_id;
            msg.debug_command.ingame_player_id = player_location.player_id;


            var pubMessage = JsonConvert.SerializeObject(msg);
            await Cache.Instance.GetSubscriber().PublishAsync($"channel_msg:{player_location.channel_id}", pubMessage);
        }

        public static async Task<bool> Execute(string target, string playerId, string Cmd, string Param1, string Param2, string Param3, string Param4, ILogger logger)
        {

            if (target == "Lobby")
            {
                await SendMessage(playerId, Cmd, Param1, Param2, Param3, Param4, logger);
            }
            else if(target == "Battle")
            {
                await SendMessageToBattle(playerId, Cmd, Param1, Param2, Param3, Param4, logger);
            }

            return true;
        }
    }
}
