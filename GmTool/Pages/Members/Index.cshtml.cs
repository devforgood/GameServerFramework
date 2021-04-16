using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.Members
{
    public class IndexModel : PageModel
    {
        private readonly CommonContext _context;

        public IndexModel(CommonContext context)
        {
            _context = context;
        }

        public IList<Member> Member { get;set; }

        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(SearchString))
            {
                Member = await _context.member.Where(x=>x.user_name == SearchString).ToListAsync();
            }
            else
            {
                Member = new List<Member>();
            }
        }
    }
}
