using GameService;
using Google.Protobuf;
using Grpc.Core;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lobby
{
    /// <summary>
    /// 정렬된 유저의 레이팅 기반 서칭 및 매칭
    /// </summary>
    public class SequentialMatchmaking
    {
        private static TimeSpan match_expire = new TimeSpan(0, 5, 0);
        private static TimeSpan match_user_expire = new TimeSpan(0, 5, 0);
        private static TimeSpan startplay_polling_period = new TimeSpan(0, 0, 30);
        private static TimeSpan restore_match_user_expire = new TimeSpan(0, 5, 0);
        private static TimeSpan game_result_expire = new TimeSpan(0, 10, 0);

        private static int MAX_START_PLAYER_COUNT = 6;


        public static async Task RemoveMatchUser(long user_no, int MapId)
        {
            var db = Cache.Instance.GetDatabase();
            await db.SortedSetRemoveAsync($"waiting_list:{MapId}", user_no);
            // match_user 는 삭제 하지 않는다. 만약 삭제하게되면 
            // waiting_list를 얻은 상태에서 match_user를 선점하게되어 이미 게임 시작 중인 유저가 다시 매칭될수 있다.

        }

        /// <summary>
        /// 이미 다른 유저가 매칭 시도를 했을 경우 
        /// </summary>
        /// <param name="user_no"></param>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        public static async Task<bool> RestoreMatchUser(long user_no, IServerStreamWriter<StartPlayReply> responseStream)
        {
            var db = Cache.Instance.GetDatabase();
            var value = await db.StringGetAsync($"match_user:{user_no}");
            if (value.HasValue)
            {
                // 매칭은 성공했으나 응답을 받지 못한 경우 백업 된 정보를 얻어온다.
                var ret = await db.StringGetAsync($"restore_match_user:{user_no}");
                if (ret.HasValue)
                {
                    StartPlayReply reply = JsonParser.Default.Parse<StartPlayReply>(ret);

                    await SequentialMatchmaking.RemoveMatchUser(user_no, reply.MapId);

                    await responseStream.WriteAsync(reply);

                    // 전송 성공시 다음에 다시 사용되지 않도록 키값 삭제
                    await db.KeyDeleteAsync($"restore_match_user:{user_no}");
                    return true;
                }
                else
                {
                    Log.Information($"cannot find restore_match_user:{user_no}");
                }
            }
            else
            {
                Log.Information($"cannot find match_user:{user_no}");
            }

            return false;
        }

        /// <summary>
        /// 매칭 대상 (플레이어) 찾기
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public static async Task<(List<long>, Dictionary<string, ServerCommon.PlayerInfo>, List<StartPlayCharacterInfo>, bool)> SearchPlayer(Session session, StartPlayRequest request)
        {
            var db = Cache.Instance.GetDatabase();
            Lobby.Session player_session = null;
            var players = new Dictionary<string, ServerCommon.PlayerInfo>();
            var character_list = new List<StartPlayCharacterInfo>();
            long match_id = 0;
            var player_list = new List<long>();
            var waiting_list = await db.SortedSetRangeByScoreAsync($"waiting_list:{request.MapId}");
            Log.Information($"waiting user ({string.Join(",", waiting_list)})");
            bool result = false;
            if (waiting_list.Length >= MAX_START_PLAYER_COUNT - 1)
            {
                match_id = await db.StringIncrementAsync("match_instance_id");
                for (int i = 0; i < waiting_list.Length; ++i)
                {
                    long user_no = (long)waiting_list[i];
                    Log.Information($"searching... waiting user {user_no}");
                    // 자신은 스킵
                    if (user_no == session.user_no)
                        continue;

                    if ((await db.StringGetAsync($"waiting:{user_no}")).HasValue)
                    {
                        // 매칭에 필요한 유저를 선점한다
                        if (await db.StringSetAsync($"match_user:{user_no}", match_id, match_user_expire, When.NotExists) == true)
                        {
                            player_session = await Session.GetSession(user_no, false);
                            if (player_session == null)
                            {
                                Log.Information("cannot find Session {0}", user_no);
                                await db.KeyDeleteAsync($"match_user:{user_no}");
                                continue;
                            }

                            ServerCommon.PlayerInfo player = new ServerCommon.PlayerInfo()
                            {
                                user_no = player_session.user_no,
                                character_type = player_session.character_type,
                                user_id = player_session.user_name,
                                team = (byte)(players.Count % (int)core.BaseStruggleTeam.TeamCount),
                            };
                            players.Add(player_session.session_id, player);
                            character_list.Add(new StartPlayCharacterInfo() { SelectedCharacter = player_session.character_type, UserId = player_session.user_name, Team = player.team });

                            player_list.Add(user_no);
                            Log.Information($"Candidate User {user_no}");

                            if (player_list.Count == MAX_START_PLAYER_COUNT - 1)
                            {
                                result = true;
                                break;
                            }
                        }
                        else
                        {
                            Log.Information("already other match assign user {0}", user_no);
                        }
                    }
                    else
                    {
                        Log.Information("wait timeout user {0}", user_no);

                        // 유효하지 않는 유저는 대기자 목록에서 삭제한다.
                        await SequentialMatchmaking.RemoveMatchUser(user_no, request.MapId);
                    }
                }
            }

            Log.Information($"StartPlay player_list ({string.Join(",", player_list)})");
            return (player_list, players, character_list, result);
        }

        /// <summary>
        /// 배틀서버에 알림
        /// </summary>
        /// <param name="players"></param>
        /// <param name="worldId"></param>
        /// <param name="channel_id"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public static async Task PubStartPlay(Dictionary<string, ServerCommon.PlayerInfo> players, byte worldId, string channel_id, Session session)
        {
            // 전체 참여자 목록 구성
            var pubMessage = JsonConvert.SerializeObject(new ServerCommon.InternalMessage() { message_type = (byte)ServerCommon.InternalMessageType.Participant, world_id = worldId, channel_id = channel_id, players = players });
            Log.Information($"PubStartPlay {pubMessage}");

            // 배틀 체널에 참여자 명단을 알림
            await Cache.Instance.GetSubscriber().PublishAsync($"channel_msg:{channel_id}", pubMessage);
        }

        /// <summary>
        /// 게임 플레이 시작
        /// </summary>
        /// <param name="player_list"></param>
        /// <param name="players"></param>
        /// <param name="character_list"></param>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        public static async Task<bool> StartPlay(List<long> player_list, Dictionary<string, ServerCommon.PlayerInfo> players, List<StartPlayCharacterInfo> character_list, Session session, StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream)
        {
            // 매칭에 필요한 인원을 모두 찾았을때
            // 전투 가능한 서버를 찾아 세팅
            (bool ret, string server_addr, byte worldId, string channel_key, string channel_id) = await Channel.GetAvailableServer(request.MapId);
            if (ret == false)
            {
                // 전투 가능한 서버가 없다
                Log.Error($"Cannot find Server user_no:{session.user_no}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.BusyServer });
                return false;
            }

            ServerCommon.PlayerInfo player = new ServerCommon.PlayerInfo() 
            { 
                user_no = session.user_no, 
                character_type = session.character_type, 
                user_id = session.user_name, 
                team = (byte)(players.Count % (int)core.BaseStruggleTeam.TeamCount) 
            };
            players.Add(session.session_id, player);

            character_list.Add(new StartPlayCharacterInfo() { SelectedCharacter = session.character_type, UserId = session.user_name, Team = player.team });
            var reply = new StartPlayReply()
            {
                Code = ErrorCode.Success,
                IsStart = true,
                BattleServerAddr = server_addr,
                WorldId = worldId,
                MapId = request.MapId,
            };
            character_list.ForEach(x => reply.CharacterList.Add(x));


            string reply_str = new JsonFormatter(new JsonFormatter.Settings(true)).Format(reply);
            Log.Information($"StartPlay Reply {reply_str}");

            // 매칭된 유저들에게 알림
            for (int i = 0; i < player_list.Count; ++i)
            {
                await Cache.Instance.GetSubscriber().PublishAsync($"sub_user:{player_list[i]}", reply_str);
            }

            await SequentialMatchmaking.RemoveMatchUser(session.user_no, request.MapId);

            // 배틀서버에 알림
            await PubStartPlay(players, worldId, channel_id, session);

            Log.Information($"StartPlay {session.user_no}, channel_msg:{channel_key}");

            await responseStream.WriteAsync(reply);

            // 매칭이 성공적으로 이루어졌으므로 이후 매칭 결과를 pub/sub으로 전달 받는다.

#pragma warning disable 4014
            Task.Run(async () => await WaitGameResult(channel_id) );
#pragma warning restore 4014

            return true;
        }

        /// <summary>
        /// 매칭 시도를 실패하면 선점한 플레이어를 삭제, 다른 매칭에 검색 되도록
        /// </summary>
        /// <param name="player_list"></param>
        /// <returns></returns>
        public static async Task ClearReservedPlayer(List<long> player_list)
        {
            var db = Cache.Instance.GetDatabase();
            await db.KeyDeleteAsync(player_list.Select(key => (RedisKey)$"match_user:{key}").ToArray());
        }

        /// <summary>
        /// 매칭 조건이 맞지 않아 대기
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        public static async Task WaitStartPlay(Session session, StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream)
        {
            var db = Cache.Instance.GetDatabase();

            // 대기자로 등록
            await db.KeyDeleteAsync($"match_user:{session.user_no}"); // 매칭 선점 클리어
            await db.SortedSetAddAsync($"waiting_list:{request.MapId}", session.user_no, session.rating);
            await db.StringSetAsync($"waiting:{session.user_no}", 0, startplay_polling_period);


            // 조건에 만족하는 유저가 없다면 대기 (redis puh로 활성화)
            var queue = Cache.Instance.GetSubscriber().Subscribe($"sub_user:{session.user_no}");

            var cts = new CancellationTokenSource();
            cts.CancelAfter((int)startplay_polling_period.TotalMilliseconds);
            try
            {
                var ret = await queue.ReadAsync(cts.Token);
                StartPlayReply reply = JsonParser.Default.Parse<StartPlayReply>(ret.Message);
                try
                {
                    // 매칭이 성공되었음을 알림
                    await responseStream.WriteAsync(reply);

                    // 다른 유저로 부터 매칭이 되었음을 받았다
                    // 매칭 정보를 삭제
                    await SequentialMatchmaking.RemoveMatchUser(session.user_no, request.MapId);
                }
                catch (InvalidOperationException)
                {
                    Log.Information($"StartPlay restore match user {ret.Message}");
                    // 전송 실패시 매칭 데이터를 백업
                    await db.StringSetAsync($"restore_match_user:{session.user_no}", ret.Message, restore_match_user_expire);
                }
            }
            catch (OperationCanceledException)
            {
                await SequentialMatchmaking.RemoveMatchUser(session.user_no, request.MapId);

                // 대기시간 만료 클라이언트에게 타임 아웃 처리를 보낸다.
                await responseStream.WriteAsync(new StartPlayReply()
                {
                    Code = ErrorCode.Timeout,
                    IsStart = false
                });

            }
        }

        public static async Task CancelStartPlay(Session session, IServerStreamWriter<CancelStartPlayReply> responseStream)
        {
            var db = Cache.Instance.GetDatabase();

            // 다른 유저가 매칭 시도를 했을 경우 
            var value = await db.StringGetAsync($"match_user:{session.user_no}");
            if (value.HasValue)
            {
                // 이미 매칭이 완료되었기 때문에 매칭 취소를 진행할 수 없다.
                await responseStream.WriteAsync(new CancelStartPlayReply() { Code = ErrorCode.AlreadyMatch });
                return;
            }

            // 매칭 대기열에서 제거
            await SequentialMatchmaking.RemoveMatchUser(session.user_no, session.map_id);

            await responseStream.WriteAsync(new CancelStartPlayReply() { Code = ErrorCode.Success });
        }


        /// <summary>
        /// 매칭 조건이 맞지 않아 대기
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        public static async Task WaitGameResult(string channel_id)
        {
            string key = $"game_result:{channel_id}";
            Log.Information($"WaitGameResult {key}");
            var queue = Cache.Instance.GetSubscriber().Subscribe(key);

            var cts = new CancellationTokenSource();
            cts.CancelAfter((int)game_result_expire.TotalMilliseconds);
            try
            {
                var ret = await queue.ReadAsync(cts.Token);
                ServerCommon.GameResult reply = JsonConvert.DeserializeObject<ServerCommon.GameResult>(ret.Message);

                Log.Information($"game result {ret.Message.ToString()}");

            }
            catch (OperationCanceledException ex)
            {
                Log.Information($"game result timeout {ex.ToString()}");
            }
        }


        public static async Task StartPlay(StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream, ServerCallContext context)
        {
            Log.Information($"StartPlay mapId:{request.MapId}, SelectedCharacter:{request.SelectedCharacter}, IsImmediatelyJoin{request.IsImmediatelyJoin}");

            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.LostSession });
                return;
            }

            // 게임 시작 요청 정보를 캐싱
            await session.UpdateSessionLock(request.SelectedCharacter, request.MapId, true);

            if (request.IsImmediatelyJoin)
            {
                (bool ret, string server_addr, byte worldId, string channel_key, string channel_id) = await Channel.GetAvailableServer(request.MapId);
                if (ret == false)
                {
                    // 전투 가능한 서버가 없다
                    Log.Error($"Cannot find Server user_no:{session.user_no}");
                    await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.BusyServer });
                    return;
                }

                var tmp_players = new Dictionary<string, ServerCommon.PlayerInfo>();
                ServerCommon.PlayerInfo player = new ServerCommon.PlayerInfo()
                {
                    user_no = session.user_no,
                    character_type = session.character_type,
                    user_id = session.user_name,
                    team = (byte)core.MathHelpers.GetRandomInt((int)core.BaseStruggleTeam.TeamCount),
                };
                tmp_players.Add(session.session_id, player);

                var characters = new List<StartPlayCharacterInfo>();
                characters.Add(new StartPlayCharacterInfo() { SelectedCharacter = session.character_type, UserId = session.user_name, Team = player.team });
                var reply = new StartPlayReply()
                {
                    Code = ErrorCode.Success,
                    IsStart = true,
                    BattleServerAddr = server_addr,
                    WorldId = worldId,
                    MapId = request.MapId,
                };
                characters.ForEach(x => reply.CharacterList.Add(x));

                await responseStream.WriteAsync(reply);


                await SequentialMatchmaking.PubStartPlay(tmp_players, worldId, channel_id, session);


#pragma warning disable 4014
                Task.Run(async () => await SequentialMatchmaking.WaitGameResult(channel_id));
#pragma warning restore 4014

                return;

            }

            // 다른 플레이어로 인해 매칭이 시작 되었는지 확인
            if ((await SequentialMatchmaking.RestoreMatchUser(session.user_no, responseStream)) == true)
            {
                return;
            }

            // 대기중이 플레이어 찾기
            (var player_list, var players, var character_list, var search_success) = await SequentialMatchmaking.SearchPlayer(session, request);
            if (search_success == true)
            {
                // 게임 시작
                if ((await SequentialMatchmaking.StartPlay(player_list, players, character_list, session, request, responseStream)) == false)
                {
                    // 예약했던 플레이어 취소
                    await SequentialMatchmaking.ClearReservedPlayer(player_list);
                }
                return;
            }

            // 예약했던 플레이어 취소
            await SequentialMatchmaking.ClearReservedPlayer(player_list);

            Log.Information("StartPlay waiting... {0}", session.user_no);

            // 대기
            await SequentialMatchmaking.WaitStartPlay(session, request, responseStream);
        }

    }
}
