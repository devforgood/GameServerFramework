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
    public class IndexModel : PageModel
    {
        private readonly GmTool.Models.LogContext _context;
        private readonly GmTool.Models.CommonContext _commonContext;

        public IndexModel(GmTool.Models.LogContext context, GmTool.Models.CommonContext commonContext)
        {
            _context = context;
            _commonContext = commonContext;

            StartDate = DateTime.Now;
            EndDate = DateTime.Now;
        }

        public IList<HistoryLog> HistoryLog { get;set; }


        [BindProperty(SupportsGet = true)]
        public string SearchString { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime EndDate { get; set; }

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(SearchString))
            {
                var Member = await _commonContext.member.Where(x => x.user_name == SearchString).FirstOrDefaultAsync();
                HistoryLog = await _context.history_log.Where(x => x.submit_time >=StartDate && x.submit_time < EndDate && x.user_no == Member.user_no).ToListAsync();

            }
            else
            {
                //HistoryLog = await _context.history_log.Take(1000).ToListAsync();
                HistoryLog = new List<HistoryLog>();
            }
        }
    }
}
