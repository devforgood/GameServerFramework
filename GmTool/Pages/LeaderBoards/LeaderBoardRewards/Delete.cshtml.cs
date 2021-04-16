using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.LeaderBoards.LeaderBoardRewards
{
    public class DeleteModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public DeleteModel(GmTool.Models.CommonContext context)
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

        public async Task<IActionResult> OnPostAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            LeaderBoardReward = await _context.leader_board_reward.FindAsync(id);

            if (LeaderBoardReward != null)
            {
                _context.leader_board_reward.Remove(LeaderBoardReward);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
