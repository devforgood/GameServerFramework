using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;
using Newtonsoft.Json;

namespace GmTool.Pages.GameServerState
{
    public class IndexModel : PageModel
    {
        public IndexModel()
        {
        }

        public IList<ServerCommon.Channel> Channels { get;set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task OnGetAsync()
        {
            Channels = new List<ServerCommon.Channel>();

            var maps = JsonConvert.DeserializeObject<IList<JMapData>>(await Cache.Instance.GetDatabase().HashGetAsync("scripts", "Map"));
            foreach(var map in maps)
            {
                var mapId = map.ID;
                var entry = await Cache.Instance.GetDatabase().HashGetAllAsync($"channel_info:{mapId}");
                for (int i = 0; i < entry.Length; ++i)
                {
                    var ch = JsonConvert.DeserializeObject<ServerCommon.Channel>(entry[i].Value);
                    var channel_state = await Cache.Instance.GetDatabase().StringGetAsync(ch.channel_id);
                    if (channel_state.HasValue == false)
                    {
                        continue;
                    }
                    ch.map_id = mapId;
                    Channels.Add(ch);
                }
            }

        }
    }
}
