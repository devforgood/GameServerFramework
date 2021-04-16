using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Server
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IHostApplicationLifetime _applicationLifetime;

        public Worker(ILogger<Worker> logger, IHostApplicationLifetime applicationLifetime)
        {
            _logger = logger;
            _applicationLifetime = applicationLifetime;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                while (!stoppingToken.IsCancellationRequested)
                {
                    Timing.sInstance.Update();
                    Server.sInstance.DoFrame();

                    //_logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    await Task.Delay(1, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                //Console.WriteLine(ex.ToString());
                //Console.ReadKey();
                Log.Fatal(ex, "Host terminated unexpectedly");
                Log.CloseAndFlush();
                _applicationLifetime.StopApplication();
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }
    }
}
