using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.rank.get.response
{
    public class msg
    {
        public string playerId;
        public int rank;
        public long score;
        public long highscore;
        public int cardinality;
        public Dictionary<string, string> property;
        public int seasonSeq;
    }
}
