using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using core;
using System.Net.Http;
using System.Net;

namespace Server
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
                services.AddHostedService<Worker>();
                services.AddApplicationInsightsTelemetryWorkerService();
            });

            return host;
        }

        static bool ReadyServer()
        {
            ServerConfiguration.Instance.WriteLog();

            if (Server.StaticInit(Convert.ToUInt16(ServerConfiguration.Instance.config["port"]),
                Convert.ToByte(ServerConfiguration.Instance.config["world_count"]),
                Convert.ToInt32(ServerConfiguration.Instance.config["map_id"]),
                Convert.ToBoolean(ServerConfiguration.Instance.config["battle_auth"])) == false)
            {
                Log.Error("Server.StaticInit error");
                return false;
            }

            string dns = "";
            if(ServerConfiguration.Instance.config["ip"]!=null)
            {
                dns = ServerConfiguration.Instance.config["ip"];
            }
            else if(ServerConfiguration.Instance.config["meta_hostname"] != null)
            {
                try
                {
                    HttpClient client = new HttpClient();

                    HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Get, ServerConfiguration.Instance.config["meta_hostname"]);


                    HttpResponseMessage response = client.SendAsync(msg).Result;

                    // Get the response
                    var responseString =  response.Content.ReadAsStringAsync().Result;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Log.Error($"getInfo error :{responseString}");
                        return false;
                    }
                    dns = responseString;
                }
                catch (HttpRequestException e)
                {
                    Log.Error($"HttpRequestException :{e.Message}");
                    return false;
                }
                catch (Exception e)
                {
                    Log.Error($"Exception :{e.Message}");
                    return false;
                }
 
            }
            else
            {
                Log.Error("server address error");
                return false;
            }


            var server_addr = dns + ":" + ServerConfiguration.Instance.config["port"];
            var redis_addr = ServerConfiguration.Instance.config["redis:ip"] + ":" + ServerConfiguration.Instance.config["redis:port"];

            var DefaultFirstReplicationTimeout = ServerConfiguration.Instance.config.GetSection("DefaultFirstReplicationTimeout");
            if (DefaultFirstReplicationTimeout.Value != null)
            {
                ReplicationManagerServer.DefaultFirstReplicationTimeout = int.Parse(DefaultFirstReplicationTimeout.Value);
            }

            if (Convert.ToBoolean(ServerConfiguration.Instance.config["server_process_update"]))
            {
                ServerProcessManager.sInstance.Init(server_addr, redis_addr);
            }

            if (ServerConfiguration.Instance.channel_update == true)
            {
                Cache.sInstance.Init(Convert.ToByte(ServerConfiguration.Instance.config["world_count"]), server_addr,
                    redis_addr, ServerConfiguration.Instance.config["name"], Convert.ToInt32(ServerConfiguration.Instance.config["map_id"]));
                Subscribe.Do((Lidgren.Network.NetServer)NetworkManagerServer.sInstance.GetServer());
                CacheThread.Run();
            }

            return true;
        }

        static int Main(string[] args)
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


            
            var host = CreateHostBuilder(args).Build();

            if(ReadyServer() == false)
            {
                return 1;
            }


            host.Run();
            return 0;
        }
    }
}
