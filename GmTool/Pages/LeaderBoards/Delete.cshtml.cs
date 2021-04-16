using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.LeaderBoards
{
    public class DeleteModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public DeleteModel(GmTool.Models.CommonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LeaderBoard LeaderBoard { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LeaderBoard = await _context.leader_board.FirstOrDefaultAsync(m => m.leader_board_no == id);

            if (LeaderBoard == null)
            {
                return NotFound();
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LeaderBoard = await _context.leader_board.FindAsync(id);

            if (LeaderBoard != null)
            {
                _context.leader_board.Remove(LeaderBoard);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
