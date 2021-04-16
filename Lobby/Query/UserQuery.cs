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
    public class UserQuery
    {
        public static async Task UpdateUser(long member_no, long user_no, long character_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                await context.Database.ExecuteSqlRawAsync($"upDate user set character_no = {character_no} where user_no = {user_no}");
            }
        }

        public static async Task<Models.User> GetUser(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                return await context.user.Where(x => x.user_no == user_no).AsNoTracking().FirstOrDefaultAsync();
                //return await context.user.FromSqlRaw($"seLect * FROM user WHERE user_no = {user_no}").AsNoTracking().FirstOrDefaultAsync();
            }
        }

        public static async Task<Models.User> AddUser(long member_no, Models.User user)
        {
            using (var context = new Lobby.Models.GameContext(member_no))
            {
                context.user.Add(user);
                await context.SaveChangesAsync();
                return user;
            }
        }

        public static async Task<bool> UpdateUser(long member_no, Models.User user)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    var row = await context.user.Where(x => x.user_no == user.user_no).FirstOrDefaultAsync();
                    if (row != null && row != default(Models.User))
                    {
                        row.Copy(user);
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

        public static async Task<bool> UpdateUserGrowth(long member_no, Models.User user)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    await context.Database.ExecuteSqlRawAsync($"upDate user set play_point = {user.play_point}, battle_score = {user.battle_score}, user_grade = {user.user_grade} where user_no = {user.user_no}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return false;
            }
            return true;
        }

        public static async Task<bool> UpdateUserGoods(long member_no, Models.User user)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    await context.Database.ExecuteSqlRawAsync($"upDate user set gem = {user.gem}, coin = {user.coin}, battle_coin = {user.battle_coin}, medal = {user.medal}, upgrade_stone = {user.upgrade_stone} where user_no = {user.user_no}");
                }
            }
            catch (Exception e)
            {
                Log.Error($"{e.ToString()}");
                return false;
            }
            return true;
        }

        public static async Task<bool> UpdateUserMedalCharge(long member_no, Models.User user)
        {
            try
            {
                using (var context = new Lobby.Models.GameContext(member_no))
                {
                    await context.Database.ExecuteSqlRawAsync($"upDate user set medal_charge = {user.medal_charge}, medal_charge_time = '{user.medal_charge_time?.ToString("yyyy-MM-dd HH:mm:ss")}' where user_no = {user.user_no}");
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
