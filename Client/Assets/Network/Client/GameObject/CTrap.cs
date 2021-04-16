using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CTrap : core.Trap
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CTrap(); }

    protected CTrap()
    {

    }

    public override void CompleteCreate()
    {
        Debug.Log($"create trap {GetNetworkId()}");


    }

    public override void HandleDying()
    {
        base.HandleDying();

        Debug.Log($"remove trap {GetNetworkId()}");

    }



}
