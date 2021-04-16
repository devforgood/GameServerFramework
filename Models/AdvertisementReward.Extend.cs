using System;
using System.Collections.Generic;
using System.Text;


namespace Lobby.Models
{
    public partial class AdvertisementReward
    {
        public void Copy(AdvertisementReward other)
        {
            advertisement_id = other.advertisement_id;
            reward = other.reward;
            occ_time = other.occ_time;
        }
    }
}
