using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace core
{
    public enum ReplicationAction
    {
        RA_Create,
        RA_Update,
        RA_Destroy,
        RA_RPC,
        RA_MAX
    };

    public static partial class NetBufferExtensions
    {
        public static void Write(this NetBuffer buff, ReplicationAction r)
        {
            buff.Write((byte)r, ReplicationCommand.ReplicationActionTypeSize);
        }

        public static void Read(this NetBuffer buff, out ReplicationAction r)
        {
            r = (ReplicationAction)buff.ReadByte(ReplicationCommand.ReplicationActionTypeSize);
        }
    }

    public class ReplicationCommand
    {
        public const int ReplicationActionTypeSize = 2; // bit
        UInt32 mDirtyState;
        ReplicationAction mAction;

        public ReplicationCommand() { }
        public ReplicationCommand(UInt32 inInitialDirtyState)
        {
            mAction = ReplicationAction.RA_Create;
            mDirtyState = inInitialDirtyState;
        }
        public void HandleCreateAckd() { if (mAction == ReplicationAction.RA_Create) { mAction = ReplicationAction.RA_Update; } }
        public void AddDirtyState(UInt32 inState) { mDirtyState |= inState; }
        public void SetDestroy() { mAction = ReplicationAction.RA_Destroy; }

        public bool HasDirtyState() { return (mAction == ReplicationAction.RA_Destroy) || (mDirtyState != 0); }

        public ReplicationAction GetAction() { return mAction; }
        public UInt32 GetDirtyState() { return mDirtyState; }
        public void ClearDirtyState(UInt32 inStateToClear)
        {
            mDirtyState &= ~inStateToClear;

            if (mAction == ReplicationAction.RA_Destroy)
            {
                mAction = ReplicationAction.RA_Update;
            }
        }


    }
}
