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
    public class AdvertisementRewardQuery
    {

        public static async Task<IList<Models.AdvertisementReward>> Gets(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.advertisement_reward.Where(x => x.user_no == user_no).AsNoTracking().ToListAsync();
            }
        }

        public static async Task<Models.AdvertisementReward> Add(long member_no, Models.AdvertisementReward entity)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.advertisement_reward.Add(entity);
                await context.SaveChangesAsync();
                return entity;
            }
        }

        public static async Task<bool> Update(long member_no, Models.AdvertisementReward entity)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var row = await context.advertisement_reward.Where(x => x.advertisement_no == entity.advertisement_no).FirstOrDefaultAsync();
                    if (row != null && row != default(Models.AdvertisementReward))
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
