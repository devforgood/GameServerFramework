using GameService;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class MailQuery
    {
        public static async Task<List<Models.SystemMail>> GetSystemMail(DateTime last_play_time)
        {
            try
            {
                using (var context = new Lobby.Models.CommonContext())
                {
                    return await context.system_mail.Where(x => x.submit_time > last_play_time).AsNoTracking().ToListAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "GetSystemMail");
            }
            return null;
        }

        public static async Task<Models.Mail> SendMail(long member_no, Models.Mail mail)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    context.mailbox.Add(mail);
                    await context.SaveChangesAsync();
                    return mail;
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "SendMail");
            }
            return null;
        }

        public static async Task<List<Models.Mail>> GetMail(long member_no, long user_no, int skip, int take, List<MailState> states)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    if (states != null)
                        return await context.mailbox.Where(x => x.user_no == user_no && states.Contains(x.mail_state)).Skip(skip).Take(take).AsNoTracking().ToListAsync();
                    else
                        return await context.mailbox.Where(x => x.user_no == user_no).Skip(skip).Take(take).AsNoTracking().ToListAsync();
                }

            }
            catch (Exception e)
            {
                Log.Error(e, "GetMail");
            }
            return null;
        }
    }
}
