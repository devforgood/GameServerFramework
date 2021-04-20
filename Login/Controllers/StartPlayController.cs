using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login.Controllers
{
    public class StartPlayController : Controller
    {
        private readonly ILogger<StartPlayController> _logger;

        public StartPlayController(ILogger<StartPlayController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index(string session_id, int map_id)
        {
            try
            {
                Server.Session session = await Server.Session.GetSession(session_id);
                if (session == null)
                {
                    _logger.LogError($"StartPlay session is null {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.LostSession }));
                }

                (bool ret, string server_addr, byte worldId, string channel_key, string channel_id) = await ServerCommon.ChannelLoader.GetAvailableServer(map_id);

                var reply = new Login.StartPlayReply();
                reply.worldid = worldId;
                reply.addr = server_addr;
                return Content(JsonConvert.SerializeObject(reply));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.Unknown }));
        }
    }
}
