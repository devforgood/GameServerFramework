using GameService;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Newtonsoft.Json;
using Serilog;
using Server;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Lobby
{
    public class ChatService : GameChat.ChatService.ChatServiceBase
    {
        public ChatService()
        {


        }

        public override async Task CreateRoom(GameChat.CreateRoomRequest request, IServerStreamWriter<GameChat.ChatMessage> responseStream, ServerCallContext context)
        {
            var room_id = Guid.NewGuid().ToString();

            // 조건에 만족하는 유저가 없다면 대기 (redis puh로 활성화)
            var queue = Cache.Instance.GetSubscriber().Subscribe($"room:{room_id}");

            var cts = new CancellationTokenSource();
            //cts.CancelAfter((int)startplay_polling_period.TotalMilliseconds);
            try
            {
                var ret = await queue.ReadAsync(cts.Token);

                var reply = JsonParser.Default.Parse<GameChat.ChatMessage>(ret.Message);
                try
                {
                    await responseStream.WriteAsync(reply);
                }
                catch (InvalidOperationException)
                {
                    Log.Information($"StartPlay restore match user {ret.Message}");
                }
            }
            catch (OperationCanceledException)
            {
                // cts cancel
            }
        }

        public override async Task<global::GameChat.SendChatReply> SendChat(GameChat.SendChatRequest request, ServerCallContext context)
        {
            var msg = JsonConvert.SerializeObject(request);
            await Cache.Instance.GetSubscriber().PublishAsync($"room:{request.RoomId}", msg);

            return new GameChat.SendChatReply()
            {
                Code = GameChat.ErrorCode.Success
            };
        }

    }
}