using core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class STrain : Train
    {
        ReplicationPeriod replicationPeriod = new ReplicationPeriod(0.01f);
        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new STrain(), worldId); }

        public override void HandleDying()
        {

            NetworkManagerServer.sInstance.UnregisterGameObject(this);
        }

        public override void Update()
        {
            float deltaTime = Timing.sInstance.GetDeltaTime();



            if (mState == TrainState.Run)
            {
                var last_distance = mapData.jMapMovePathData[0].moveEndPos - GetLocation();
                SetLocation(GetLocation() + mVelocity * deltaTime);
                var distance = mapData.jMapMovePathData[0].moveEndPos - GetLocation();

                //LogHelper.LogInfo($"train distance {distance}");

                if (distance.sqrMagnitude > last_distance.sqrMagnitude)
                {
                    mState = TrainState.Init;
                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.State);
                }

                if (replicationPeriod.UpdateSendingStatePacket())
                {
                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Pose);
                }
            }

            if(mState == TrainState.Init && Timing.sInstance.GetFrameStartTime() > (mCreateTime-2f))
            {

                mState = TrainState.Ready;
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.State);
            }


            if (mState == TrainState.Ready && Timing.sInstance.GetFrameStartTime() > mCreateTime)
            {
                //LogHelper.LogInfo($"train x{GetLocation().x},y{GetLocation().y},z{GetLocation().z}");

                mCreateTime = Timing.sInstance.GetFrameStartTime() + mapData.jMapMovePathData[0].createTime;
                SetLocation(mapData.jMapMovePathData[0].moveStartPos);
                IsResetLocation = IsResetLocation ? false : true;

                mState = TrainState.Run;
                NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.State);

                if (replicationPeriod.UpdateSendingStatePacket())
                {
                    NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, (uint)ReplicationState.Pose);
                }
            }
        }
        protected STrain()
        {

        }

    }
}
