using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class MissionQuery : IQuery<Models.Mission>
    {
        public async Task<List<Models.Mission>> Gets(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.mission.AsNoTracking().Where(x => x.user_no == user_no).ToListAsync();
            }
        }

        public async Task<Models.Mission> Get(long member_no, long mission_no)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    return await context.mission.AsNoTracking().Where(x => x.mission_no == mission_no).FirstOrDefaultAsync();
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return null;
            }
        }

        public async Task<Models.Mission> Insert(long member_no, Models.Mission mission)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                await context.mission.AddAsync(mission);
                await context.SaveChangesAsync();
                return mission;
            }
        }

        public async Task<bool> Update(long member_no, Models.Mission mission)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var db_mission = await context.mission.Where(x => x.mission_no == mission.mission_no).FirstOrDefaultAsync();
                    if (db_mission != null && db_mission != default(Models.Mission))
                    {
                        db_mission.Copy(mission);
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

        public static async Task<Models.Mission> Remove(long member_no, Models.Mission mission)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.mission.Remove(mission);
                await context.SaveChangesAsync();
                return mission;
            }
        }
    }
}
