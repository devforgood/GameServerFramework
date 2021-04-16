using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Lobby.Models
{
    public class LogContext : DbContext
    {
        int shard_id_;
        public LogContext(long member_no)
        {
            shard_id_ = Shard.GetShardId(member_no);
        }

        public LogContext(Session session)
        {
            shard_id_ = Shard.GetShardId(session.member_no);
        }
        //public static readonly ILoggerFactory MyLoggerFactory = LoggerFactory.Create(builder =>
        //{
        //    builder
        //        .AddFilter((category, level) =>
        //            category == DbLoggerCategory.Database.Command.Name
        //            && level == LogLevel.Information)
        //        .AddConsole()
        //        .AddDebug()
        //        .AddSerilog()
        //        ;
        //});

        public DbSet<HistoryLog> history_log { get; set; }
        public DbSet<MatchLog> match_log { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                //.UseLoggerFactory(MyLoggerFactory)
                .UseMySQL(ServerConfiguration.Instance.LogContext[shard_id_]);
        }
        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<HistoryLog>()
        //        .HasKey(x => new { x.idx, x.addtime });
        //}

        public static void test()
        {
            History.Info(1, 1, 0, HistoryLogAction.GainPlayPoint, (byte)HistoryLogReason.GameResultReward, 2, 1, "111", "");


            var result = GetHistoryLog(1).Result;


        }

        private static async Task<List<HistoryLog>> GetHistoryLog(long member_no)
        {
            using (var context = new Lobby.Models.LogContext(member_no))
            {
                return await context.history_log.FromSqlRaw("SELECT * FROM history_log").ToListAsync();
            }
        }
    }
}
