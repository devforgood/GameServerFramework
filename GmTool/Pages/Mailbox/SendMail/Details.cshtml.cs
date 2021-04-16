using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using GmTool.Models;
using Lobby.Models;

namespace GmTool.Pages.Mailbox.SendMail
{
    public class DetailsModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public DetailsModel(GmTool.Models.CommonContext context)
        {
            _context = context;
        }

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
    }
}
