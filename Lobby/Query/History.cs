using System;
using System.Threading.Tasks;
using Serilog;

namespace Lobby
{
    public class History
    {
        public static void Info(long memberNo, long userNo, long characterNo, HistoryLogAction action, byte reason, int param1, int param2, string param3, string param4)
        {
            using (var context = new Lobby.Models.LogContext(memberNo))
            {
                try
                {
                    context.history_log.Add(new Lobby.Models.HistoryLog()
                    {
                        user_no = userNo,
                        character_no = characterNo,
                        action = (byte)action,
                        reason = reason,
                        param1 = param1,
                        param2 = param2,
                        param3 = param3,
                        param4 = param4,
                        submit_time = DateTime.UtcNow,
                    });

                    context.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error($"{e.ToString()}");
                }
            }
        }
    }
}
