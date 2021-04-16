using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace core
{
    public class Move
    {
        public InputState mInputState = new InputState();
        public float mTimestamp;
        public float mDeltaTime;

        public Move()
        {

        }

        public Move(InputState inInputState, float inTimestamp, float inDeltaTime)
        {
            mInputState = inInputState;
            mTimestamp = inTimestamp;
            mDeltaTime = inDeltaTime;
        }

        public InputState GetInputState() { return mInputState; }
        public float GetTimestamp() { return mTimestamp; }
        public float GetDeltaTime() { return mDeltaTime; }

        public bool Write(NetOutgoingMessage inOutputStream)
        {
            mInputState.Write(inOutputStream);
            inOutputStream.Write(mTimestamp);

            return true;
        }
        public bool Read(NetIncomingMessage inInputStream)
        {
            mInputState.Read(inInputStream);
            mTimestamp = inInputStream.ReadFloat();

            return true;
        }

    }
}