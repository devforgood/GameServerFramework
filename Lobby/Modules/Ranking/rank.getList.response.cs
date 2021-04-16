using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.rank.getList.response
{
    public class msg
    {
        public List<Score> scores;
        public int seasonSeq;
        public int cardinality;
        public long nextResetTime;
        public int myRank;
        public long myScore;
        public Dictionary<string, string> myProperty;
    }

    public class Score
    {
        public int rank;
        public string playerId;
        public long score;
        public Dictionary<string, string> property;
    }

}
