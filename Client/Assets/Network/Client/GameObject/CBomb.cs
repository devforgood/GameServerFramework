using Lidgren.Network;
using UnityEngine;
using System.Collections.Generic;

public partial class CBomb : core.Bomb
{
    public GameObject mTarget = null;
    //public BombBehaviour mBombBehaviour = null;

    public static new core.NetGameObject StaticCreate(byte worldId) { return new CBomb(); }


    public override void CompleteCreate()
    {
        /*
        GameObject go = GameObject.Find("NewBomb");
        if (go == null)
            return;

        GameObject bomb = GameObject.Instantiate(go, GetLocation(), go.transform.rotation);
        mTarget = bomb;
        mBombBehaviour = bomb.GetComponent<BombBehaviour>();
        mBombBehaviour.bomb = this;

        var parent = NetworkManagerClient.sInstance.GetGameObject(mParentNetworkId);
        if (parent != null)
        {
            ((CActor)parent).LastBombNetworkId = GetNetworkId();
            Debug.Log($"bomb create {GetNetworkId()}, {((CActor)parent).LastBombNetworkId}");


            //((CActor)parent)?.mActorBehaviour?.PlayAnimation("Bomb");
        }
        */


   //     if (mapObjectId != 0)
   //     {
   //         // 맵에서 생성된 폭탄
   //         GameObject go = MapManager.GetMapObject(mapObjectId);
   //         if (go != null)
   //         {
   //             MapAutoBombProperty mapAutoBomb = go.GetComponent<MapAutoBombProperty>();
   //             SetLocation(mapAutoBomb.transform.position);
   //             mapAutoBomb.CreateSkillBomb(this, (int)core.ReservedPlayerId.Trap);
			//	SoundManager.Instance.Play( ACDataStorage.MAP_OBJECT_PROPERTY.GetData( (int)MapPropertyType.AutoCreateBomb, 1 ).SoundId1, mapAutoBomb.transform.position );
			//}
   //         else
   //         {
   //             Debug.LogError($"create bomb error {GetNetworkId()}, {mapObjectId}");
   //         }
   //         return;
   //     }
   //     else
   //     {
   //         SkillManager.GetInstance().InstallSkillBomb(this, mPlayerId, mSkillId, mExplodeTime, mIsHidden, mTeam);
   //     }
/*
        bool LateCreate = false;
        var parent = NetworkManagerClient.sInstance.GetGameObject(mParentNetworkId, core.World.DefaultWorldIndex);
        if (parent != null)
        {
            if (parent.GetClassId() == (byte)core.GameObjectClassId.Actor)
            {
                if (((CActor)parent).ActorSkillMgr != null)
                {
                    ((CActor)parent).LastBombNetworkId = GetNetworkId();
                    ((CActor)parent).ActorSkillMgr.InstallBomb(this);
                }
                else
                {
                    Debug.Log($"not yet skill mgr {GetNetworkId()}, {mParentNetworkId}");
                    LateCreate = true;

                }
            }
        }
        else
        {
            Debug.Log($"not yet actor {GetNetworkId()}, {mParentNetworkId}");
            LateCreate = true;
        }

        if(LateCreate)
        {
            int networkId = GetNetworkId();
            int parentNetworkId = mParentNetworkId;
            NetworkManagerClient.sInstance.RegisterLinkedObjectEvent(parentNetworkId, () =>
            {
                var bombObject = NetworkManagerClient.sInstance.GetGameObject(networkId, core.World.DefaultWorldIndex);
                var gameObject = NetworkManagerClient.sInstance.GetGameObject(parentNetworkId, core.World.DefaultWorldIndex);
                if (gameObject != null && bombObject != null)
                {
                    if (gameObject.GetClassId() == (byte)core.GameObjectClassId.Actor)
                    {
                        if (((CActor)gameObject).ActorSkillMgr != null)
                        {
                            ((CActor)gameObject).LastBombNetworkId = networkId;
                            ((CActor)gameObject).ActorSkillMgr.InstallBomb(this);
                            Debug.Log($"complete bomb {networkId}, {parentNetworkId}");
                        }
                    }
                }
            });
        }
*/

    }

    public void OnExplode(List<int> objectList)
    {
        // 폭발 데미지 오브젝트 서버 전달 
        InvokeServerRpc(BombResult, objectList);
    }

    public override void HandleDying()
    {
        base.HandleDying();
        if (mTarget != null)
            GameObject.Destroy(mTarget, 0.3f);
    }

    public override void Update()
    {
        if(mIsExplode)
        {
            //if (mBombBehaviour != null)
            //    mBombBehaviour.Explode();

            mIsExplode = false;
        }
    }
}
