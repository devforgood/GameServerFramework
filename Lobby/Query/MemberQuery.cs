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
    public class MemberQuery
    {
        public static async Task<Models.Member> GetMember(string player_id)
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                return await context.member.Where(x => x.player_id == player_id).AsNoTracking().FirstOrDefaultAsync();
                //return await context.member.FromSqlRaw("seLect * FROM member WHERE player_id =@player_id", new MySqlParameter("@player_id", player_id)).AsNoTracking().FirstOrDefaultAsync();
            }
        }

        public static async Task<Models.Member> AddMember(Models.Member member)
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                await context.member.AddAsync(member);
                await context.SaveChangesAsync();
                return member;
            }
        }
        public static async Task UpdateMember(long member_no, long user_no)
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                await context.Database.ExecuteSqlRawAsync($"upDate member set user_no = {user_no} where member_no = {member_no}");
            }
        }

        public static async Task<ErrorCode> UpdateMember(long member_no, string user_name)
        {
            try
            {
                using (var context = new Lobby.Models.CommonContext())
                {
                    var user = await context.member.Where(x => x.member_no == member_no).FirstOrDefaultAsync();
                    if (user == null && user == default(Models.Member))
                    {
                        Log.Error($"UpdateUser find member {member_no}");
                        return ErrorCode.WrongParam;
                    }
                    user.user_name = user_name;
                    await context.SaveChangesAsync();
                }

                // sql injection
                //using (var context = new Lobby.Models.GameContext())
                //{
                //    await context.Database.ExecuteSqlCommandAsync($"upDate user set user_name = {user_name} where user_no = {user_no}");
                //}
            }
            catch (DbUpdateException ex)
            {
                if (ex.InnerException != null && ((MySqlException)ex.InnerException).Number == 1062)
                {
                    return ErrorCode.Duplicate;
                }
                else
                {
                    Log.Error($"{ex.ToString()}");
                    return ErrorCode.WrongParam;
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.ToString()}");
                return ErrorCode.WrongParam;
            }
            return ErrorCode.Success;
        }

        public static async Task<ErrorCode> UpdateMemberLastPlayTime(long member_no, DateTime currentTime)
        {
            try
            {
                using (var context = new Lobby.Models.CommonContext())
                {
                    var user = await context.member.Where(x => x.member_no == member_no).FirstOrDefaultAsync();
                    if (user == null && user == default(Models.Member))
                    {
                        Log.Error($"UpdateUser find member {member_no}");
                        return ErrorCode.WrongParam;
                    }
                    user.last_play_time = currentTime;
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error($"{ex.ToString()}");
                return ErrorCode.WrongParam;
            }
            return ErrorCode.Success;
        }
    }
}
