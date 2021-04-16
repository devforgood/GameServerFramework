using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class CBuff : core.Buff
{
    public static new core.NetGameObject StaticCreate(byte worldId) { return new CBuff(); }

    protected CBuff()
    {

    }

    public override void HandleDying()
    {
        Debug.Log($"remove CBuff {GetNetworkId()}");

        base.HandleDying();
    }

    public override void OnAfterDeserialize(UInt32 readState)
    {
        bool IsCreate = false;
        if ((readState & (UInt32)ReplicationState.Base) != 0)
        {
            //mSpellData = ACDataStorage.SPELL[ mSpellId ];
            IsCreate = true;
            Debug.Log($"create CBuff {GetNetworkId()}, spell:{mSpellData?.Index}, buff:{mSpellData.BuffID}");

        }

        if ((readState & (UInt32)ReplicationState.AddStatus) != 0)
        {

        }

        var parent = (CActor)NetworkManagerClient.sInstance.GetGameObject(mParentNetworkId, core.World.DefaultWorldIndex);
        if (parent == null)
        {
            int networkId = GetNetworkId();
            int parentNetworkId = mParentNetworkId;
            NetworkManagerClient.sInstance.RegisterLinkedObjectEvent(parentNetworkId, () =>
            {
                var buff = (CBuff)NetworkManagerClient.sInstance.GetGameObject(networkId, core.World.DefaultWorldIndex);
                var actor = (CActor)NetworkManagerClient.sInstance.GetGameObject(parentNetworkId, core.World.DefaultWorldIndex);

                if (buff != null && actor != null)
                {
                    // 해당 유저에 버프 등록
                    if(IsCreate)
                        actor.buff.AddBuff(buff);

                    actor.OnChangedBuff(buff.GetBuffType(), buff);
                }
            });
        }
        else
        {
            // 해당 유저에 버프 등록
            if (IsCreate)
                parent.buff.AddBuff(this);

            parent.OnChangedBuff(this.GetBuffType(), this);
        }
    }
}
