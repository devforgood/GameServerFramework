using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CItem : core.Item
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CItem(); }
    GameObject prefab = null;

    //public GameObject mTarget = null;

    protected CItem()
    {

    }

    public override void CompleteCreate()
    {
        Debug.Log($"create item {GetNetworkId()}, {ItemId}, {GetLocation()}");

        prefab = ItemManager.GetItemObject(ItemId);
        if(prefab)
        {
            //prefab.GetComponent<BaseGameItem>().networkId = GetNetworkId();
            //prefab.transform.position = GetLocation();
            //prefab.SetActive(true);
        }
    }

    public override void HandleDying()
    {
        base.HandleDying();

        Debug.Log($"remove item {GetNetworkId()}, {ItemId}, {GetLocation()}");

        if ( prefab !=null)
        {
            //ItemManager.GetItemEffect(prefab.transform.position)?.SetActive(true);
            //BaseGameItem baseGameItem = prefab.GetComponent<BaseGameItem>();
            //baseGameItem.networkId = 0;
            //baseGameItem.PlayDestroyEffect();
            //prefab.SetActive(false);
        }
    }

    /// <summary>
    /// 캐릭터 충돌시 아이템 획득
    /// </summary>
    /// <param name="inActor"></param>
    /// <returns></returns>
    public override bool HandleCollisionWithActor(core.Actor inActor)
    {
        return false;
    }
}