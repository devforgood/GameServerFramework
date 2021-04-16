using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.Mailbox.SearchMail
{
    public class IndexModel : PageModel
    {
        private readonly GmTool.Models.GameContext _context;
        private readonly CommonContext _commonContext;

        public IndexModel(GmTool.Models.GameContext context, CommonContext commonContext)
        {
            _context = context;
            _commonContext = commonContext;
        }

        public IList<Mail> Mail { get;set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }
        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(SearchString))
            {
                var Member = await _commonContext.member.Where(x => x.user_name == SearchString).FirstOrDefaultAsync();
                if (Member != default)
                {
                    Mail = await _context.mailbox.Where(x => x.user_no == Member.user_no).ToListAsync();
                }
                else
                {
                    Mail = new List<Mail>();
                }
            }
            else
            {
                Mail = new List<Mail>();
            }
        }
    }
}
