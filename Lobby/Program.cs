using Grpc.Core;
using Serilog;
using Serilog.Events;
using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Grpc.Host;
using System.Collections.Generic;

namespace Lobby
{
    class Program
    {

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args);
            // Add this line to be able to run as a windows service
            // https://docs.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-3.1&tabs=visual-studio
            //host.UseWindowsService();
            host.UseSystemd();
            // This will add the configuration to the application.
            // It will allow you to inject the configuration as
            // IConfiguration configuration in your constructors
            host.ConfigureAppConfiguration(
                  (hostContext, config) =>
                  {
                      config.SetBasePath(Directory.GetCurrentDirectory());
                      config.AddJsonFile(ServerConfiguration.Instance.appsettingsFilename, false, true);
                      config.AddCommandLine(args);
                  }
            );
            // This will configure logging. It reads the log settings from
            // the appsettings.json configuration file, and adds serilog,
            // allowing the application to write logs to file
            host.ConfigureLogging(
                loggingBuilder =>
                {
                    var configuration = new ConfigurationBuilder()
                        .AddJsonFile(ServerConfiguration.Instance.appsettingsFilename)
                        .Build();

                    Log.Logger = new LoggerConfiguration()
                        .Enrich.WithThreadId()
                        .ReadFrom.Configuration(configuration)
                        .CreateLogger();

                    loggingBuilder.AddSerilog(Log.Logger, dispose: true);

                    Log.Information("server version : 1.0.10");
                }
            );
            // The AddHostedServer adds the worker service to the application.
            // The AddApplicationInsightsTelemetryWorkerService is very important. Without this,
            // the Application Insights logging will not work.
            host.ConfigureServices((hostContext, services) =>
            {
                var serverCredentials = ServerCredentials.Insecure;
                var EnableTLS = ServerConfiguration.Instance.config.GetSection("EnableTLS");
                if (EnableTLS.Value != null && bool.Parse(EnableTLS.Value) == true)
                {
                    var rootCert = File.ReadAllText("Credentials/ca.pem");
                    var keyCertPair = new KeyCertificatePair(
                            File.ReadAllText("Credentials/server.pem"),
                            File.ReadAllText("Credentials/server.key"));

                    serverCredentials = new SslServerCredentials(new[] { keyCertPair }, null, SslClientCertificateRequestType.DontRequest);
                }

                // Better to use Dependency Injection for GreeterImpl
                Server server = new Server
                {
                    Services = { GameService.Lobby.BindService(new LobbyService()) },
                    Ports = { new ServerPort("[::]", Convert.ToUInt16(ServerConfiguration.Instance.config["port"]), serverCredentials) }
                };

                services.AddSingleton<Server>(server);
                services.AddSingleton<IHostedService, GrpcHostedService>();
                //services.AddHostedService<GrpcHostedService>();
                services.AddApplicationInsightsTelemetryWorkerService();
            });

            return host;
        }

        static void Main(string[] args)
        {
            try
            {
                if (args.Length > 0)
                {
                    ServerConfiguration.Instance.appsettingsFilename = args[0];
                    ServerConfiguration.Instance.Init(ServerConfiguration.Instance.appsettingsFilename);
                    if (args.Length > 1)
                    {
                        //Console.WriteLine($"SetCurrentDirectory {args[1]}");
                        Directory.SetCurrentDirectory(args[1]);
                    }
                }

                if(ServerConfiguration.Instance.GameContext.Length != ServerConfiguration.Instance.LogContext.Length)
                {
                    Log.Error($"GameContext.Length({ServerConfiguration.Instance.GameContext.Length}) != LogContext.Length({ServerConfiguration.Instance.LogContext.Length})");
                    return;
                }
                Shard.MaxSharding = ServerConfiguration.Instance.GameContext.Length;

                {
                    Dictionary<string, string> Scripts = new Dictionary<string, string>();
                    JsonData.Instance.LoadOriginalData(Scripts);
                    JsonData.Instance.LoadData(true, true);
                    DebugCommandData.LoadData();

                    //Cache.Instance.GetDatabase().KeyDelete("scripts");
                    foreach (var script in Scripts)
                    {
                        Cache.Instance.GetDatabase().HashSet($"scripts", script.Key, script.Value);
                    }
                }

                if (Convert.ToBoolean(ServerConfiguration.Instance.config["server_process_update"]))
                {
                    core.ServerProcessManager.sInstance.Init(ServerConfiguration.Instance.config["ip"] + ":" + ServerConfiguration.Instance.config["port"]
                        , ServerConfiguration.Instance.config["redis:ip"] + ":" + ServerConfiguration.Instance.config["redis:port"]);
                }

                UnitTest.Test();
                //UnitTest.QueryListUp();

                Subscribe.RegisterSubscribe("lobby");

#if GRPC_DEBUG
                Environment.SetEnvironmentVariable("GRPC_TRACE", "api,http,cares_resolver,cares_address_sorting,transport_security,tsi");
                Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
                Grpc.Core.GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());
#endif

                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                //Console.ReadKey();
                Log.Fatal(ex, "Host terminated unexpectedly");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
