using GameService;
using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ChatClient
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Environment.SetEnvironmentVariable("GRPC_TRACE", "api,http,cares_resolver,cares_address_sorting,transport_security,tsi");
            Environment.SetEnvironmentVariable("GRPC_VERBOSITY", "debug");
            Grpc.Core.GrpcEnvironment.SetLogger(new Grpc.Core.Logging.ConsoleLogger());



            var channel = new Channel("localhost:50051", ChannelCredentials.Insecure);
            var client = new GameChat.ChatService.ChatServiceClient(channel);



            int cnt = 0;
            using (var call = client.CreateRoom(new GameChat.CreateRoomRequest { RoomName = "testroom", SenderName = "testuser" }))
            {
                var responseStream = call.ResponseStream;
                while (await responseStream.MoveNext())
                {
                    await client.SendChatAsync(new GameChat.SendChatRequest() { RoomId = responseStream.Current.RoomId, Message = $"test {cnt++}" });
                }
            }

            await channel.ShutdownAsync();
        }
    }
}
