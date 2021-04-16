using core;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uint32_t = System.UInt32;


namespace Server
{
    public class ReplicationManagerTransmissionData : TransmissionData
    {
        class ReplicationTransmission
        {
            public ReplicationTransmission(int inNetworkId, ReplicationAction inAction, uint32_t inState, byte inWorldId)
            {
                mNetworkId = inNetworkId;
                mAction = inAction;
                mState = inState;
                mWorldId = inWorldId;
            }
            public int GetNetworkId() { return mNetworkId; }
            public ReplicationAction GetAction() { return mAction; }
            public uint32_t GetState() { return mState; }

            public byte GetWorldId() { return mWorldId; }


            int mNetworkId;
            ReplicationAction mAction;
            uint32_t mState;
            byte mWorldId;
        };

        ReplicationManagerServer mReplicationManagerServer;

        List<ReplicationTransmission> mTransmissions = new List<ReplicationTransmission>();
        public ReplicationManagerTransmissionData(ReplicationManagerServer inReplicationManagerServer)
        {
            mReplicationManagerServer = inReplicationManagerServer;
        }

        public void AddTransmission(int inNetworkId, ReplicationAction inAction, uint32_t inState, byte inWorldId)
        {
            //it would be silly if we already had a transmission for this network id in here...
            foreach ( var transmission in mTransmissions )
            {
                //assert(inNetworkId != transmission.GetNetworkId());
                if (inNetworkId == transmission.GetNetworkId())
                {
                    Log.Warning($"AddTransmission inNetworkID:{inNetworkId}, transmission.NetworkID:{transmission.GetNetworkId()}");
                }
            }

            mTransmissions.Add(new ReplicationTransmission(inNetworkId, inAction, inState, inWorldId));

        }

        public override void HandleDeliveryFailure(DeliveryNotificationManager inDeliveryNotificationManager)
        {
            if(mTransmissions.Count > 10)
            {
                Log.Warning($"HandleDeliveryFailure FailCount:{mTransmissions.Count}");
            }
            //Log.Warning($"HandleDeliveryFailure NetworkIDs:{string.Join(",", mTransmissions.Select(x => x.GetNetworkId()))}");
            //Log.Warning($"HandleDeliveryFailure Actions:{string.Join(",", mTransmissions.Select(x => x.GetAction()))}");

            //run through the transmissions
            foreach (var rt in mTransmissions)
            {
                switch (rt.GetAction())
                {
                    case ReplicationAction.RA_Create:
                        HandleCreateDeliveryFailure(rt.GetNetworkId(), rt.GetWorldId());
                        break;
                    case ReplicationAction.RA_Update:
                        HandleUpdateStateDeliveryFailure(rt.GetNetworkId(), rt.GetState(), inDeliveryNotificationManager, rt.GetWorldId());
                        break;
                    case ReplicationAction.RA_Destroy:
                        HandleDestroyDeliveryFailure(rt.GetNetworkId());
                        break;
                }

            }
        }
        public override void HandleDeliverySuccess(DeliveryNotificationManager inDeliveryNotificationManager)
        {
            //run through the transmissions, if any are Destroyed then we can remove this network id from the map
            foreach (var rt in mTransmissions)
            {
                switch (rt.GetAction())
                {
                    case ReplicationAction.RA_Create:
                        HandleCreateDeliverySuccess(rt.GetNetworkId());
                        break;
                    case ReplicationAction.RA_Destroy:
                        HandleDestroyDeliverySuccess(rt.GetNetworkId());
                        break;
                }
            }
        }

        void HandleCreateDeliveryFailure(int inNetworkId, byte inWorldId)
        {
            NetGameObject gameObject = NetworkManagerServer.sInstance.GetGameObject(inNetworkId, inWorldId);
            if (gameObject != null)
            {
                mReplicationManagerServer.ReplicateCreate(inNetworkId, gameObject.GetAllStateMask());
            }
        }
        void HandleUpdateStateDeliveryFailure(int inNetworkId, uint32_t inState, DeliveryNotificationManager inDeliveryNotificationManager, byte inWorldId)
        {
            //does the object still exist? it might be dead, in which case we don't resend an update
            if (NetworkManagerServer.sInstance.GetGameObject(inNetworkId, inWorldId) != null)
            {
                //look in all future in flight packets, in all transmissions
                //remove written state from dirty state
                foreach (var inFlightPacket in inDeliveryNotificationManager.GetInFlightPackets())
                {
                    ReplicationManagerTransmissionData rmtdp = (ReplicationManagerTransmissionData)(inFlightPacket.GetTransmissionData((int)TransmissionDataType.kReplicationManager));

                    foreach (var otherRT in rmtdp.mTransmissions)
                    {
                        inState &= ~otherRT.GetState();
                    }
                }

                //if there's still any dirty state, mark it
                if (inState != 0)
                {
                    mReplicationManagerServer.SetStateDirty(inNetworkId, inState);
                }
            }
        }
        void HandleDestroyDeliveryFailure(int inNetworkId)
        {
            //Log.Information($"HandleDestroyDeliveryFailure {inNetworkId}");

            mReplicationManagerServer.ReplicateDestroy(inNetworkId);
        }
        void HandleCreateDeliverySuccess(int inNetworkId)
        {
            //Log.Information($"HandleCreateDeliverySuccess {inNetworkId}");

            //we've received an ack for the create, so we can start sending as only an update
            mReplicationManagerServer.HandleCreateAckd(inNetworkId);

        }
        void HandleDestroyDeliverySuccess(int inNetworkId)
        {
            //Log.Information($"HandleDestroyDeliverySuccess {inNetworkId}");

            mReplicationManagerServer.RemoveFromReplication(inNetworkId);
        }
    }

}
