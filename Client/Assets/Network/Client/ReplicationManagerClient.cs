using core;
using Lidgren.Network;
using System;
using UnityEngine;

public class ReplicationManagerClient
{

    public void Read(NetIncomingMessage inInputStream )
    {
        while (inInputStream.LengthBits - inInputStream.Position >= NetGameObject.NetworkIdSize)
        {
            //read the network id...
            int networkId = (int)inInputStream.ReadUInt32(NetGameObject.NetworkIdSize);
            //Debug.Log($"Read {networkId}");
            
            //only need 2 bits for action...
            core.ReplicationAction action;
            inInputStream.Read(out action);

            switch (action)
            {
                case core.ReplicationAction.RA_Create:
                    ReadAndDoCreateAction(inInputStream, networkId);
                    break;
                case core.ReplicationAction.RA_Update:
                    ReadAndDoUpdateAction(inInputStream, networkId);
                    break;
                case core.ReplicationAction.RA_Destroy:
                    ReadAndDoDestroyAction(inInputStream, networkId);
                    break;
            }

        }
        //Debug.Log($"Read end {inInputStream.LengthBits}, {inInputStream.Position}");

    }

    void ReadAndDoCreateAction(NetIncomingMessage inInputStream, int inNetworkId)
    {
        UInt32 classID = inInputStream.ReadByte();

        //LogHelper.LogInfo($"replication {(core.GameObjectClassId)classID}");

        //we might already have this object- could happen if our ack of the create got dropped so server resends create request 
        //( even though we might have created )

        NetGameObject gameObject = NetworkManagerClient.sInstance.GetGameObject(inNetworkId, core.World.DefaultWorldIndex);
        if (gameObject == null)
        {
            //create the object and map it...
            gameObject = GameObjectRegistry.sInstance.CreateGameObject(classID, false);
            gameObject.SetNetworkId(inNetworkId);
            NetworkManagerClient.sInstance.AddToNetworkIdToGameObjectMap(gameObject, core.World.DefaultWorldIndex);
            gameObject.IsCreate = true;

            //it had really be the rigth type...
            //Assert(gameObject.GetClassId() == fourCCName);
            //Debug.Log($"ReadAndDoCreateAction networkID {inNetworkId}");

        }
        else
        {
            gameObject.IsCreate = false;
        }

        //and read state
        gameObject.Read(inInputStream);
        if(gameObject.IsCreate)
            gameObject.CompleteCreate();
    }

    void ReadAndDoUpdateAction(NetIncomingMessage inInputStream, int inNetworkId)
    {
        //need object
        NetGameObject gameObject = NetworkManagerClient.sInstance.GetGameObject(inNetworkId, core.World.DefaultWorldIndex);
        if (gameObject != null)
        {
            gameObject.IsCreate = false;

            //gameObject MUST be found, because create was ack'd if we're getting an update...
            //and read state
            gameObject.Read(inInputStream);
        }
        else
        {
            Debug.LogError($"ReadAndDoUpdateAction cannot find network object {inNetworkId}");
        }
    }

    void ReadAndDoDestroyAction(NetIncomingMessage inInputStream, int inNetworkId)
    {
        //Debug.Log($"ReadAndDoDestroyAction {inNetworkId}");

        //if something was destroyed before the create went through, we'll never get it
        //but we might get the destroy request, so be tolerant of being asked to destroy something that wasn't created
        NetGameObject gameObject = NetworkManagerClient.sInstance.GetGameObject(inNetworkId, core.World.DefaultWorldIndex);
        if (gameObject != null)
        {
            gameObject.SetDoesWantToDie(true);
            NetworkManagerClient.sInstance.RemoveFromNetworkIdToGameObjectMap(gameObject, core.World.DefaultWorldIndex);

            //Debug.Log($"ReadAndDoDestroyAction networkID {inNetworkId}");

        }
    }
}

