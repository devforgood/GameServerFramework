using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class TickCounter
    {
        float mStartTimestamp;
        uint mTickNumber;
        readonly float DefaultDeltaTime = 0.033f;

        public void Reset()
        {
            mTickNumber = 0;
            mStartTimestamp = 0f;
        }

        public void Correct(uint tickNumber, float rtt)
        {
            mTickNumber = tickNumber;
            mTickNumber += (uint)(rtt / DefaultDeltaTime);
            LogHelper.LogInfo($"Correct tick:{mTickNumber}, server tick:{tickNumber}, rtt{rtt}");
        }

        public uint TickNumber 
        { 
            get 
            { 
                return mTickNumber; 
            }
        }

        [System.Diagnostics.Conditional("USE_TICK_COUNTER")]
        public void Update()
        {
            mStartTimestamp += Timing.sInstance.GetDeltaTime();

            while(mStartTimestamp >= DefaultDeltaTime)
            {
                mStartTimestamp -= DefaultDeltaTime;
                ++mTickNumber;

                //LogHelper.LogInfo($"TickCounter tick:{TickNumber}, stamp:{Timing.sInstance.GetFrameStartTime()}");
            }
        }
    }
}
