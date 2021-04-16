using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.score.getList.response
{
    public class msg
    {
        public int seasonSeq;
        public int cardinality;
        public string sortingType;
        public List<rank.getList.response.Score> scores;
    }
}
