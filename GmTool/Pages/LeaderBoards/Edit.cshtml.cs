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

namespace GmTool.Pages.LeaderBoards
{
    public class EditModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public EditModel(GmTool.Models.CommonContext context)
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

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(LeaderBoard).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeaderBoardExists(LeaderBoard.leader_board_no))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return RedirectToPage("./Index");
        }

        private bool LeaderBoardExists(long id)
        {
            return _context.leader_board.Any(e => e.leader_board_no == id);
        }
    }
}
