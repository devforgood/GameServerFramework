using GameService;
using Grpc.Core;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lobby
{
    public class MinimumStartPlay
    {
        public bool Enable;
        public int PlayerCount;
        public int Timeout;
    }

    /// <summary>
    /// 랭크를 기준으로 매칭 상대를 조회 및 매칭 처리
    /// </summary>
    public class RankMatchmaking
    {
        private static TimeSpan match_waiting_expire = new TimeSpan(0, 5, 0);

        readonly static int defaultGetWaitingUser = 100; // 다른 유저가 선점 중이거나, 만료된 유저일 경우를 대비하여 여유있게 획득한다.
        public static int StartPlayerCount = 0;
        public static Dictionary<int, MinimumStartPlay> minimumStartPlayMap = new Dictionary<int, MinimumStartPlay>();

        public static int MatchCount = 0;


        static int GetStartPlayerCount(JGameModeData game_mode)
        {
            if (StartPlayerCount != 0)
                return StartPlayerCount;

            return game_mode.PlayerCount;
        }

        public static async Task<(MatchResult, bool)> SearchPlayer(Session session, long match_id, JMapData map_data, JGameModeData game_mode)
        {
            bool result = false;
            var matchResult = new MatchResult(match_id, session.map_id);
            if (GetStartPlayerCount(game_mode) <= 1)
                return (matchResult, true);

            Session other_session = null;
            var seq = new RankSequencer() { rank = session.rank, min_rank = session.min_rank, max_rank = session.max_rank };
            foreach(var rank in seq)
            {
                var users = await WaitingList.GetWaitingUser(session.map_id, rank, defaultGetWaitingUser);
                for (int i = 0; i < users.Count; ++i)
                {
                    //Log.Information($"searching... waiting user : {users[i]}, rank : {rank}");

                    // 자신은 스킵
                    if (users[i] == session.user_no)
                        continue;

                    if ((other_session = await Session.GetSession(users[i], false)) == null // 대기중 유저 세션 만료
                        ) 
                    {
                        Log.Information("cannot find Session {0}", users[i]);
                        await WaitingList.RemoveWaitingUser(users[i]);
                        continue;
                    }

                    if(other_session.rank != rank)
                        continue;

                    if (await MatchUser.OccupyMatchUser(users[i], match_id) == false)
                        continue;

                    if (await matchResult.AddPlayer(other_session, map_data, game_mode) == false)
                    {
                        Log.Error($"SearchPlayer error user_no:{session.user_no}");
                        continue;
                    }

                    Log.Information($"Candidate User {other_session.user_no}");

                    if (matchResult.replyToBattleServer.players.Count == GetStartPlayerCount(game_mode) - 1)
                    {
                        result = true;
                        return (matchResult, result);
                    }
                }
            }

            return (matchResult, result);
        }

        public static async Task<bool> RestoreMatchUser(Session session, IServerStreamWriter<StartPlayReply> responseStream)
        {
            var (match_success, reply, match_id) = await RestoreMatchUser(session);
            if (match_success == false)
            {
                return false;
            }
            History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.StartPlay, (byte)HistoryLogReason.None, reply.CharacterList.Count, reply.MapId, match_id.ToString(), session.character_type.ToString());
            _ = LogProxy.writeActionLog(session, "플레이", "매칭성공", reply.MapId.ToString()).ConfigureAwait(false);

            await responseStream.WriteAsync(reply);

            return true;
        }

        public static async Task<(bool, StartPlayReply, long)> RestoreMatchUser(Session session)
        {
            Log.Information($"RestoreMatchUser  session:{session.session_id}, user_no:{session.user_no}");
            (var success, var match_id, var timeToLive) = await MatchUser.GetMatchUser(session.user_no);
            if (success == false)
            {
                Log.Information($"RestoreMatchUser cannot find session:{session.session_id}, user_no:{session.user_no}");
                return (false, null, 0);
            }

            // 잠시 동안 유저는 할당했지만 아직 매칭 정보가 새팅중일수 있다.
            (var match_success, var reply) = await Match.LoadMatch(match_id);
            if (match_success == false)
            {
                Log.Information($"RestoreMatchUser LoadMatch false session:{session.session_id}, user_no:{session.user_no}");

                if (timeToLive != null)
                {
                    // 일정 시간 이후에도 매칭 정보가 없다면 해당 유저 할당을 강제 해지하여 매칭이 되도록 한다.
                    if ((match_waiting_expire - (TimeSpan)timeToLive).TotalSeconds > 3)
                    {
                        await MatchUser.RemoveMatchUser(session.user_no);
                    }
                }
                return (false, null, 0);
            }

            return (true, reply, match_id);
        }

        /// <summary>
        /// 매칭이 완료되었음을 배틀 서버로 전달
        /// </summary>
        /// <param name="players"></param>
        /// <param name="worldId"></param>
        /// <param name="channel_id"></param>
        /// <param name="session"></param>
        /// <returns></returns>
        public static async Task PubStartPlay(ServerCommon.InternalMessage msg)
        {
            // 전체 참여자 목록 구성
            var pubMessage = JsonConvert.SerializeObject(msg);
            Log.Information($"PubStartPlay {pubMessage}");

            // 배틀 체널에 참여자 명단을 알림
            await Cache.Instance.GetSubscriber().PublishAsync($"channel_msg:{msg.channel_id}", pubMessage);
        }

        public static async Task<bool> StartPlay(long match_id, Session session, StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream, MatchResult matchResult, JMapData map_data, JGameModeData game_mode)
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

            if(await matchResult.AddPlayer(session, map_data, game_mode) == false)
            {
                Log.Error($"StartPlay error user_no:{session.user_no}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.NotEnough });
                return false;
            }

            await matchResult.Finish(worldId, server_addr, channel_id, game_mode);

            // 매칭된 유저들에게 알림
            await Match.SaveMatch(match_id, matchResult.replyToClient);

            // 대기 목록에서 삭제
            foreach(var p in matchResult.replyToBattleServer.players.Values)
                await WaitingList.RemoveWaitingUser(p.user_no);


            // 배틀서버에 알림
            await PubStartPlay(matchResult.replyToBattleServer);

            Log.Information($"StartPlay {session.user_no}, channel_msg:{channel_key}");

            // 게임 시작 요청에 대한 응답 전송
            await responseStream.WriteAsync(matchResult.replyToClient);

            // 매칭이 성공적으로 이루어졌으므로 이후 매칭 결과를 pub/sub으로 전달 받는다.


            _ = GameResult.WaitGameResult(session, channel_id, map_data, game_mode).ConfigureAwait(false);

            History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.StartPlay, (byte)HistoryLogReason.None, matchResult.replyToBattleServer.players.Count, matchResult.replyToClient.MapId, match_id.ToString(), session.character_type.ToString());
            _ = LogProxy.writeActionLog(session, "플레이", "매칭성공", matchResult.replyToClient.MapId.ToString()).ConfigureAwait(false);


            var characterNames = new List<string>();
            var characterLevels = new List<int>();
            foreach(var player in matchResult.replyToBattleServer.players)
            {
                characterNames.Add(player.Value.user_id);
                characterLevels.Add(player.Value.character_level);
            }

            _ = LogProxy.writeRoundLog(session, game_mode.Name, "", "", "10", 0, 0, 0, characterNames, characterLevels, "").ConfigureAwait(false);

            return true;
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
            // 대기자로 등록
            await MatchUser.RemoveMatchUser(session.user_no);
            await WaitingList.AddWaitingUser(new WaitingUser() {map_id = session.map_id, rank = session.rank }, session.user_no);
        }

        public static async Task StartPlaySimulate(StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream, ServerCallContext context, Session session, JMapData map_data, JGameModeData game_mode)
        {

            (bool ret, string server_addr, byte worldId, string channel_key, string channel_id) = await Channel.GetAvailableServer(request.MapId);
            if (ret == false)
            {
                // 전투 가능한 서버가 없다
                Log.Error($"Cannot find Server user_no:{session.user_no}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.BusyServer });
                return;
            }

            long match_id = await MatchInstanceId.GetMatchInstanceId();


            var matchResult = new MatchResult(match_id, request.MapId);
            if(await matchResult.AddPlayer(session, map_data, game_mode)==false)
            {
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.NotEnough });
                return;
            }

            if (ServerConfiguration.Instance.gameSetting.EnableAIMatch)
                matchResult.AddAI(session, map_data, game_mode);
            await matchResult.Finish(worldId, server_addr, channel_id, game_mode);


            await responseStream.WriteAsync(matchResult.replyToClient);
            await PubStartPlay(matchResult.replyToBattleServer);


            _ = GameResult.WaitGameResult(session, channel_id, map_data, game_mode).ConfigureAwait(false);


        }

        public static async Task StartPlay(StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream, ServerCallContext context)
        {
            var mapData = ACDC.MapData[request.MapId];
            if(mapData==null)
            {
                Log.Warning($"StartPlay error map id {request.MapId}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.WrongParam });
                return;
            }

            var gameModeData = ACDC.GameModeData[mapData.GameMode];
            if(gameModeData==null)
            {
                Log.Warning($"StartPlay error mode {mapData.GameMode}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.WrongParam });
                return;
            }

            MinimumStartPlay minimumStartPlay;
            minimumStartPlayMap.TryGetValue(request.MapId, out minimumStartPlay);

            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.LostSession });
                return;
            }

            Log.Information($"StartPlay user_no:{session.user_no}, user_name:{session.user_name}, mapId:{request.MapId}, SelectedCharacter:{request.SelectedCharacter}, IsImmediatelyJoin{request.IsImmediatelyJoin}");

            bool checkSelectCharacter = await session.SelectCharacter(request.SelectedCharacter);
            if(checkSelectCharacter == false)
            {
                Log.Warning($"StartPlay error character id {request.SelectedCharacter}");
                await responseStream.WriteAsync(new StartPlayReply() { Code = ErrorCode.WrongParam });
                return;
            }

            bool IsAISwitch = false;
            bool IsFirstRequest = false;
            bool IsWaitingUser = await WaitingList.IsWaitingUser(session.user_no);
            bool IsMatchTimeout = false;
            // 이미 대기중이 였지만 요청이 달라진 경우,  최초 요청으로 판단
            if (IsWaitingUser == false || session.IsChangeRequest(request))
            {
                IsFirstRequest = true;
                History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.TryStartPlay, (byte)HistoryLogReason.None, request.SelectedCharacter, request.MapId, "", "");
                _ = LogProxy.writeActionLog(session, "플레이", "매칭시도", request.MapId.ToString()).ConfigureAwait(false);
            }
            else
            {
                if (DateTime.UtcNow > session.first_request_time.AddMilliseconds(ServerConfiguration.Instance.gameSetting.AIMatchTime))
                {
                    IsMatchTimeout = true;
                    if (ServerConfiguration.Instance.gameSetting.EnableAIMatch == true)
                        IsAISwitch = true;

                }

                if( minimumStartPlay != null
                    && minimumStartPlay.Enable 
                    && DateTime.UtcNow > session.first_request_time.AddSeconds(minimumStartPlay.Timeout))
                {
                    Log.Information($"matchTimeout first_request_time:{session.first_request_time}, now:{DateTime.UtcNow}, timeout:{session.first_request_time.AddSeconds(minimumStartPlay.Timeout)}");
                    IsMatchTimeout = true;
                }
            }

            Log.Information($"flag IsAISwitch:{IsAISwitch}, IsFirstRequest:{IsFirstRequest}, IsWaitingUser{IsWaitingUser}, IsMatchTimeout{IsMatchTimeout}");


            if(request.SelectedCharacter != session.character_type || request.MapId != session.map_id)
            {
                var user = await UserCache.GetUser(session.member_no, session.user_no, false);
                user.character_no = session.character_no;
                user.map_id = (byte)request.MapId;
                user.IsDirty = true;
            }

            // 게임 시작 요청 정보를 캐싱
            await session.UpdateSessionLock(request.SelectedCharacter, request.MapId, IsFirstRequest);

            if (request.IsImmediatelyJoin)
            {
                await StartPlaySimulate(request, responseStream, context, session, mapData, gameModeData);
                return;
            }

            long match_id = await MatchInstanceId.GetMatchInstanceId();
            if ((await MatchUser.OccupyMatchUser(session.user_no, match_id)) == false)
            {
                // 다른 플레이어로 인해 매칭이 완료 되었는지 확인
                if ((await RestoreMatchUser(session, responseStream)) == true)
                {
                    if (ServerConfiguration.Instance.gameSetting.EnableReJoin == false)
                    {
                        // 재입장이 불가하면, 게임 시작 직후 매칭 정보 삭제
                        await MatchUser.RemoveMatchUser(session.user_no);
                    }
                    return;
                }
                else
                {
                    await responseStream.WriteAsync(new StartPlayReply() 
                    {   
                        Code = ErrorCode.NotEnough,
                        IsStart = false,
                        BattleServerAddr = "",
                        WorldId = 0,
                        MapId = request.MapId,
                    });
                    return;
                }
            }

            if(request.IsCancel)
            {
                // 대기 목록에서 제거
                await WaitingList.RemoveWaitingUser(session.user_no);

                await MatchUser.RemoveMatchUser(session.user_no);


                await responseStream.WriteAsync(new StartPlayReply()
                {
                    Code = ErrorCode.NotEnough,
                    IsStart = false,
                    BattleServerAddr = "",
                    WorldId = 0,
                    MapId = request.MapId,
                });
                return;
            }

            // 대기중이 플레이어 찾기
            (var matchResult, var search_success) = await SearchPlayer(session, match_id, mapData, gameModeData);
            if (search_success == false && IsAISwitch == true)
            { 
                // 부족한 인원 만큼 AI로 채워 넣는다.(자신은 제외)
                int cnt = (GetStartPlayerCount(gameModeData) - 1) - matchResult.replyToClient.CharacterList.Count;
                for(int i=0;i<cnt;++i)
                {
                    matchResult.AddAI(session, mapData, gameModeData);
                }
            }

            if (search_success == true || IsAISwitch == true
                || (ServerConfiguration.Instance.gameSetting.MatchForce && IsMatchTimeout)
                || (minimumStartPlay != null && minimumStartPlay.Enable && IsMatchTimeout && matchResult.replyToClient.CharacterList.Count + 1 >= minimumStartPlay.PlayerCount)
                )
            {
                // 게임 시작
                if ((await StartPlay(match_id, session, request, responseStream, matchResult, mapData, gameModeData)) == false)
                {
                    // 예약했던 플레이어 취소
                    await MatchUser.CancelOccupiedMatchUser(matchResult.replyToBattleServer.players);
                }
                else
                {
                    if (ServerConfiguration.Instance.gameSetting.EnableReJoin == false)
                    {
                        // 재입장이 불가하면, 게임 시작 직후 매칭 정보 삭제
                        await MatchUser.RemoveMatchUser(session.user_no);
                    }
                }
                return;
            }

            // 예약했던 플레이어 취소
            await MatchUser.CancelOccupiedMatchUser(matchResult.replyToBattleServer.players);

            Log.Information("StartPlay waiting... {0}", session.user_no);

            // 대기
            await WaitStartPlay(session, request, responseStream);

            // 검색 실패시 다음 검색 조건 범위를 넓힌다.
            await session.WideningRangeRankLock();

            // 실패 결과 리턴
            matchResult.replyToClient.Code = ErrorCode.NotEnough;
            matchResult.replyToClient.IsStart = false;
            matchResult.replyToClient.CharacterList.Add(new StartPlayCharacterInfo()); // 자신포함으로 빈슬롯 한개 넣어줌
            await responseStream.WriteAsync(matchResult.replyToClient);
        }
    }
}
