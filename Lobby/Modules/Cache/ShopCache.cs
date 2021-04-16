using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class ShopCache : UserSubStorage<Models.Shop, ShopQuery>
    {
        public static ShopCache Instance = new ShopCache();

        ShopCache()
        {
            query = new ShopQuery();
            entities_name = "shops";
            entity_name = "shop";
        }
    }
}
