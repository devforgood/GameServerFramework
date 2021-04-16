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

namespace GmTool.Pages.Mailbox.SendMail
{
    public class EditModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public EditModel(GmTool.Models.CommonContext context)
        {
            _context = context;
        }

        [BindProperty]
        public SystemMail SystemMail { get; set; }

        public async Task<IActionResult> OnGetAsync(long? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            SystemMail = await _context.system_mail.FirstOrDefaultAsync(m => m.system_mail_no == id);

            if (SystemMail == null)
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

            _context.Attach(SystemMail).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SystemMailExists(SystemMail.system_mail_no))
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

        private bool SystemMailExists(long id)
        {
            return _context.system_mail.Any(e => e.system_mail_no == id);
        }
    }
}
