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
    public class IndexModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;

        public IndexModel(GmTool.Models.CommonContext context)
        {
            _context = context;
        }

        public IList<SystemMail> SystemMail { get;set; }

        public async Task OnGetAsync()
        {
            SystemMail = await _context.system_mail.ToListAsync();
        }
    }
}
