using core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uint32_t = System.UInt32;
using uint16_t = System.UInt16;
using StackExchange.Redis;
using Serilog;
using System.Diagnostics;

namespace Server
{
    public class Server : Engine
    {
        public static bool StaticInit(uint16_t port, byte world_count, int map_id, bool is_battle_auth)
        {
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            JsonData.Instance.LoadOriginalData();
            JsonData.Instance.LoadData(true, false);
            var mapData = ACDC.MapData[map_id];
            sInstance = new Server(port, world_count);
            if (!World.StaticInit(mapData.ResourceDataPath))
                return false;
            World.InitServer(world_count, 0, NetworkManagerServer.sInstance);
            World.Map = mapData;
            NetworkManagerServer.sInstance.IsBattleAuth = is_battle_auth;
            NetworkManagerServer.sInstance.IsPermitDebugUser = ServerConfiguration.Instance.IsPermitDebugUser;
            SActor.EnableDebugCommand =  ServerConfiguration.Instance.EnableDebugCommand;
            PlayerController.EnableAiSwitch = ServerConfiguration.Instance.game_setting.EnableAiSwitch;
            PlayerController.mTimeBetweenStatePackets = ServerConfiguration.Instance.game_setting.mTimeBetweenStatePackets;

            stopWatch.Stop();
            Log.Information($"Data Loading Time : {stopWatch.Elapsed.TotalSeconds}sec");

            return true;
        }

        public override void DoFrame()
        {
            NetworkManagerServer.sInstance.ProcessIncomingPackets();

            //NetworkManagerServer.sInstance.CheckForDisconnects();

            NetworkManagerServer.sInstance.UpdatePlayer();

            base.DoFrame();

 
            World.LateUpdate();

            NetworkManagerServer.sInstance.SendOutgoingPackets();
#if USE_STATISTICS
            NetworkManagerServer.sInstance.ShowStatistics();
#endif
        }

        public override int Run()
        {
            return base.Run();
        }

        Server(uint16_t port, byte worldCount)
        {
            GameObjectRegistry.sInstance.RegisterAll(System.Reflection.Assembly.GetExecutingAssembly(), "Server.S", true);

            InitNetworkManager(port, worldCount);

            //NetworkManagerServer::sInstance->SetDropPacketChance( 0.8f );
            //NetworkManagerServer::sInstance->SetSimulatedLatency( 0.25f );
            //NetworkManagerServer::sInstance->SetSimulatedLatency( 0.5f );
            //NetworkManagerServer::sInstance->SetSimulatedLatency( 0.1f );
            IsClient = false;
            IsServer = true;
        }

        bool InitNetworkManager(uint16_t port, byte worldCount)
        {
            return NetworkManagerServer.StaticInit(port, worldCount, ServerConfiguration.Instance.ConnectionTimeout);
        }
    }
}
