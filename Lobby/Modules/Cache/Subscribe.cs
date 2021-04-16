using Lidgren.Network;
using Lobby;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lobby
{
    public class Subscribe
    {

        public static void RegisterSubscribe(string subscript_key)
        {
            try
            {
                Log.Information("sub {0}", subscript_key);
                ISubscriber sub = Cache.Instance.GetSubscriber();
                sub.Subscribe(subscript_key, async (channel, message) =>
                {
                    try
                    {
                        Log.Information($"redis msg {message}");
                        var request = JsonConvert.DeserializeObject<ServerCommon.DebugCommand>(message);
                        JDebugCommandData commandData =  DebugCommandData.dataMap.Values.Where(x => x.Command == request.cmd).FirstOrDefault();
                        if(commandData == null || commandData == default(JDebugCommandData))
                        {
                            Log.Information($"not exist received msg {request.cmd}");
                            return;
                        }

                        if (commandData.Apply == "all") // 모든 서버가 실행이 되야 할 경우
                        {
                            // 디버그 커맨드 실행
                            await Lobby.DebugCommand.Execute(null, request.cmd, request.param1, request.param2, request.param3, request.param4);
                        }
                        else
                        {
                            // 한서버에서만 처리되도록 수정
                            var ret = await Cache.Instance.GetDatabase().StringSetAsync($"lobby_msg_lock:{request.msg_id}", 0, new TimeSpan(0, 3, 0), When.NotExists);
                            if (ret == false)
                            {
                                Log.Information("already receive msg");
                                return;
                            }

                            //  세션 찾기 및 세션 만들기
                            var (session, err, player_id) = await Session.LoginSession("", request.player_id, "", false, null);
                            if (session == null)
                            {
                                Log.Error("login {0}", request.player_id);
                                return;
                            }

                            // 디버그 커맨드 실행
                            await Lobby.DebugCommand.Execute(session, request.cmd, request.param1, request.param2, request.param3, request.param4);
                        }

                    }
                    catch (Exception ex)
                    {
                        Log.Error($"redis sub callback error {ex.ToString()}");
                    }
                });
            }
            catch(Exception ex)
            {
                Log.Error($"redis sub error {ex.ToString()}");
            }

        }

    }
}
