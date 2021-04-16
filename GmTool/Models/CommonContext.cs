using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Lobby.Models;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace GmTool.Models
{
    public class CommonContext : DbContext
    {
        public static string ConnectionString;

        public DbSet<Member> member { get; set; }
        public DbSet<BannedWord> banned_word { get; set; }
        public DbSet<SystemMail> system_mail { get; set; }
        public DbSet<LeaderBoard> leader_board { get; set; }
        public DbSet<LeaderBoardReward> leader_board_reward { get; set; }

        public CommonContext(DbContextOptions<CommonContext> options) : base(options)
        {
        }


        #region 디비 컨텍스트 직접 생성, 페이지를 제외한 곳에서 사용하기 위한 용도
        public CommonContext()
        {

        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseMySQL(ConnectionString);
        }
        #endregion
    }
}
