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
    public class GameEventQuery
    {

        public static async Task<IList<Models.GameEvent>> Gets(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.game_event.Where(x => x.user_no == user_no).AsNoTracking().ToListAsync();
            }
        }

        public static async Task<Models.GameEvent> Add(long member_no, Models.GameEvent entity)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.game_event.Add(entity);
                await context.SaveChangesAsync();
                return entity;
            }
        }

        public static async Task<bool> Update(long member_no, Models.GameEvent entity)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var row = await context.game_event.Where(x => x.event_no == entity.event_no).FirstOrDefaultAsync();
                    if (row != null && row != default(Models.GameEvent))
                    {
                        row.Copy(entity);
                        await context.SaveChangesAsync();
                        return true;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return false;
            }
            return true;
        }



    }
}
