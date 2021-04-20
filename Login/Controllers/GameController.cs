using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Login.Controllers
{
    public class GameController : Controller
    {
        private readonly CommonContext _context;
        private readonly ILogger<GameController> _logger;

        public GameController(ILogger<GameController> logger, CommonContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> GameResult(string session_id, int coin)
        {
            try
            {
                Server.Session session = await Server.Session.GetSession(session_id);
                if(session == null)
                {
                    _logger.LogError($"SelectCharacter session is null {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.LostSession }));
                }


                // db update
                var member = await _context.member.Where(x => x.member_no == session.member_no).FirstOrDefaultAsync();
                if(member == default)
                {
                    _logger.LogError($"SelectCharacter member no is null {session.member_no}, {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.NotExist }));
                }
                member.coin += coin;
                await _context.SaveChangesAsync();

                // cache update
                session.coin += coin;
                await Server.Session.SetSession(session);


                var reply = new Login.SelectCharacterReply();
                reply.selected_character = session.selected_character;
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
