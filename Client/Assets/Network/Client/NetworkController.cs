using core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Network.Lobby
{
    public class NetworkController : MonoSingleton<NetworkController>
    {
        Assets.Network.Lobby.GameWebRequest request = new Assets.Network.Lobby.GameWebRequest("https://localhost:44379");

        private CancellationTokenSource m_kCts = new CancellationTokenSource();
        public bool m_bIs_start = false;

        Action OnUpdateIngame = null;


        public void Login()
        {
            request.SendMessageAsync("Login/Index", null
                , (string reply) =>
            {
                Debug.Log(reply);
            });

        }

        public void PrepareJoin(int map_id = 1)
        {
            JsonData.Instance.LoadOriginalData();
            JsonData.Instance.LoadData(true, false);

            World.StaticInit(ACDC.MapData[map_id].ResourceDataPath);

            World a_kWorld = World.Instance(World.DefaultWorldIndex);
            var counts = a_kWorld.InitProb(World.DefaultWorldIndex, false, true);

            GameObjectRegistry.sInstance.RegisterAll(System.Reflection.Assembly.GetExecutingAssembly(), "C", false, true, counts);

            a_kWorld.InitializeWorld();
            a_kWorld.ClearByClient();

            World.InitStaticObject();
        }

        public async Task<bool> JoinBattle(string sServer_addr, int WorldId, string session_id, bool is_debug_user = false, string debug_user_name = "", byte debug_user_team = 0, int debug_player_id = 0)
        {
            Debug.Log($"try connect battle server {sServer_addr}, {WorldId}, {session_id}");
            try
            {
                if (m_bIs_start)
                {
                    Debug.LogWarning($"alread started");
                    return false;
                }

                System.Net.IPEndPoint addr = core.NetworkHelper.CreateIPEndPoint(sServer_addr);

                if (addr == null)
                {
                    return false;
                }

                NetworkManagerClient.StaticInit();
                NetworkManagerClient.sInstance.Init(addr, session_id, (byte)WorldId);
                NetworkManagerClient.sInstance.SetDebugUser(is_debug_user, debug_user_name, debug_user_team, debug_player_id);
                OnUpdateIngame = UpdateIngame;

                m_kCts = new CancellationTokenSource();

                while (NetworkManagerClient.sInstance.tryConnectCount <= 5)
                {
                    await Task.Delay(1000, m_kCts.Token);
                    if (NetworkManagerClient.sInstance.State == NetworkManagerClient.NetworkClientState.Welcomed)
                    {
                        m_bIs_start = true;

                        return true;
                    }
                }

                // udp 통신은 가능하나 타임아웃이라면 tcp를 시도해볼 것 없이 실패
                if (NetworkManagerClient.sInstance.IsUdpOk == false)
                {
                    Debug.Log("change TCP");
                    // tcp 연결 시도
                    NetworkManagerClient.sInstance.IsTcp = true;

                    // 3초간 응답 없으면 연결 실패
                    await Task.Delay(3000, m_kCts.Token);
                }


                // 서버 접속 제한 시간 초과로 인해 접속 실패...
                if (NetworkManagerClient.sInstance.State != NetworkManagerClient.NetworkClientState.Welcomed)
                {
                    NetworkManagerClient.sInstance.IsTrySend = false;
                    Debug.Log("start play failed");
                    return false;
                }

                m_bIs_start = true;

                return true;
            }
            catch (TaskCanceledException ex)
            {
                if (NetworkManagerClient.sInstance.State != NetworkManagerClient.NetworkClientState.Welcomed)
                {
                    NetworkManagerClient.sInstance.IsTrySend = false;
                    Debug.Log("start play failed");
                    OnUpdateIngame = null;
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.ToString());
            }
            OnUpdateIngame = null;
            return false;
        }

        void UpdateIngame()
        {
            Timing.sInstance.Update();
            InputManager.Instance.Update();
            Engine.sInstance.DoFrame();
            World.LateUpdate();

            // 클라이언트는 패킷 처리를 가장 나중에 한다 
            // 가장 마지막에 처리되는 것이 우선순위가 높음
            NetworkManagerClient.sInstance.ProcessIncomingPackets();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN

            InputManager.Instance.KeyEvent();

#endif

            NetworkManagerClient.sInstance.SendOutgoingPackets();
        }

        private void FixedUpdate()
        {
            // web
            request.Update();

            OnUpdateIngame?.Invoke();
        }

    }
}
