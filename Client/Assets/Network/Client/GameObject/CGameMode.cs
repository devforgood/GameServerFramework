using Lidgren.Network;
using UnityEngine;
using System.Collections.Generic;
using System;

public class CGameMode : core.GameMode
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CGameMode(); }


    protected CGameMode()
    {
        IsSetPlayTime = false;
    }

    public override void CompleteCreate()
    {
        Debug.Log($"create GameMode {GetNetworkId()}");

        core.World.Instance().GameMode = this;
    }




    public override void OnGameEnd()
    {
        try
        {


        }
        catch(Exception ex)
        {
            Debug.LogError(ex.ToString());
        }
    }

    public override void SwitchKing(core.Team team, int beforeKing, int currentKing)
    {


	}
}
