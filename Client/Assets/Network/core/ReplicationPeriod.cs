using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public class ReplicationPeriod
    {
        public float mTimeBetweenStatePackets = 0.1f;
        public float mTimeOfLastSatePacket = 0f;

        public ReplicationPeriod(float TimeBetweenStatePackets)
        {
            mTimeBetweenStatePackets = TimeBetweenStatePackets;
            mTimeOfLastSatePacket = Timing.sInstance.GetFrameStartTime();
        }

        public bool UpdateSendingStatePacket()
        {
            float time = core.Timing.sInstance.GetFrameStartTime();

            if (time > mTimeOfLastSatePacket + mTimeBetweenStatePackets)
            {
                mTimeOfLastSatePacket = time;
                return true;
            }
            return false;
        }
    }
}
