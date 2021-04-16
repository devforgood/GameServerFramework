using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CProp : core.Prop
{ 
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CProp(); }
    GameObject mapProp;
    //MapDestroyProperty mapDestroyProperty;
    //public GameObject mTarget = null;

    protected CProp()
    {

    }

    public override void CompleteCreate()
    {
        //Debug.Log($"create prop {GetNetworkId()}, {mapObjectId}");
        //MapManager.SetActiveMapObj((int)mapObjectId, true);

        //mapProp = MapManager.GetMapObject((int)mapObjectId);
        //if(mapProp == null)
        //{
        //    return;
        //}
        //mapDestroyProperty = mapProp.GetComponent<MapDestroyProperty>();
        //var behaviour = mapProp.AddComponent<PropBehaviour>();
        //behaviour.NetworkId = GetNetworkId();


        JMapObjectData mapData;
        if(core.World.mapGameObject.TryGetValue(mapObjectId, out mapData)==false)
        {
            Debug.Log($"cannot find map object {mapObjectId}");
            return;
        }

        Set(mapData);

#if _USE_BEPU_PHYSICS
        collision = core.World.Instance(WorldId).CreateCollision(mapData);
#endif

    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        if (IsDie)
        {
            // todo : 폭발 스킬 시전
            if (mapProp)
            {
                //폭발 스킬 시전시 Prop의 BoxCollider 비활성화.
                BoxCollider[] arrBoxCol = mapProp.GetComponentsInChildren<BoxCollider>();
                for (int i = 0; i < arrBoxCol.Length; ++i)
                    arrBoxCol[i].enabled = false;
                mapProp.GetComponentInChildren<MeshRenderer>().enabled = false;
                //if (mapDestroyProperty)
                //{
                //    SetLocation(mapDestroyProperty.transform.position);
                //    mapDestroyProperty.CreateSkillBomb(this, (int)core.ReservedPlayerId.Trap);
                //}
            }
        }
    }

    public void OnExplode(List<int> objectList)
    {
        // 폭발 데미지 오브젝트 서버 전달. 
        InvokeServerRpc(PropExplodeResult, objectList);
    }


    public override void HandleDying()
    {
        base.HandleDying();
        //if (mTarget != null)
        //    GameObject.Destroy(mTarget, 0.3f);
        if (mapProp)
        {
			//SoundManager.Instance.PlaySoundRange( ACDC.MapObjectPropertyData.GetData( (int)MapPropertyType.MapDestroy, 1 ).SoundId1, mapProp.transform.position );
			//SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.MapDestroy, 1 ).SoundId1, mapProp.transform.position );
			//GameObject destroyEffect = mapDestroyProperty.destroyEffect;
   //         if(destroyEffect)
   //             destroyEffect.SetActive(true);
   //         mapDestroyProperty.tweenTransform.DOShakePosition(0.3f, new Vector3(0.125f, 0, 0.125f)).OnComplete(() =>
   //         {
   //             mapDestroyProperty.PlayDestroyEffect();
   //             BoxCollider[] arrBoxCol = mapProp.GetComponentsInChildren<BoxCollider>();
   //             for (int i = 0; i < arrBoxCol.Length; ++i)
   //                 arrBoxCol[i].enabled = false;
   //             MapManager.SetActiveMapObj((int)mapObjectId, false);
   //             //CharacterController.isGrounded 체크 강제로 호출.
   //             ((ACLocalCharacter)ACScrambleBattle.ENTRY.LocalCharacter)?.CharacterController.SimpleMove(Vector3.zero);
   //         });
   //         MapIconProperty mapIcon = mapProp.GetComponent<MapIconProperty>();
   //         if (mapIcon)
   //             mapIcon.SetIcon(false);
#if _USE_BEPU_PHYSICS
        if (collision != null)
            core.World.Instance(WorldId).space.Remove(collision);
#endif

            Debug.Log($"remove prop {GetNetworkId()}, {mapObjectId}");
            return;
        }
    }
}
