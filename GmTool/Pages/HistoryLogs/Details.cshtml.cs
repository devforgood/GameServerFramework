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
    public class DetailsModel : PageModel
    {
        private readonly GmTool.Models.LogContext _context;

        public DetailsModel(GmTool.Models.LogContext context)
        {
            _context = context;
        }

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
    }
}
