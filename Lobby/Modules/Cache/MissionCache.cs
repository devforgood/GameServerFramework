using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class MissionCache : UserSubStorage<Models.Mission, MissionQuery>
    {

        public static MissionCache Instance = new MissionCache();

        MissionCache()
        {
            query = new MissionQuery();
            entities_name = "missions";
            entity_name = "mission";
        }
    }
}
