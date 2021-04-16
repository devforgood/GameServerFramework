using core;
using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace Server
{
    public class AIStateManager
    {
        public AIState[] mAIState = null;
        public uint mAIStateCount = 0;

        public AIState[] mAIStateBuffer1 = new AIState[(int)AIPlayer.MaxAIPlayer];
        public AIState[] mAIStateBuffer2 = new AIState[(int)AIPlayer.MaxAIPlayer];
        public bool mCurrentBufferIndex;
        public uint AIPlayerCount;


        public AIStateManager()
        {
            mCurrentBufferIndex = false;
            for (int i = 0; i < (int)AIPlayer.MaxAIPlayer; ++i)
            {
                mAIStateBuffer1[i] = new AIState();
                mAIStateBuffer2[i] = new AIState();
            }
        }

        public void Read(NetIncomingMessage inInputStream)
        {
            AIPlayerCount = inInputStream.ReadUInt32((int)AIPlayer.MaxAIPlayerBit);
            if (AIPlayerCount > 0)
            {
                for (int i = 0; i < AIPlayerCount; ++i)
                {
                    if (mCurrentBufferIndex == false)
                        mAIStateBuffer1[i].Read(inInputStream);
                    else
                        mAIStateBuffer2[i].Read(inInputStream);
                }
            }
        }

        public bool Set()
        {
            if (AIPlayerCount > 0)
            {
                if (mCurrentBufferIndex == false)
                    mAIState = mAIStateBuffer1;
                else
                    mAIState = mAIStateBuffer2;

                mAIStateCount = AIPlayerCount;

                mCurrentBufferIndex = !mCurrentBufferIndex;
                return true;
            }
            return false;
        }

        public AIState GetState(int player_id)
        {
            for (int i = 0; i < (int)mAIStateCount; ++i)
            {
                if (mAIState[i].playerId == player_id)
                    return mAIState[i];
            }
            return null;
        }
    }
}
