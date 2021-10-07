using System;
using System.Collections.Generic;
using System.Text;

namespace WebAPIClient
{

    public class RemovedPlayer
    {
        public long dayTimeSlot { get; set; }
        public string playerId { get; set; }
        public string appId { get; set; }
        public long regTime { get; set; }
        public long removedTime { get; set; }
        public string beforeAppStatus { get; set; }
        public string idpCode { get; set; }
        public string idpId { get; set; }
        public string removeReason { get; set; }
    }

    public class ResponseRemovedPlayers
    {
        public int? nextBucket { get; set; }
        public RemovedPlayer[] players {get;set;}

    }
}
