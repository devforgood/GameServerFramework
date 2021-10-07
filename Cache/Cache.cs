using core;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Cache
    {
        /// <summary>
        /// Global instance of ChannelMgr
        /// </summary>
        public static Cache Instance = new Cache();


        public ConnectionMultiplexer cache = null;

        public string Address;

        public ISubscriber GetSubscriber()
        {
            return cache.GetSubscriber();
        }

        public IDatabase GetDatabase()
        {
            return cache.GetDatabase();
        }

        public ConnectionMultiplexer GetConnection()
        {
            return cache;
        }

        public void Init(string cache_server_addr)
        {
            Address = cache_server_addr;
            cache = ConnectionMultiplexer.Connect(cache_server_addr);
        }
    }
}
