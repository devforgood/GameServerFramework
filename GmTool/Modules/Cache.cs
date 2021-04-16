using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmTool
{
    public class Cache
    {
        public static string RedisIpAddress;
        public static string RedisPort;

        /// <summary>
        /// Get global instance of Cache
        /// </summary>
        private static Cache _Instance = null;
        public ConnectionMultiplexer redis;

        public static Cache Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new Cache();
                }
                return _Instance;
            }
        }

        Cache()
        {
            redis = ConnectionMultiplexer.Connect($"{RedisIpAddress }:{RedisPort}");
        }


        ~Cache()
        {
            redis.Dispose();
        }

        public ConnectionMultiplexer GetConnection()
        {
            return redis;
        }

        public ISubscriber GetSubscriber()
        {
            return redis.GetSubscriber();
        }

        public IDatabase GetDatabase()
        {
            return redis.GetDatabase();
        }

        public IServer GetServer()
        {
            return redis.GetServer($"{RedisIpAddress }:{RedisPort}");
        }
    }
}
