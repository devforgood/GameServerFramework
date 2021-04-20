using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Configuration;

namespace Login
{
    public class CommonContext : DbContext
    {
        public DbSet<Member> member { get; set; }

        public CommonContext(DbContextOptions<CommonContext> options) : base(options)
        {
        }

    }
}
