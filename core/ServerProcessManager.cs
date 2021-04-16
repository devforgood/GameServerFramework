using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;

namespace core
{
    public class ServerProcessManager
    {
        /// <summary>
        /// Global instance
        /// </summary>
        public static ServerProcessManager sInstance = new ServerProcessManager();

        public ConnectionMultiplexer cache = null;

        public void Init( string server_addr, string cache_server_addr)
        {
            cache = ConnectionMultiplexer.Connect(cache_server_addr);

            var db = cache.GetDatabase();
            var serverProcessInfo = new ServerProcess();
            serverProcessInfo.ProcessId = Process.GetCurrentProcess().Id;
            serverProcessInfo.ProcessName = Process.GetCurrentProcess().ProcessName;
            serverProcessInfo.WorkingDirectory = System.IO.Directory.GetCurrentDirectory();
            serverProcessInfo.ServerAddress = server_addr;
            serverProcessInfo.SubmitTime = DateTime.UtcNow;
           

            db.HashSet("server_info", $"{server_addr}", JsonConvert.SerializeObject(serverProcessInfo));
        }

        public async Task<List<ServerProcess>> GetServerProesss()
        {
            List<ServerProcess> serverProcesses = new List<ServerProcess>();
            var entry = await cache.GetDatabase().HashGetAllAsync("server_info");
            for (int i = 0; i < entry.Length; ++i)
            {
                serverProcesses.Add(JsonConvert.DeserializeObject<ServerProcess>(entry[i].Value));
            }
            return serverProcesses;

        }

    }
}
