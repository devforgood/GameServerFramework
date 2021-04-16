using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;
using Newtonsoft.Json;

namespace GmTool.Pages.Members
{
    public class CheatModel : PageModel
    {
        private readonly CommonContext _context;

        public CheatModel(CommonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Member Member { get; set; }

        [BindProperty]
        public IList<JDebugCommandData> DebugCommand { get; set; }


        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                Member = await _context.member.Take(1).FirstOrDefaultAsync();

            }
            else
            {
                Member = await _context.member.FirstOrDefaultAsync(m => m.member_no == id);
            }

            if (Member == null)
            {
                return NotFound();
            }

            DebugCommand = JsonConvert.DeserializeObject<IList<JDebugCommandData>>(await Cache.Instance.GetDatabase().HashGetAsync("scripts", "DebugCommand"));

            return Page();
        }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await SendMessage(Member.player_id, "InsertItem", "1", "10", "", "");

            return RedirectToPage("./Index");
        }

        public async Task<IActionResult> OnPost2Async()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            await SendMessage(Member.player_id, "InsertItem", "1", "10", "", "");

            return RedirectToPage("./Index");
        }

        private bool MemberExists(long id)
        {
            return _context.member.Any(e => e.member_no == id);
        }

        public async Task SendMessage(string playerId, string Cmd, string Param1, string Param2, string Param3, string Param4)
        {
            var msg = new ServerCommon.DebugCommand();
            msg.player_id = playerId;
            msg.cmd = Cmd;
            msg.param1 = Param1;
            msg.param2 = Param2;
            msg.param3 = Param3;
            msg.param4 = Param4;

            msg.msg_id = (long)Cache.Instance.GetDatabase().StringIncrement("lobby_msg_instance_id");
            await Cache.Instance.GetSubscriber().PublishAsync($"lobby", JsonConvert.SerializeObject(msg));
        }


        public void OnPostView(string playerId, string Command, string Param1, string Param2, string Param3, string Param4)
        {
            //Message = $"View handler fired for {id}";

        }
    }
}
