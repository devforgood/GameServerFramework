using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class RoundLog : PlayerLog
    {
        public string gameMode{ get; set; } 
        public string gameModeDtl{ get; set; } 
        public string roundAttr1{ get; set; } 
        public string resultTp{ get; set; } 
        public long resultAmt{ get; set; } 
        public long startTime{ get; set; } 
        public long endTime{ get; set; } 
        public string character1Id{ get; set; } 
        public string character2Id{ get; set; } 
        public string character3Id{ get; set; } 
        public string character4Id{ get; set; } 
        public string character5Id{ get; set; } 
        public string character6Id{ get; set; } 
        public int character1Lv{ get; set; } 
        public int character2Lv{ get; set; } 
        public int character3Lv{ get; set; } 
        public int character4Lv{ get; set; } 
        public int character5Lv{ get; set; } 
        public int character6Lv{ get; set; }
        public string memo { get; set; }
        public long modTime { get; set; }

        public RoundLog(PlayerLog player = null) : base(player)
        {

        }
    }
}
