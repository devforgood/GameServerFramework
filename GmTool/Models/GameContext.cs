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
    public class GameContext : DbContext
    {
        public DbSet<User> user { get; set; }
        public DbSet<Character> character_info { get; set; }
        public DbSet<Mission> mission { get; set; }
        public DbSet<Shop> shop { get; set; }
        public DbSet<Mail> mailbox { get; set; }


        public GameContext(DbContextOptions<GameContext> options) : base(options)
        {
        }

    }
}
