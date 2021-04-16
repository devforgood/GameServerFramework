using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CPropCooldown : core.PropCooldown
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CPropCooldown(); }
    //public GameObject mTarget = null;

    protected CPropCooldown()
    {

    }

    public override void CompleteCreate()
    {
        Debug.Log($"create cooldown {GetNetworkId()}, {mapObjectId}, remain_time:{RemainTime}");


        JMapObjectData mapData;
        if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
        {
            Debug.Log($"cannot find map object {mapObjectId}");
            return;
        }

        Set(mapData);

        if((mapData.mapTypes & (1 << (int)MapPropertyType.Jump)) != 0)
        {
            //GameObject mapJumpGo = MapManager.GetMapObject(mapObjectId);
            //if (mapJumpGo != null)
            //{
            //    mapJumpGo.GetComponent<MapJumpProperty>().PlayJumpAni();   //점프대 점프 애니.
            //    SoundManager.Instance.Play((int)EAInGameSoundID.JUMP_SHOOT, mapJumpGo.transform.position);
            //}
            
        }
    }
    public override void HandleDying()
    {
        Debug.Log($"remove cooldown {GetNetworkId()}, {mapObjectId}");
        //GameObject mapJumpGo = MapManager.GetMapObject(mapObjectId);
        //mapJumpGo.GetComponentInChildren<SkinnedMeshRenderer>()?.materials[1]?.SetFloat("_Dimming", 1);
        //base.HandleDying();
    }
}
