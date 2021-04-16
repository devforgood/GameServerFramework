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
    public class LogContext : DbContext
    {
        public DbSet<HistoryLog> history_log { get; set; }

        //public string ConnectionString { get; set; }

        //public LogContext(string connectionString)
        //{
        //    this.ConnectionString = connectionString;
        //}

        //private MySqlConnection GetConnection()
        //{
        //    return new MySqlConnection(ConnectionString);
        //}

        public LogContext(DbContextOptions<LogContext> options)
: base(options)
        {
        }

        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
            
        //    var builder = new ConfigurationBuilder().AddJsonFile("appsettings.json",  optional: true, reloadOnChange: true);
        //    var config = builder.Build();

        //    optionsBuilder.UseMySQL(config.GetConnectionString("LogContext"));
        //}

        //protected override void OnModelCreating(ModelBuilder modelBuilder)
        //{
        //    modelBuilder.Entity<HistoryLog>()
        //        .HasKey(x => new { x.idx, x.addtime });
        //}

    }
}
