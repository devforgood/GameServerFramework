using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby
{
    public class Cache
    {
        /// <summary>
        /// Get global instance of Cache
        /// </summary>
        public static readonly Cache Instance = new Cache();
        public ConnectionMultiplexer redis;


        private Cache()
        {
            redis = ConnectionMultiplexer.Connect(ServerConfiguration.Instance.config["redis:ip"] + ":" + ServerConfiguration.Instance.config["redis:port"]);
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
            return redis.GetServer(ServerConfiguration.Instance.config["redis:ip"], UInt16.Parse(ServerConfiguration.Instance.config["redis:port"]));
        }
    }
}
