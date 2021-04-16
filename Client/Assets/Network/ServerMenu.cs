using Assets.Network.Lobby;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using core;

#if UNITY_EDITOR
public class ServerMenu : ScriptableObject
{
    [MenuItem("Server/Run Local Server")]
    public static void RunLocalServer()
    {
        try
        {
            core.GameHost.Instance.Init(1);
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
    }

    [MenuItem("Server/Join Game Server")]
    public static async Task JoinGameServer()
    {
        try
        {
            NetworkController.Instance.PrepareJoin(1);

            await NetworkController.Instance.JoinBattle("127.0.0.1:65001", 1, Guid.NewGuid().ToString());
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
    }
}
#endif