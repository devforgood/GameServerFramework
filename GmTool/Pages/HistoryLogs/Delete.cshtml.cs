using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.HistoryLogs
{
    public class DeleteModel : PageModel
    {
        private readonly GmTool.Models.LogContext _context;

        public DeleteModel(GmTool.Models.LogContext context)
        {
            _context = context;
        }

        [BindProperty]
        public HistoryLog HistoryLog { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            HistoryLog = await _context.history_log.FirstOrDefaultAsync(m => m.idx == id);

            if (HistoryLog == null)
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

            HistoryLog = await _context.history_log.FindAsync(id);

            if (HistoryLog != null)
            {
                _context.history_log.Remove(HistoryLog);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
