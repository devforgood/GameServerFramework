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
    public class DetailsModel : PageModel
    {
        private readonly CommonContext _context;

        public DetailsModel(CommonContext context)
        {
            _context = context;
        }

        public Member Member { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Member = await _context.member.FirstOrDefaultAsync(m => m.member_no == id);

            if (Member == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
