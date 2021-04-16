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

namespace GmTool.Pages.LeaderBoards.LeaderBoardRewards
{
    public class EditModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public EditModel(GmTool.Models.CommonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public LeaderBoardReward LeaderBoardReward { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LeaderBoardReward = await _context.leader_board_reward.FirstOrDefaultAsync(m => m.leader_board_reward_no == id);

            if (LeaderBoardReward == null)
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

            _context.Attach(LeaderBoardReward).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!LeaderBoardRewardExists(LeaderBoardReward.leader_board_reward_no))
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

        private bool LeaderBoardRewardExists(long id)
        {
            return _context.leader_board_reward.Any(e => e.leader_board_reward_no == id);
        }
    }
}
