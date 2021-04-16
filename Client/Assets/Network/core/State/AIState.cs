using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace core
{
    public class AIState : CharacterState
    {
        public int playerId;

        public new bool Write(NetOutgoingMessage inOutputStream)
        {
            inOutputStream.Write(ref location);
            inOutputStream.Write(ref velocity);
            inOutputStream.Write(playerId);
            return true;
        }

        public new bool Read(NetIncomingMessage inInputStream)
        {
            inInputStream.Read(ref location);
            inInputStream.Read(ref velocity);
            playerId = inInputStream.ReadInt32();
            return true;
        }
    }
}
