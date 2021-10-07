using System;
using System.Collections.Generic;
using System.Text;

namespace WebAPIClient
{
    public class RquestRemovedPlayers
    {
        public long dayTimeSlot { get; set; }
        public int bucketFrom { get; set; }
        public int size { get; set; }
    }
}
