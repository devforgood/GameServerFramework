using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using uint64_t = System.UInt64;


namespace core
{
    public class Timing
    {
        /// <summary>
        /// Global instance of Timing
        /// </summary>
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        public static Timing sInstance = new Timing();

        DateTime sStartTime;

        float mDeltaTime;
        //uint64_t mDeltaTick;

        double mLastFrameStartTime;
        float mFrameStartTimef;
        //double mPerfCountDuration;

        public Timing()
        {
            sStartTime = DateTime.UtcNow;
        }

        public void Update()
        {
            double currentTime = GetTime();
            mDeltaTime = (float)(currentTime - mLastFrameStartTime);
            mLastFrameStartTime = currentTime;
            mFrameStartTimef = (float)mLastFrameStartTime;
        }

        public float GetDeltaTime() { return mDeltaTime; }

        public double GetTime()
        {
            return (DateTime.UtcNow - sStartTime).TotalMilliseconds/1000;
        }

        public float GetTimef()
        {
            return (float)GetTime();
        }

        public float GetFrameStartTime() { return mFrameStartTimef; }
    }
}
