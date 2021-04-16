using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class ActionLog : PlayerLog
    {
        public string category { get; set; }
        public string action { get; set; }
        public string label { get; set; }
        public string actionAttr1 { get; set; }
        public string actionAttr2 { get; set; }
        public string gameLogYn { get; set; }
        public long modTime { get; set; }

        public ActionLog(PlayerLog player = null) : base(player)
        {
        }


    }
}
