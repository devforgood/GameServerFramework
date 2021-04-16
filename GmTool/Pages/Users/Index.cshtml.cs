using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.Users
{
    public class IndexModel : PageModel
    {
        private readonly GameContext _context;
        private readonly CommonContext _commonContext;


        public IndexModel(GameContext context, CommonContext commonContext)
        {
            _context = context;
            _commonContext = commonContext;
        }

        public IList<User> User { get;set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(SearchString))
            {
                var Member = await _commonContext.member.Where(x => x.user_name == SearchString).FirstOrDefaultAsync();
                User = await _context.user.Where(x=>x.user_no == Member.user_no).ToListAsync();

            }
            else
            {
                //User = await _context.user.Take(1000).ToListAsync();
                User = new List<User>();

            }
        }
    }
}
