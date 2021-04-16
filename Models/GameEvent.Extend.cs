using System;
using System.Collections.Generic;
using System.Text;


namespace Lobby.Models
{
    public partial class GameEvent
    {
        public void Copy(GameEvent other)
        {
            event_id = other.event_id;
            reward = other.reward;
            occ_time = other.occ_time;
        }
    }
}
