using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace core
{
    public class MoveList
    {
        float mLastMoveTimestamp;
        public List<Move> mMoves = new List<Move>();

#if MOVEMENT_DEBUG
        public int mCurrentDebugSeq = 0;
#endif 

        public MoveList()
        {

            mLastMoveTimestamp = -1.0f;
        }

        public Move AddMove(InputState inInputState, float inTimestamp)
        {
            //first move has 0 time. it's okay, it only happens once
            float deltaTime = mLastMoveTimestamp >= 0.0f ? inTimestamp - mLastMoveTimestamp : 0.0f;

#if MOVEMENT_DEBUG
            inInputState.mDebugSeq = ++mCurrentDebugSeq;
#endif

            mMoves.Add(new Move(inInputState, inTimestamp, deltaTime));

            mLastMoveTimestamp = inTimestamp;

            return mMoves.LastOrDefault();
        }
        public bool AddMoveIfNew(Move inMove)
        {
            //we might have already received this move in another packet ( since we're sending the same move in multiple packets )
            //so make sure it's new...

            //adjust the deltatime and then place!
            float timeStamp = inMove.GetTimestamp();

            // todo : 패킷 유효성 판단

            // 최근 받은 이동 패킷보다 과거이면 버려진다.
            if (timeStamp > mLastMoveTimestamp)
            {
                // 이전 패킷과의 간격
                float deltaTime = mLastMoveTimestamp >= 0.0f ? timeStamp - mLastMoveTimestamp : 0.0f;

                mLastMoveTimestamp = timeStamp;

                mMoves.Add(new Move(inMove.GetInputState(), timeStamp, deltaTime));
                return true;
            }

            return false;

        }

		public	void RemovedProcessedMoves(float inLastMoveProcessedOnServerTimestamp)
        {
            while (mMoves.Count != 0 && mMoves[0].GetTimestamp() <= inLastMoveProcessedOnServerTimestamp)
            {
                mMoves.RemoveAt(0);
            }
        }

        public float GetLastMoveTimestamp() { return mLastMoveTimestamp; }

        public Move GetLatestMove() { return mMoves.LastOrDefault(); }

        public void Flush()
        {
            // 마지만 인풋값이 이동중이라면 계속 이동을 하도록 수정
            if (mMoves.Count != 0)
            {
                if (mMoves[mMoves.Count - 1].mInputState.mIsMove == true)
                {
                    if (mMoves.Count != 1)
                    {
                        mMoves.RemoveRange(0, mMoves.Count - 1);
                        //LogHelper.LogInfo($"MoveList Flush delta{mMoves[0].GetDeltaTime()}");
                    }
                }
                else
                {
                    mMoves.Clear();
                }
            }
        }

        public void Clear()
        {
            mMoves.Clear();
        }
        public bool HasMoves() { return mMoves.Count != 0; }
        public int GetMoveCount() { return mMoves.Count; }

    }
}
