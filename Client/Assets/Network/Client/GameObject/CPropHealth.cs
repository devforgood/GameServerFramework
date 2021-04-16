using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CPropHealth : core.PropHealth
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CPropHealth(); }
    JMapObjectData mapData;
    //public GameObject mTarget = null;

    protected CPropHealth()
    {

    }

    public override void CompleteCreate()
    {
        Debug.Log($"create PropHealth {GetNetworkId()}, {mapObjectId}");

        if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
        {
            Debug.Log($"cannot find map object {mapObjectId}");
            return;
        }
        Set(mapData);
    }
    public override void HandleDying()
    {
        Debug.Log($"remove PropHealth {GetNetworkId()}, {mapObjectId}");

        // todo : HP가 0이 되었을때 처리

        base.HandleDying();
    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        if ((readState & (UInt32)ReplicationState.Health) != 0)
        {
            // todo : HP가 변경 되었을때 처리
            //GameObject mapDestroyGo = MapManager.GetMapObject(mapObjectId);
            //if (mapDestroyGo != null)
            //{
            //    mapDestroyGo.GetComponent<MapDestroyProperty>()?.MapChangeHP(mHealth);
            //    mapDestroyGo.GetComponent<MapSwitchProperty>()?.MapChangeHP(mHealth);
            //}
        }
    }
}
