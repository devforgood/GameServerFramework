using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Lidgren.Network;

public class CTreasureBox : core.TreasureBox
{
    GameObject prefab = null;

    public static new core.NetGameObject StaticCreate(byte worldId) { return new CTreasureBox(); }


    protected CTreasureBox()
    {

    }

    public override void CompleteCreate()
    {
        Debug.Log($"create TreasureBox {GetNetworkId()}, {mapObjectId}");
    }

    public override void HandleDying()
    {
        base.HandleDying();
    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        if ((readState & (UInt32)ReplicationState.MapObjectID) != 0)
        {
            if (mapData == null)
            {
                if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
                {
                    Debug.Log($"cannot find map object {mapObjectId}");
                    return;
                }
                Set(mapData);
            }
        }

        if ((readState & (UInt32)ReplicationState.SpawnItem) != 0)
        {

            if (ItemId != 0)
            {
                // 아이템 생성
                if (prefab == null)
                {
                    prefab = ItemManager.GetItemObject(ItemId, false);
                    if (prefab != null)
                    {
                        Vector3 pos = GetLocation();
                        pos.y += 1;
                        prefab.transform.position = pos;
                        prefab.SetActive(true);
                        Debug.Log($"create item {ItemId}");
						//SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.AutoCreateItem, 1 ).SoundId1, prefab.transform.position );
						//GameObject treasureGo = MapManager.GetMapObject(mapObjectId);
      //                  if(treasureGo)
      //                      treasureGo.GetComponent<MapAutoItemProperty>().SetAniParameter("Open");
                    }
                    else
                    {
                        Debug.LogError($"cannot find item {ItemId}");
                    }
                }

            }
            else
            {
                // 아이템 파괴
                if (prefab != null)
                {
     //               BaseGameItem gameItem = prefab.GetComponent<BaseGameItem>();
     //               if (gameItem)
     //               {
					//	SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.AutoCreateItem, 1 ).SoundId2, prefab.transform.position );
					//	GameObject treasureGo = MapManager.GetMapObject(mapObjectId);
     //                   if (treasureGo)
     //                       treasureGo.GetComponent<MapAutoItemProperty>().SetAniParameter("Close");

     //                   ACItemComponent		a_kItemComponent	= ACComponentProvider.GetEntity<ACItemEntity>( gameItem.itemID );

     //                   gameItem.PlayDestroyEffect();
     //                   SoundManager.Instance.Play( a_kItemComponent.SoundID, prefab.transform.position );
					//}
                    prefab.SetActive(false);
                    prefab = null;

                    Debug.Log($"remove item {ItemId}");
                }

            }
        }
    }
}

