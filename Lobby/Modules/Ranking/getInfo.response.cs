using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby.getInfo.response
{
    public class msg
    {
        public string appId;
        public string leaderboardId;
        public string desc;
        public Season season;
        public string sortingType;
        public bool friendsLeaderboard;
        public string recordType;
        public int reserveSeasons;
        public string regUser;
        public long regTime;
        public int seasonSeq;
        public long seasonStartTime;
        public long seasonEndTime;
    }

    public class Season
    {
        public string type;
        public int resetDay;
        public int resetHour;
        public long beginTime;
        public long endTime;
        public long nextResetTime;
        public int seq;
    }
}
