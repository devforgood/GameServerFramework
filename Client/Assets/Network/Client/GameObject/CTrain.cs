using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


public class CTrain : core.Train
{
    public GameObject mTarget = null;
    float moveSoundPlayDeltaTime = 0;

    public static new core.NetGameObject StaticCreate(byte worldId) { return new CTrain(); }


    public override void CompleteCreate()
    {
        core.LogHelper.LogInfo($"train start x{GetLocation().x}, y{GetLocation().y}, z{GetLocation().z}, networkID:{NetworkId}");

        if (mapData == null)
        {
            if (core.World.mapGameObject.TryGetValue(mapObjectId, out mapData) == false)
            {
                Debug.Log($"cannot find map object {mapObjectId}");
                return;
            }
            Set(mapData);
        }

        //mTarget = MapManager.GetMapObject(mapData.jMapMovePathData[0].moveObjectUID);
        //if (mTarget != null)
        //{
        //    var script = mTarget.AddComponent<TrainBehaviour>();
        //    script.train = this;

        //    BoxCollider[] arrCol = mTarget.GetComponentsInChildren<BoxCollider>();
        //    for(int i = 0; i< arrCol.Length; ++i)
        //        MapMoveObjectProperty.SetNetID(arrCol[i].GetInstanceID(), NetworkId);
        //}
    }

    public override void HandleDying()
    {
        base.HandleDying();
        if (mTarget != null)
            GameObject.Destroy(mTarget, 0.3f);
    }

    public override void OnAfterDeserialize(uint readState)
    {
        if ((readState & (UInt32)ReplicationState.State) != 0)
        {
            if(mLastState != mState && mState == TrainState.Ready)
            {
                // 기차 출발 2초전
                core.LogHelper.LogInfo($"train ready x{GetLocation().x}, y{GetLocation().y}, z{GetLocation().z}, networkID:{NetworkId}");
                //SoundManager.Instance.Play((int)EAInGameSoundID.TRAIN_APPEAR);
            }
        }
    }
}
