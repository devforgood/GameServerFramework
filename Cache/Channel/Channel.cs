using System;
using System.Collections.Generic;
using System.Text;

namespace ServerCommon
{
    public enum ChannelState : uint
    {
        CHL_READY = 0,
        CHL_BUSY = 1,
        CHL_SUSPEND = 2,
    }

    public class Channel
    {
        public string channel_id;
        public string server_addr;
        public ChannelState channel_state;
        public byte world_id;
        public int user_count;
        public DateTime submit_time;
        public int map_id;
    }
}
