using Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class GameHost : MonoSingleton<GameHost>
    {



        public bool Init(int map_id, byte world_id = 1)
        {
            core.LogHelper.LogInfo($"GameHost.Init {map_id}, {world_id}");

            //World.StaticInit(ACDataStorage.MAP[map_id].ResourceDataPath);

            Engine.sInstance.IsServer = true;
            NetworkManagerServer.StaticInit(65001, 2, 25);

            GameObjectRegistry.sInstance.IsHost = true;
            GameObjectRegistry.sInstance.RegisterAll(System.Reflection.Assembly.GetExecutingAssembly(), "Server.S", true);

            JsonData.Instance.LoadOriginalData();
            JsonData.Instance.LoadData(true, false);

            //Instance = new GameHost();

            //if (!World.StaticInit(mapData.ResourceDataPath))
            //    return false;
            World.Map = ACDC.MapData[map_id];
            NetworkManagerServer.sInstance.IsBattleAuth = false;
            NetworkManagerServer.sInstance.IsPermitDebugUser = true;
            SActor.EnableDebugCommand = true;
            PlayerController.EnableAiSwitch = false;
            PlayerController.mTimeBetweenStatePackets = 30f;

            GameMode.ReadyTimeout = 3;

            NetworkManagerServer.sInstance.Clear(world_id);
            World.Instance(world_id).Reset(true, NetworkManagerServer.sInstance);
            World.Instance(world_id).GameMode.Init(map_id,  1, 1);
            //World.Instance(world_id).GameMode.game_mode.SetModeData(ret);
            //mNewPlayerId[world_id] = ret.players.Count + 1;

            return true;
        }

        void FixedUpdate()
        {
            NetworkManagerServer.sInstance.ProcessIncomingPackets();

            //NetworkManagerServer.sInstance.CheckForDisconnects();

            NetworkManagerServer.sInstance.UpdatePlayer();

            //World.Update();


            NetworkManagerServer.sInstance.SendOutgoingPackets();
#if USE_STATISTICS
            NetworkManagerServer.sInstance.ShowStatistics();
#endif
        }




        //protected override void OnCreate()
        //{

        //}

        //protected override void Shutdown()
        //{
        //}
    }
}
