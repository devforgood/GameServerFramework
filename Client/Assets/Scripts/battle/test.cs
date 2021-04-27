using Assets.Network.Lobby;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;

public class test : MonoBehaviour
{
    async Task Start()
    {
        //NetworkController.Instance.Login();

        CActor.FuncCreateGameObject = () =>
        {
            var obj = (GameObject)Instantiate(Resources.Load("Character"), new Vector3(0, 0, 0), Quaternion.identity);
            return obj.GetComponent<ACBattleCharacter>();
        };


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

}
