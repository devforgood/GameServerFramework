using core;
using Microsoft.Extensions.Configuration;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class GameSetting
    {
        public float mTimeBetweenStatePackets = 0.06f;
        public bool EnableAiSwitch = false;
    }


    public class ServerConfiguration
    {
        /// <summary>
        /// Get global instance of Cache
        /// </summary>
        public static readonly ServerConfiguration Instance = new ServerConfiguration();

        public IConfiguration config;
        public string appsettingsFilename = "appsettings.json";

        public GameSetting game_setting = new GameSetting();
        public float ConnectionTimeout;
        public bool IsPermitDebugUser;
        public bool EnableDebugCommand;
        public bool channel_update;



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

            DeliveryNotificationManager.MinDelayBeforeAckTimeout = Convert.ToSingle(config["game_setting:MinDelayBeforeAckTimeout"]);
            DeliveryNotificationManager.MaxDelayBeforeAckTimeout = Convert.ToSingle(config["game_setting:MaxDelayBeforeAckTimeout"]);

            game_setting.mTimeBetweenStatePackets = Convert.ToSingle(config["game_setting:TimeBetweenStatePackets"]);
            game_setting.EnableAiSwitch = Convert.ToBoolean(config["game_setting:EnableAiSwitch"]);

            // game mode
            GameMode.EndAble = Convert.ToBoolean(config["game_setting:GameMode:EndAble"]);
            GameMode.InitTimeout = Convert.ToSingle(config["game_setting:GameMode:InitTimeout"]);
            GameMode.ReadyTimeout = Convert.ToSingle(config["game_setting:GameMode:ReadyTimeout"]);
            GameMode.ExpiredTime = Convert.ToSingle(config["game_setting:GameMode:ExpiredTime"]);

            ConnectionTimeout = Convert.ToSingle(config["ConnectionTimeout"]);

            IsPermitDebugUser = Convert.ToBoolean(config["permit_debug_user"]);

            EnableDebugCommand = Convert.ToBoolean(config["EnableDebugCommand"]);
            channel_update = Convert.ToBoolean(config["channel_update"]);

        }
    }
}
