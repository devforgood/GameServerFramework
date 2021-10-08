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

            var reply1 = new GameChat.ChatMessage()
            {
                Code = GameChat.ErrorCode.Success,
                RoomId = room_id,
                Message = "welcome",
            };
            
            await responseStream.WriteAsync(reply1);


            // 조건에 만족하는 유저가 없다면 대기 (redis puh로 활성화)
            var queue = Cache.Instance.GetSubscriber().Subscribe($"room:{room_id}");


            while (true)
            {
                try
                {
                    var ret = await queue.ReadAsync(context.CancellationToken);

                    var reply = JsonConvert.DeserializeObject<GameChat.ChatMessage>(ret.Message);
                    try
                    {

                        await responseStream.WriteAsync(reply);
                        Log.Information($"send {ret.Message}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Log.Error($"send error {ret.Message} {ex.ToString()}");
                        break;
                    }
                }
                catch (OperationCanceledException ex)
                {
                    // cts cancel
                    Log.Error($"cts error {ex.ToString()}");
                    break;
                }
            }
        }

        public override async Task<global::GameChat.SendChatReply> SendChat(GameChat.SendChatRequest request, ServerCallContext context)
        {
            GameChat.ChatMessage chat_msg = new GameChat.ChatMessage()
            {
                RoomId = request.RoomId,
                Message = request.Message,
            };

            var msg = JsonConvert.SerializeObject(chat_msg);
            await Cache.Instance.GetSubscriber().PublishAsync($"room:{request.RoomId}", msg);

            return new GameChat.SendChatReply()
            {
                Code = GameChat.ErrorCode.Success
            };
        }

    }
}