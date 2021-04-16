using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CButton : core.Button
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CButton(); }
    GameObject mapProp;
    //MapSpecialObjectProperty mapSpecialObject;

    protected CButton()
    {

    }

    JMapObjectData GetMapData()
    {
        if (mapData == null)
        {
            core.World.mapGameObject.TryGetValue(mapObjectId, out mapData);
        }
        return mapData;
    }

    public override void CompleteCreate()
    {
        Debug.Log($"create CButton {GetNetworkId()}, {mapObjectId}");

        //MapManager.SetActiveMapObj((int)mapObjectId, true);

        //mapProp = MapManager.GetMapObject((int)mapObjectId);
        //if (mapProp == null)
        //{
        //    return;
        //}
        //var behaviour = mapProp.AddComponent<PropBehaviour>();
        //behaviour.NetworkId = GetNetworkId();

        //JMapObjectData mapData = GetMapData();
        //if (mapData == null)
        //{
        //    Debug.Log($"cannot find map object {mapObjectId}");
        //    return;
        //}

        //Set(mapData);
    }
    public override void HandleDying()
    {
        Debug.Log($"remove CButton {GetNetworkId()}, {mapObjectId}");

        base.HandleDying();

        if (mapProp)
        {
			//SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.MapDestroy, 1 ).SoundId1, mapProp.transform.position );
			//GameObject destroyEffect = mapProp.GetComponent<MapDestroyProperty>()?.destroyEffect;
   //         if (destroyEffect)
   //             destroyEffect.SetActive(true);
   //         mapProp.GetComponentInChildren<BoxCollider>().enabled = false;
   //         MapManager.SetActiveMapObj((int)mapObjectId, false);

#if _USE_BEPU_PHYSICS
        if (collision != null)
            core.World.Instance(WorldId).space.Remove(collision);
#endif

            Debug.Log($"remove CButton {GetNetworkId()}, {mapObjectId}");
            return;
        }
    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        if ((readState & (UInt32)ReplicationState.OnOff) != 0)
        {
            // todo : 스위치 상태가 변경 되었을때 처리
            Debug.Log($"CButton NetworkId:{GetNetworkId()}, mapObjectId:{mapObjectId}, mOnOff:{mOnOff}, specialObjectUID:{GetMapData().jMapSwitchData[0].specialObjectUID}");

            int[] arrSpecialObjUID = GetMapData().jMapSwitchData[0].specialObjectUID;
            for (int i = 0; i < arrSpecialObjUID.Length; ++i)
            {
                //GameObject specialObj = MapManager.GetMapObject(arrSpecialObjUID[i]);
                //if (specialObj)
                //    mapSpecialObject = specialObj.GetComponent<MapSpecialObjectProperty>();
                //mapSpecialObject.Play(mOnOff);
            }

            // 스위치가 켜져 있고, hp가 있는 스위치오브젝트라면 파괴 상태로 해놓는다.
            if (mOnOff == true)
            {
                if (mapData != null)
                {
                    if (mapData.objectHP != 0)
                    {
                        HandleDying();
                    }
                }
            }
        }
    }
}
