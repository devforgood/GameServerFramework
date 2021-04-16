using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.HistoryLogs
{
    public class CreateModel : PageModel
    {
        private readonly GmTool.Models.LogContext _context;

        public CreateModel(GmTool.Models.LogContext context)
        {
            _context = context;
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        [BindProperty]
        public HistoryLog HistoryLog { get; set; }

        // To protect from overposting attacks, enable the specific properties you want to bind to, for
        // more details, see https://aka.ms/RazorPagesCRUD.
        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            _context.history_log.Add(HistoryLog);
            await _context.SaveChangesAsync();

            return RedirectToPage("./Index");
        }
    }
}
