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

namespace GmTool.Pages.HistoryLogs
{
    public class EditModel : PageModel
    {
        private readonly GmTool.Models.LogContext _context;

        public EditModel(GmTool.Models.LogContext context)
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

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.Attach(HistoryLog).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!HistoryLogExists(HistoryLog.idx))
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

        private bool HistoryLogExists(long id)
        {
            return _context.history_log.Any(e => e.idx == id);
        }
    }
}
