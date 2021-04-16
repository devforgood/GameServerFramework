using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.score.accumulate.request
{
    public class msg
    {
        public string leaderboardId;
        public long delta;
        public string txCode;
        public int validationCode;
        public string subkey;
    }
}
