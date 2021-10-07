using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public struct GameSetting
    {
        public bool EnableAIMatch;
        public double AIMatchTime;
        public bool EnableReJoin;
        public bool MatchForce;
    }

    public class ServerConfiguration
    {
        /// <summary>
        /// Get global instance of Cache
        /// </summary>
        public static readonly ServerConfiguration Instance = new ServerConfiguration();

        public IConfiguration config;
        public string appsettingsFilename = "appsettings.json";


        private ServerConfiguration()
        {
            Init(appsettingsFilename);
        }
        public void WriteLog()
        {
            foreach (var conf in config.AsEnumerable())
            {
                Log.Information($"{conf.Key}:{conf.Value}");
            }
        }
        public void Init(string filename)
        {
            config = new ConfigurationBuilder().AddJsonFile(filename, true, true).Build();

        }
    }
}
