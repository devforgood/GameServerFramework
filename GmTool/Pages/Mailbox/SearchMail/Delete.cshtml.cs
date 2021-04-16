using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.Mailbox.SearchMail
{
    public class DeleteModel : PageModel
    {
        private readonly GmTool.Models.GameContext _context;

        public DeleteModel(GmTool.Models.GameContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Mail Mail { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Mail = await _context.mailbox.FirstOrDefaultAsync(m => m.mail_no == id);

            if (Mail == null)
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

            Mail = await _context.mailbox.FindAsync(id);

            if (Mail != null)
            {
                _context.mailbox.Remove(Mail);
                await _context.SaveChangesAsync();
            }

            return RedirectToPage("./Index");
        }
    }
}
