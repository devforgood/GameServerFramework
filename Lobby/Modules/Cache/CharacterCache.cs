using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class CharacterCache : UserSubStorage<Models.Character, CharacterQuery>
    {
        public static CharacterCache Instance = new CharacterCache();

        CharacterCache()
        {
            query = new CharacterQuery();
            entities_name = "characters";
            entity_name = "character";
        }
    }
}
