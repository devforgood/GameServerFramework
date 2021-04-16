using System;
using System.Collections.Generic;
using System.Text;

namespace GmTool.rank.getList.request
{
    class msg
    {
        public string leaderboardId;
        public int seasonSeq;
        public int fromRank;
        public int toRank;
        public bool withoutProperty;
        public string playerId;
        public string subkey;
        public CacheProperty cacheProperty;
    }

    class CacheProperty
    {
        public int ttlSec;
    }
}
