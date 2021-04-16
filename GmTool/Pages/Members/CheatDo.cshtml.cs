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
using Microsoft.Extensions.Logging;

namespace GmTool.Pages.Members
{
    public class CheatDoModel : PageModel
    {
        private readonly CommonContext _context;
        private readonly ILogger _logger;


        public CheatDoModel(CommonContext context, ILogger<CheatDoModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public JDebugCommandData Command { get; set; }

        [BindProperty]
        public string PlayerId { get; set; }

        [BindProperty]
        public long member_no { get; set; }

        [BindProperty]
        public string Param1 { get; set; }

        [BindProperty]
        public string Param2 { get; set; }

        [BindProperty]
        public string Param3 { get; set; }

        [BindProperty]
        public string Param4 { get; set; }

        [BindProperty]
        public string CommandString { get; set; }

        [BindProperty]
        public string Target { get; set; }

        public async Task<IActionResult> OnGetAsync(long id, string playerid, int cmd)
        {
            member_no = id;
            PlayerId = playerid;

            var cmd_list = JsonConvert.DeserializeObject<IList<JDebugCommandData>>(await Cache.Instance.GetDatabase().HashGetAsync("scripts", "DebugCommand"));
            Command = cmd_list.Where(x => x.ID == cmd).FirstOrDefault();

            Param1 = Command.DefaultValue1;
            Param2 = Command.DefaultValue2;
            Param3 = Command.DefaultValue3;
            Param4 = Command.DefaultValue4;
            CommandString = Command.Command;
            Target = Command.Target;

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

            await DebugCommand.Execute(Target, PlayerId, CommandString, Param1, Param2, Param3, Param4, _logger);

            return RedirectToPage("./Cheat", new { id = member_no });
        }

        private bool MemberExists(long id)
        {
            return _context.member.Any(e => e.member_no == id);
        }
    }
}
