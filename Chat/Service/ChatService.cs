using core;
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


        private async Task WaitRoomAsync(string roomId, IServerStreamWriter<GameChat.ChatMessage> responseStream, ServerCallContext context)
        {
            await Cache.Instance.GetDatabase().StringIncrementAsync($"room_user_cnt:{roomId}");

            // 조건에 만족하는 유저가 없다면 대기 (redis puh로 활성화)
            var queue = Cache.Instance.GetSubscriber().Subscribe($"room:{roomId}");


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

            await Cache.Instance.GetDatabase().StringDecrementAsync($"room_user_cnt:{roomId}");

        }

        private async Task SendChatMessageAsync(string roomId, string Message)
        {
            GameChat.ChatMessage chat_msg = new GameChat.ChatMessage()
            {
                RoomId = roomId,
                Message = Message,
            };

            var msg = JsonConvert.SerializeObject(chat_msg);
            await Cache.Instance.GetSubscriber().PublishAsync($"room:{roomId}", msg);
        }

        public static async Task<List<GameChat.ChatRoom>> GetChatRoomList(long skip, long take)
        {
            var room_list = await Cache.Instance.GetDatabase().SortedSetRangeByScoreAsync($"room_z", double.NegativeInfinity, double.PositiveInfinity, StackExchange.Redis.Exclude.None
                , StackExchange.Redis.Order.Descending, skip, take);


            var result = new List<GameChat.ChatRoom>();
            foreach (var roomId in room_list)
            {
                result.Add(await _GetChatRoom(roomId));
            }

            return result;
        }
        private static async Task<GameChat.ChatRoom> _GetChatRoom(string roomId)
        {
            var r = new GameChat.ChatRoom();
            r.RoomId = roomId;
            r.CreateTime = (int)await Cache.Instance.GetDatabase().SortedSetScoreAsync($"room_z", roomId);
            //r.CreateTime = (int)await Cache.Instance.GetDatabase().SortedSetRankAsync($"room_z", roomId, StackExchange.Redis.Order.Descending);
            r.RoomName = await Cache.Instance.GetDatabase().StringGetAsync($"room_info:{roomId}");
            return r;
        }

        public override async Task CreateRoom(GameChat.CreateRoomRequest request, IServerStreamWriter<GameChat.ChatMessage> responseStream, ServerCallContext context)
        {
            var roomId = Guid.NewGuid().ToString();

            var reply1 = new GameChat.ChatMessage()
            {
                Code = GameChat.ErrorCode.Success,
                RoomId = roomId,
                Message = "welcome",
            };

            await Cache.Instance.GetDatabase().SortedSetAddAsync($"room_z", roomId, DateTime.UtcNow.ToEpochTime());
            await Cache.Instance.GetDatabase().StringSetAsync($"room_info:{roomId}", request.RoomName);


            await responseStream.WriteAsync(reply1);

  
            await WaitRoomAsync(roomId, responseStream, context);

        }

        public override async Task JoinRoom(GameChat.JoinRoomRequest request, IServerStreamWriter<GameChat.ChatMessage> responseStream, ServerCallContext context)
        {
            var reply1 = new GameChat.ChatMessage()
            {
                Code = GameChat.ErrorCode.Success,
                RoomId = request.RoomId,
                Message = "welcome join",
            };

            await responseStream.WriteAsync(reply1);

            await WaitRoomAsync(request.RoomId, responseStream, context);

        }

        public override async Task<global::GameChat.SendChatReply> SendChat(GameChat.SendChatRequest request, ServerCallContext context)
        {
            await SendChatMessageAsync(request.RoomId, request.Message);

            return new GameChat.SendChatReply()
            {
                Code = GameChat.ErrorCode.Success
            };
        }


        public override async Task<global::GameChat.GetChatRoomsReply> GetChatRooms(GameChat.GetChatRoomsRequest request, ServerCallContext context)
        {
            var reply = new GameChat.GetChatRoomsReply();

            var ret = await GetChatRoomList(0, 100);
            reply.Rooms.Add(ret);

            return reply;
        }
    }
}