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
    public class DetailsModel : PageModel
    {
        private readonly GameContext _context;

        public DetailsModel(GameContext context)
        {
            _context = context;
        }

        public User User { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            User = await _context.user.FirstOrDefaultAsync(m => m.user_no == id);

            if (User == null)
            {
                return NotFound();
            }
            return Page();
        }
    }
}
