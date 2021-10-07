using ServerCommon;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    public class CacheThread
    {
        public static void Run()
        {
            Task.Run(async () =>
            {
                while(true)
                {
                    await ChannelUpdater.Instance.Update();
                    await Task.Delay(10000);
                }
            });
        }
    }
}
