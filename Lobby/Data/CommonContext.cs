using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Serilog;

namespace Lobby.Models
{
    public class CommonContext : DbContext
    {
#if DEBUG
        public static readonly ILoggerFactory MyLoggerFactory  = LoggerFactory.Create(builder =>
        {
            //builder
            //    .AddFilter("Microsoft", LogLevel.Warning)
            //    .AddFilter("System", LogLevel.Warning)
            //    .AddFilter("LoggingConsoleApp.Program", LogLevel.Debug)
            //    .AddConsole()
            //    .AddEventLog();
            builder
                .AddFilter((category, level) =>
                    category == DbLoggerCategory.Database.Command.Name
                    && level == LogLevel.Information)
                .AddConsole()
                .AddDebug()
                .AddSerilog()
                ;

        });
#endif

        public DbSet<Lobby.Models.Member> member { get; set; } = null!;

        public DbSet<Lobby.Models.BannedWord> banned_word { get; set; } = null!;
        public DbSet<Lobby.Models.SystemMail> system_mail { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
#if DEBUG
                .UseLoggerFactory(MyLoggerFactory)
#endif
                //.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking) // 읽기 전용 (https://docs.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.dbcontextoptionsbuilder.usequerytrackingbehavior?view=efcore-3.1)
                .UseMySQL(ServerConfiguration.Instance.CommonContext);
        }

        public static void test()
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                var ret = context.member
                    .TagWith("This is my spatial query!")
                    .Where(x => x.member_no == 1).ToList();
            }

            var result = GetMember().Result;


            //TryInsert();

            var result2 = GetMember2().Result;

            var last_member = MemberQuery.GetMember("G310f55c2d9ad4c068a80fb0bcdc3d96e").Result;

        }

        private static async Task<List<Member>> GetMember2()
        {
            //string player_id = "' OR 1=1 -- ";
            //string player_id = "12 OR 1=1 -- ";
            string player_id = "123";

            using (var context = new Lobby.Models.CommonContext())
            {
                //return await context.member.FromSql($"SELECT * FROM member").ToListAsync();
                //string query = $"SELECT * FROM member where player_id = '{player_id.GetEscapeString()}'";
                //string query = $"SELECT * FROM member where player_id = {player_id.GetEscapeString()}";
                //return await context.member.FromSql(query).ToListAsync();

                return await context.member.FromSqlRaw("SELECT * FROM member where player_id =@player_id", new MySqlParameter("@player_id", player_id) ).ToListAsync();
            }
        }
        private static async Task TryInsert()
        {
            int i = 24;
            bool saveFail;
            do
            {
                saveFail = false;
                try
                {
                    // context 예외 발생시 재사용이 안되므로 다시 할당해야 한다
                    using (var context = new Lobby.Models.CommonContext())
                    {
                        var member = new Member();
                        member.user_no = 2;
                        member.player_id = $"12{i++}";
                        member.game_token = "aaaaaa";
                        member.last_play_time = DateTime.UtcNow;

                        context.member.Add(member);
                        await context.SaveChangesAsync();
                        Log.Information($"member_no {member.member_no}");
                    }
                }
                catch (DbUpdateException ex)
                {
                    if (ex.InnerException != null && ((MySqlException)ex.InnerException).Number == 1062)
                    {
                        saveFail = true;
                    }
                    else
                    {
                        Log.Information(ex.ToString());
                        throw ex;
                    }
                }
                catch(Exception ex)
                {
                    Log.Information(ex.ToString());
                    throw ex;
                }
            }
            while (saveFail);
        }

        private static async Task<Member> GetMember()
        {
            using (var context = new Lobby.Models.CommonContext())
            {
                return await context.member.FromSqlRaw("SELECT * FROM member").FirstOrDefaultAsync();
            }
        }
    }
}
