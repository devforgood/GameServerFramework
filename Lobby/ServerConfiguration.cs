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

        public string CommonContext { get; private set; }
        public string [] GameContext { get; private set; }

        public string [] LogContext { get; private set; }

        public GameSetting gameSetting;
        public bool EnableCheckAccessToken;
        public bool EnableDebugCommand;

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
            CommonContext = config["ConnectionStrings:CommonContext"];
            GameContext = config.GetSection("ConnectionStrings:GameContext").Get<string[]>();
            LogContext = config.GetSection("ConnectionStrings:LogContext").Get<string[]>();

            gameSetting.EnableAIMatch = bool.Parse(config["GameSetting:EnableAIMatch"]);
            gameSetting.AIMatchTime = double.Parse(config["GameSetting:AIMatchTime"]);
            gameSetting.EnableReJoin = bool.Parse(config["GameSetting:EnableReJoin"]);
            gameSetting.MatchForce = bool.Parse(config["GameSetting:MatchForce"]);

            EnableCheckAccessToken = bool.Parse(config["EnableCheckAccessToken"]);
            EnableDebugCommand = bool.Parse(config["EnableDebugCommand"]);
        }
    }
}
