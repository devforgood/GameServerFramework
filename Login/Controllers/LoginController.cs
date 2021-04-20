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
    public class LoginController : Controller
    {
        private readonly CommonContext _context;
        private readonly ILogger<LoginController> _logger;

        public LoginController(ILogger<LoginController> logger, CommonContext context)
        {
            _logger = logger;
            _context = context;
        }
        //[HttpPost()]
        public async Task<IActionResult> Index(string player_id)
        {
            try
            {
                Member member = null;
                int[] characters = null;

                Server.Session session = await Server.Session.GetSessionByPlayerId(player_id);
                if (session == null)
                {
                    member = await _context.member.AsNoTracking().Where(x => x.player_id == player_id).FirstOrDefaultAsync();
                    if (member == default)
                    {
                        member = new Member();
                        member.player_id = player_id;
                        member.selected_character = 1;
                        member.characters = "[1]";
                        member.coin = 0;
                        await _context.member.AddAsync(member);
                        await _context.SaveChangesAsync();
                    }
                    characters = JsonConvert.DeserializeObject<int[]>(member.characters);

                    session = new Server.Session()
                    {
                        member_no = member.member_no,
                        player_id = member.player_id,
                        selected_character = member.selected_character,
                        characters = characters,
                        coin = member.coin,
                    };


                    await Server.Session.SetSession(session);
                }


                var reply = new Login.LoginReply();
                reply.selected_character = session.selected_character;
                reply.coin = session.coin;
                reply.characters = session.characters;
                reply.session_id = session.session_id;
                _logger.LogInformation($"Login member_no : {session.member_no}, player_id : {session.player_id}, session_id : {session.session_id}"); ;
                return Content(JsonConvert.SerializeObject(reply));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.ToString());
            }
            return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.Unknown }));
        }

        public async Task<IActionResult> SelectCharacter(string session_id, int character_id)
        {
            try
            {
                Server.Session session = await Server.Session.GetSession(session_id);
                if(session == null)
                {
                    _logger.LogError($"SelectCharacter session is null {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.LostSession }));
                }

                if(session.selected_character == character_id)
                {
                    _logger.LogError($"SelectCharacter character is already selected {character_id}, {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.Already }));
                }

                if (session.characters.Where(x => x == character_id).FirstOrDefault()==default)
                {
                    _logger.LogError($"SelectCharacter character is not exist {character_id}, {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.NotExist }));
                }


                // db update
                var member = await _context.member.Where(x => x.member_no == session.member_no).FirstOrDefaultAsync();
                if(member == default)
                {
                    _logger.LogError($"SelectCharacter member no is null {session.member_no}, {session_id}");
                    return Content(JsonConvert.SerializeObject(new BaseReply() { errorCode = ErrorCode.NotExist }));
                }
                member.selected_character = character_id;
                await _context.SaveChangesAsync();

                // cache update
                session.selected_character = character_id;
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
