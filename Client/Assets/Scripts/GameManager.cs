using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // Start is called before the first frame update
    async Task Start()
    {
        //SRDebug.Init();

        try
        {
            core.GameHost.Instance.Init(1);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }

        await Task.Delay(3000);

        try
        {
            Assets.Network.Lobby.NetworkController.Instance.PrepareJoin(1);

            await Assets.Network.Lobby.NetworkController.Instance.JoinBattle("127.0.0.1:65001", 1, Guid.NewGuid().ToString());
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
