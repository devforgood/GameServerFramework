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
using Newtonsoft.Json;
using Microsoft.Extensions.Logging;

namespace GmTool.Pages.Members
{
    public class SuspendChannelModel : PageModel
    {
        private readonly GmTool.Models.CommonContext _context;
        private readonly ILogger _logger;


        public SuspendChannelModel(GmTool.Models.CommonContext context, ILogger<SuspendChannelModel> logger)
        {
            _context = context;
            _logger = logger;
        }


        [BindProperty]
        public string channel_id { get; set; }


        [BindProperty]
        public bool is_suspend { get; set; }


        public async Task<IActionResult> OnGetAsync(string id, bool action)
        {
            channel_id = id;
            is_suspend = action;

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


            var msg = new ServerCommon.InternalMessage();
            if(is_suspend)
                msg.message_type = (byte)ServerCommon.InternalMessageType.SuspendChannel;
            else
                msg.message_type = (byte)ServerCommon.InternalMessageType.ResumeChannel;


            var pubMessage = JsonConvert.SerializeObject(msg);
            await Cache.Instance.GetSubscriber().PublishAsync($"channel_msg:{channel_id}", pubMessage);

            return RedirectToPage("./Index");
        }

        private bool MemberExists(long id)
        {
            return _context.member.Any(e => e.member_no == id);
        }
    }
}
