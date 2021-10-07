using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace Grpc.Host
{
    public class GrpcHostedService : IHostedService
    {
        private Grpc.Core.Server _server;

        public GrpcHostedService(Grpc.Core.Server server)
        {
            _server = server;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _server.Start();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            //Log.Information($"Server Stopping {Lobby.RankMatchmaking.MatchCount}");
            //while (Lobby.RankMatchmaking.MatchCount != 0)
            //{
            //    Log.Error($"Server Stopping... please wait {Lobby.RankMatchmaking.MatchCount}");
            //    await Task.Delay(1000);
            //}

            //Log.Information($"Server Stop Complete! {Lobby.RankMatchmaking.MatchCount}");

            await _server.ShutdownAsync();
        }
    }
}