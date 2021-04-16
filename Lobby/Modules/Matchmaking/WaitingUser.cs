using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace Lobby
{
    public class WaitingUser
    {
        public int map_id { get; set; }

        public int rank { get; set; }
    }
}
