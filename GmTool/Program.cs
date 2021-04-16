using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GmTool
{
    public class Program
    {
        private static async Task<string> EnsureUser(IServiceProvider serviceProvider,
                                            string testUserPw, string UserName)
        {
            var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

            var user = await userManager.FindByNameAsync(UserName);
            if (user == null)
            {
                user = new IdentityUser
                {
                    UserName = UserName,
                    EmailConfirmed = true
                };
                await userManager.CreateAsync(user, testUserPw);
            }

            if (user == null)
            {
                throw new Exception("The password is probably not strong enough!");
            }

            return user.Id;
        }

        private static async Task<IdentityResult> EnsureRole(IServiceProvider serviceProvider,
                                                                      string uid, string role)
        {
            IdentityResult IR = null;
            var roleManager = serviceProvider.GetService<RoleManager<IdentityRole>>();

            if (roleManager == null)
            {
                throw new Exception("roleManager null");
            }

            if (!await roleManager.RoleExistsAsync(role))
            {
                IR = await roleManager.CreateAsync(new IdentityRole(role));
            }

            var userManager = serviceProvider.GetService<UserManager<IdentityUser>>();

            var user = await userManager.FindByIdAsync(uid);

            if (user == null)
            {
                throw new Exception("The testUserPw password was probably not strong enough!");
            }

            IR = await userManager.AddToRoleAsync(user, role);

            return IR;
        }

        public static async Task Initialize(IServiceProvider serviceProvider, string testUserPw)
        {
            //using (var context = new ApplicationDbContext(
            //    serviceProvider.GetRequiredService<DbContextOptions<ApplicationDbContext>>()))
            {
                // For sample purposes seed both with the same password.
                // Password is set with the following:
                // dotnet user-secrets set SeedUserPW <pw>
                // The admin user can do anything


                var adminID = await EnsureUser(serviceProvider, testUserPw, "admin@friendsgames.com");
                await EnsureRole(serviceProvider, adminID, "ContactAdministratorsRole");

                // allowed user can create and edit contacts that they create
                var managerID = await EnsureUser(serviceProvider, testUserPw, "manager@friendsgames.com");
                await EnsureRole(serviceProvider, managerID, "ContactManagersRole");

                for (int i = 0; i < 100; ++i)
                {
                    var gmID = await EnsureUser(serviceProvider, testUserPw, $"gm{i+1}@friendsgames.com");
                    await EnsureRole(serviceProvider, managerID, "ContactGameManagersRole");
                }

                //SeedDB(context, adminID);
            }
        }
        public static void Main(string[] args)
        {
            //var dir = System.IO.Directory.GetCurrentDirectory();
            //Dictionary<string, string> Scripts = new Dictionary<string, string>();
            //JsonData.Instance.LoadOriginalData(Scripts);
            //JsonData.Instance.LoadData(true, true);

            var host = CreateHostBuilder(args).Build();

            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;

                try
                {

                    // requires using Microsoft.Extensions.Configuration;
                    var config = host.Services.GetRequiredService<IConfiguration>();
                    // Set password with the Secret Manager tool.
                    // dotnet user-secrets set SeedUserPW <pw>

                    var testUserPw = "AceStudio@79";
                    Initialize(services, testUserPw).Wait();

                    Cache.RedisIpAddress = config.GetSection("redis:ip").Value;
                    Cache.RedisPort = config.GetSection("redis:port").Value;

                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred seeding the DB.");
                }
            }

            UnitTest.UnitTest.Test();
            host.Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<BackgroundTasks.Services.TimedHostedService>();
                });
    }
}
