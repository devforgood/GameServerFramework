using Lidgren.Network;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class Subscribe
    {
        public static void Do(NetServer svr)
        {
            for (int i = 0; i < Cache.sInstance.channel_list.Length; ++i)
            {
                string subscript_key = $"channel_msg:{Cache.sInstance.channel_list[i].channel_id}";
                RegisterSubscribe(subscript_key, svr);
            }
        }

        public static void RegisterSubscribe(string subscript_key, NetServer svr)
        {
            try
            {
                Log.Information("sub {0}", subscript_key);
                ISubscriber sub = Cache.sInstance.cache.GetSubscriber();
                sub.Subscribe(subscript_key, (channel, message) =>
                {
                    try
                    {
                        Log.Information($"redis msg {message}");
                        var msg = svr.CreateMessage();
                        msg.Write((string)message);
                        svr.SendUnconnectedToSelf(msg, true);
                    }
                    catch(Exception ex)
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
