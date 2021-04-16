using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace core
{
    public class InputState
    {
        public UInt32 mDirection; // use 9bit
        public bool mIsMove;
        public bool mIsShooting;
        public bool mIsBomb;
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
        public bool mIsChangeY;
        public float mYaxis;
#endif
#if MOVEMENT_DEBUG
        public int mDebugSeq;
#endif 

        public override string ToString()
        {
            return "mDirection:" + mDirection
                + ",mIsMove:" + mIsMove
                + ",mIsShooting:" + mIsShooting
                + ",mIsBomb:" + mIsBomb
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
                + ",mIsChangeY:" + mIsChangeY
                + ",mYaxis:" + mYaxis
#endif
#if  MOVEMENT_DEBUG
                + ",mDebugSeq:" + mDebugSeq;
#endif
            ;
        }

        public InputState()
        {
            Clear();
        }

        public void Clear()
        {
            mDirection = 0;
            mIsMove = false;
            mIsShooting = false;
            mIsBomb = false;
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
            mIsChangeY = false;
            mYaxis = 0.0f;
#endif
#if  MOVEMENT_DEBUG
            mDebugSeq = 0;
#endif
        }

        public InputState Clone()
        {
            return new InputState()
            {
                mDirection = this.mDirection,
                mIsMove = this.mIsMove,
                mIsShooting = this.mIsShooting,
                mIsBomb = this.mIsBomb,
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
                mIsChangeY = this.mIsChangeY,
                mYaxis = this.mYaxis,
#endif
#if MOVEMENT_DEBUG
                mDebugSeq = this.mDebugSeq,
#endif
            };
        }


        public bool IsShooting() { return mIsShooting; }


        public bool Write(NetOutgoingMessage inOutputStream)
        {
            inOutputStream.Write(mIsMove);
            if (mIsMove)
                inOutputStream.Write(mDirection, 9);
            inOutputStream.Write(mIsShooting);
            inOutputStream.Write(mIsBomb);
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
            inOutputStream.Write(mIsChangeY);
            if (mIsChangeY)
                inOutputStream.Write(mYaxis);
#endif
#if MOVEMENT_DEBUG
            inOutputStream.Write(mDebugSeq);
#endif 
            return false;
        }
        public bool Read(NetIncomingMessage inInputStream)
        {
            mIsMove = inInputStream.ReadBoolean();
            if (mIsMove)
                mDirection = inInputStream.ReadUInt32(9);
            mIsShooting = inInputStream.ReadBoolean();
            mIsBomb = inInputStream.ReadBoolean();
#if !_USE_BEPU_PHYSICS && !_USE_BULLET_SHARP // CHANGE_Y_AXIS_CLIENT
            mIsChangeY = inInputStream.ReadBoolean();
            if (mIsChangeY)
                mYaxis = inInputStream.ReadFloat();
#endif
#if MOVEMENT_DEBUG
            mDebugSeq = inInputStream.ReadInt32();
#endif 
            return true;
        }
    }
}
