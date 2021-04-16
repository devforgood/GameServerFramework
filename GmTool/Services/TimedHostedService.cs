using System;
using System.Threading;
using System.Threading.Tasks;
using GmTool.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BackgroundTasks.Services
{
    public class TimedHostedService : IHostedService, IDisposable
    {
        private int executionCount = 0;
        private readonly ILogger<TimedHostedService> _logger;
        private Timer _timer;

        public TimedHostedService(ILogger<TimedHostedService> logger)
        {
            _logger = logger;
        }

        private void Schedule(double hours = 0.0)
        {
            DateTime now = DateTime.UtcNow;

            DateTime fourOClock = DateTime.UtcNow.Date.AddHours(hours).AddMinutes(10.0);
            if (now > fourOClock)
            {
                fourOClock = fourOClock.AddDays(1.0);

                //var ret = Task.Run(GmTool.Ranking.ProcessRankingReward);
                //ret.Wait();

                GmTool.Modules.LeaderBoard.LeaderBoardScheduler.Run().ConfigureAwait(false);
            }

            int msUntilFour = (int)((fourOClock - now).TotalMilliseconds);

            // Set the timer to elapse only once.
            _timer?.Change(msUntilFour, Timeout.Infinite);
        }

        public Task StartAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service running.");

            _timer = new Timer(DoWork);
            Schedule();

            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            var count = Interlocked.Increment(ref executionCount);

            _logger.LogInformation(
                "Timed Hosted Service is working. Count: {Count}", count);

            Schedule();
        }

        public Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Timed Hosted Service is stopping.");

            _timer?.Change(Timeout.Infinite, 0);

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
