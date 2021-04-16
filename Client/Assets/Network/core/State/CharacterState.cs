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
    public class CharacterState
    {
        public Vector3 location;
        public Vector3 velocity;
        public float timeStamp;

        public bool Write(NetOutgoingMessage inOutputStream)
        {
            inOutputStream.Write(ref location);
            inOutputStream.Write(ref velocity);
            inOutputStream.Write(timeStamp);
            return true;
        }

        public bool Read(NetIncomingMessage inInputStream)
        {
            inInputStream.Read(ref location);
            inInputStream.Read(ref velocity);
            timeStamp = inInputStream.ReadFloat();
            return true;
        }

        public bool Write(System.IO.BinaryWriter inOutputStream)
        {
            inOutputStream.Write(location.x);
            inOutputStream.Write(location.y);
            inOutputStream.Write(location.z);

            inOutputStream.Write(velocity.x);
            inOutputStream.Write(velocity.y);
            inOutputStream.Write(velocity.z);

            inOutputStream.Write(timeStamp);

            return true;
        }

        public bool Read(System.IO.BinaryReader inInputStream)
        {
            location.x = inInputStream.ReadSingle();
            location.y = inInputStream.ReadSingle();
            location.z = inInputStream.ReadSingle();

            velocity.x = inInputStream.ReadSingle();
            velocity.y = inInputStream.ReadSingle();
            velocity.z = inInputStream.ReadSingle();

            timeStamp = inInputStream.ReadSingle();

            return true;
        }
    }
}
