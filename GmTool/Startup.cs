using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using GmTool.Data;
using GmTool.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

namespace GmTool
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            var contentRoot = configuration.GetValue<string>(WebHostDefaults.ContentRootKey);
        }

        public IConfiguration Configuration { get; }

        #region snippet_configureservices
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options => options.UseMySQL(Configuration.GetConnectionString("ApplicationDbContext")));
            services.AddDbContext<CommonContext>(options => options.UseMySQL(Configuration.GetConnectionString("CommonContext")));
            CommonContext.ConnectionString = Configuration.GetConnectionString("CommonContext");

            // todo : 샤딩 처리가 필요
            var GameContext = Configuration.GetSection("ConnectionStrings:GameContext").Get<string[]>();

            services.AddDbContext<GameContext>(options => options.UseMySQL(GameContext[0]));
            services.AddDbContext<LogContext>(options => options.UseMySQL(Configuration.GetConnectionString("LogContext")));

            WebAPIClient.Web.url = Configuration.GetSection("idp:url").Value;
            WebAPIClient.Web.version = Configuration.GetSection("idp:version").Value;
            WebAPIClient.Web.appId = Configuration.GetSection("idp:appId").Value;
            WebAPIClient.Web.appSecret = Configuration.GetSection("idp:appSecret").Value;
            WebAPIClient.Web.Authorization = Configuration.GetSection("idp:Authorization").Value;

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddRoles<IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddRazorPages(options =>
            {
                options.Conventions.AuthorizePage("/Privacy");
                options.Conventions.AuthorizeFolder("/Members");
                options.Conventions.AuthorizeFolder("/Users");
                options.Conventions.AuthorizeFolder("/HistoryLogs");
                options.Conventions.AuthorizeFolder("/GameServerState");

            });

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings.
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequireUppercase = true;
                options.Password.RequiredLength = 6;
                options.Password.RequiredUniqueChars = 1;

                // Lockout settings.
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                // User settings.
                options.User.AllowedUserNameCharacters =
                "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                options.User.RequireUniqueEmail = false;
            });

            services.ConfigureApplicationCookie(options =>
            {
                // Cookie settings
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(60);

                options.LoginPath = "/Identity/Account/Login";
                options.AccessDeniedPath = "/Identity/Account/AccessDenied";
                options.SlidingExpiration = true;
            });

            // proxy setting
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.KnownProxies.Add(IPAddress.Parse("10.0.0.100"));
            });
        }
        #endregion

        #region snippet_configure
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            // proxy setting
            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
        #endregion
    }
}
