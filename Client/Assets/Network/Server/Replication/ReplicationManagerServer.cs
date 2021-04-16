using core;
using Lidgren.Network;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using uint32_t = System.UInt32;

namespace Server
{
    public class ReplicationManagerServer
    {
        static readonly int max_replication_count = 50;
        public static float DefaultFirstReplicationTimeout = 30f;

        Dictionary<int, ReplicationCommand> mNetworkIdToReplicationCommand = new Dictionary<int, ReplicationCommand>();
        public bool isFirst = true;
        public float firstReplicationTimeout;

        public void ReplicateCreate(int inNetworkId, uint32_t inInitialDirtyState)
        {
            mNetworkIdToReplicationCommand[inNetworkId] = new ReplicationCommand(inInitialDirtyState);
        }

        public void ReplicateDestroy(int inNetworkId)
        {
            //it's broken if we don't find it...
            if (mNetworkIdToReplicationCommand.ContainsKey(inNetworkId) == false)
            {
                return;
            }

            mNetworkIdToReplicationCommand[inNetworkId].SetDestroy();
        }

        public void RemoveFromReplication(int inNetworkId)
        {
            mNetworkIdToReplicationCommand.Remove(inNetworkId);
        }

        public void SetStateDirty(int inNetworkId, uint32_t inDirtyState)
        {
            if(mNetworkIdToReplicationCommand.ContainsKey(inNetworkId)==false)
            {
                return;
            }
            mNetworkIdToReplicationCommand[inNetworkId].AddDirtyState(inDirtyState);
        }

        public void HandleCreateAckd(int inNetworkId)
        {
            if (mNetworkIdToReplicationCommand.ContainsKey(inNetworkId) == false)
            {
                return;
            }

            mNetworkIdToReplicationCommand[inNetworkId].HandleCreateAckd();
        }

        /// <summary>
        ///  모든 오브젝트가 한번 동기화 되었음
        /// </summary>
        /// <returns></returns>
        public bool IsCompleteCreateDelivery(PlayerController playerController)
        {
            if (isFirst)
            {
                return false;
            }

            var cnt = mNetworkIdToReplicationCommand.Count(x => x.Value.GetAction() == ReplicationAction.RA_Create);
            if (cnt > 0)
            {
                LogHelper.LogInfo($"not yet sync {cnt}/{mNetworkIdToReplicationCommand.Count}");
                //foreach(var cmd in mNetworkIdToReplicationCommand)
                //{
                //    if(cmd.Value.GetAction() == ReplicationAction.RA_Create)
                //    {
                //        Log.Information($"networId : {cmd.Key}, state:{cmd.Value.GetDirtyState()}");
                //    }
                //}

                if(firstReplicationTimeout < Timing.sInstance.GetFrameStartTime())
                {
                    if(playerController.IsTcp==false)
                    {
                        Log.Error($"timeout not yet sync {cnt}/{mNetworkIdToReplicationCommand.Count}");
                        playerController.mConnection.Disconnect("");
                    }
                }

                return false;
            }

            isFirst = true;
            return true;
        }


        public int Write(NetOutgoingMessage inOutputStream, ReplicationManagerTransmissionData ioTransmissinData, int inRestartedNetworkId, byte inWorldId)
        {
            int count = 0;
            bool skip = (inRestartedNetworkId != 0);
            //run through each replication command and do something...
            List<int> removeObject = null;
            foreach (var pair in mNetworkIdToReplicationCommand )
            {
                ReplicationCommand replicationCommand = pair.Value;
                if (replicationCommand.HasDirtyState())
                {
                    // 이전에 중단 되었던 network ID 부터 재개
                    if (skip)
                    {
                        if (inRestartedNetworkId == pair.Key)
                        {
                            skip = false;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    int networkId = pair.Key;
                    ReplicationAction action = replicationCommand.GetAction();
                    NetGameObject gameObject = NetworkManagerServer.sInstance.GetGameObject(networkId, inWorldId);
                    if (gameObject == null && action != ReplicationAction.RA_Destroy)
                    {
                        if (removeObject == null)
                            removeObject = new List<int>();

                        removeObject.Add(networkId);
                        continue;
                    }

                    // 최대로 동기화 할수 있는 개수 초과시 중단
                    ++count;
                    if (count > max_replication_count)
                    {
                        return pair.Key;
                    }



                    //well, first write the network id...
                    inOutputStream.Write((uint)networkId, NetGameObject.NetworkIdSize);
                    //Log.Information($"write networkId:{networkId}, classId:{gameObject?.GetClassId()}, action:{action}");

                    //only need 2 bits for action...
                    inOutputStream.Write(action);

                    uint32_t writtenState = 0;
                    uint32_t dirtyState = replicationCommand.GetDirtyState();

                    //now do what?
                    switch (action)
                    {
                        case ReplicationAction.RA_Create:
                            {
                                inOutputStream.Write(gameObject.GetClassId());
                                writtenState = gameObject.Write(inOutputStream, dirtyState);
                            }
                            break;
                        case ReplicationAction.RA_Update:
                            {
                                writtenState = gameObject.Write(inOutputStream, dirtyState);
                            }
                            break;
                        case ReplicationAction.RA_Destroy:
                            {
                                //don't need anything other than state!
                                writtenState = dirtyState;
                            }
                            break;
                    }

                    ioTransmissinData.AddTransmission(networkId, action, writtenState, inWorldId);

                    //let's pretend everything was written- don't make this too hard
                    replicationCommand.ClearDirtyState(writtenState);
                }
            }

            if(removeObject!=null)
            {
                foreach (var key in removeObject)
                {
                    mNetworkIdToReplicationCommand.Remove(key);
                }
            }
            return 0;
        }
    }
}
