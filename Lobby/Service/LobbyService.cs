using GameService;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Lobby.Models;
using Newtonsoft.Json;
using Serilog;
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
    public class LobbyService : GameService.Lobby.LobbyBase
    {
        public LobbyService()
        {


        }

        /// <summary>
        /// 로그인
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task Login(LoginRequest request, IServerStreamWriter<LoginReply> responseStream, ServerCallContext context)
        {
            try
            {
                Log.Information($"login player ID:{request.PlayerId}");
                if (request.PlayerId != "")
                {
                    // 유저 아이디 영문과 숫자 조합만 허용
                    //if (Regex.IsMatch(request.UserId, @"^[a-zA-Z0-9]+$") == false)
                    //{
                    //    Log.Error($"login error {request.UserId}");

                    //    await responseStream.WriteAsync(new LoginReply() { Code = ErrorCode.WrongLetters });
                    //    return;
                    //}
                }

                var (ret, responseValidate) = await WebAPIClient.Web.validate(request.PlayerId, request.AccessToken);
                if (ret == false)
                {
                    Log.Error($"Login auth error player_id:{request.PlayerId}, peer:{context.Peer}");

                    // IDP 인증 실패시 로그인 실패처리 할 것인지
                    if (context.Peer.Contains("203.246.171.143")==true)
                    {
                        Log.Warning($"Login auth white list :{request.PlayerId}");
                    }
                    else if (ServerConfiguration.Instance.EnableCheckAccessToken)
                    {
                        await responseStream.WriteAsync(new LoginReply() { Code = ErrorCode.Auth });
                        return;
                    }
                }

                // todo : 클라이언트로 부터 맴버 초기값 세팅이 필요.
                Models.Member member = new Models.Member()
                {
                    device_model_name = "",
                    os_version = "",
                    language_code = responseValidate == null ? "" : responseValidate.lang,
                    nation_code = responseValidate == null ?  "" : responseValidate.country,
                    create_time = DateTime.UtcNow,
                };

                (var session, var err, var player_id) = await Session.LoginSession(context.Peer, request.PlayerId, request.DeviceUniqueIdentifier, true, member);
                if (session == null)
                {
                    Log.Error("login {0}", context.Peer);

                    await responseStream.WriteAsync(new LoginReply()
                    {
                        Code = err
                    });
                    return;
                }

                bool IsPlaying;
                StartPlayReply ReloadStartPlay = null;
                long match_id;
                if (ServerConfiguration.Instance.gameSetting.EnableReJoin)
                {
                    (IsPlaying, ReloadStartPlay, match_id) = await RankMatchmaking.RestoreMatchUser(session);
                }
                else
                {
                    IsPlaying = false;
                }

                Log.Information($"login user_no:{session.user_no}, player_id:{player_id}, session_id:{session.session_id}, user_name:{session.user_name}, IsPlaying:{IsPlaying}, ReloadStartPlay:{ReloadStartPlay?.BattleServerAddr}");

                var reply = new LoginReply();
                reply.UserId = player_id;
                reply.SessionId = session.session_id;
                reply.Code = ErrorCode.Success;
                reply.IsPlaying = IsPlaying;
                reply.ReloadStartPlay = ReloadStartPlay;
                reply.JsonData.Add(JsonData.Instance.OriginalData);
                reply.UserName = session.user_name ?? "";
                reply.ServerTime = DateTime.UtcNow.ToTimestamp();

                await responseStream.WriteAsync(reply);

                History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.Login, (byte)HistoryLogReason.None, 0, 0, request.PlayerId, session.user_name);

            }
            catch (Exception ex)
            {
                Log.Error($"Login error {ex.ToString()}");
            }
        }

        /// <summary>
        /// 게임 시작 요청
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task StartPlay(StartPlayRequest request, IServerStreamWriter<StartPlayReply> responseStream, ServerCallContext context)
        {
            try
            {
                //await SequentialMatchmaking.StartPlay(request, responseStream, context);
                await RankMatchmaking.StartPlay(request, responseStream, context);
            }
            catch (Exception ex)
            {
                Log.Error($"StartPlay error {ex.ToString()}");
                var reply = new StartPlayReply();
                reply.Code = ErrorCode.NotEnough;
                reply.IsStart = false;
                reply.CharacterList.Add(new StartPlayCharacterInfo()); // 자신포함으로 빈슬롯 한개 넣어줌
                await responseStream.WriteAsync(reply);
            }
        }

        /// <summary>
        /// 게임 시작 취소
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task CancelStartPlay(CancelStartPlayRequest request, IServerStreamWriter<CancelStartPlayReply> responseStream, ServerCallContext context)
        {
            try
            {
                Log.Information("StartPlay {0}", context.Peer);

                var session = await context.GetSession();
                if (session == null)
                {
                    await responseStream.WriteAsync(new CancelStartPlayReply() { Code = ErrorCode.LostSession });
                    return;
                }

                //await SequentialMatchmaking.CancelStartPlay(session, responseStream);
            }
            catch (Exception ex)
            {
                Log.Error($"CancelStartPlay error {ex.ToString()}");
            }
        }

        public override async Task SelectCharacter(SelectCharacterRequest request, IServerStreamWriter<SelectCharacterReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new SelectCharacterReply() { Code = ErrorCode.LostSession });
                return;
            }

            bool checkSelectCharacter = await session.SelectCharacter(request.CharacterId);
            if (checkSelectCharacter == false)
            {
                await responseStream.WriteAsync(new SelectCharacterReply() { Code = ErrorCode.WrongParam });
                return;
            }

            if (request.CharacterId != session.character_type)
            {
                await UserCache.UpdateUserLock(session, session.user_no, session.character_no);
            }

            await session.UpdateSessionLock(request.CharacterId, session.map_id, false);


            await responseStream.WriteAsync(new SelectCharacterReply() { Code = ErrorCode.Success });
        }

        public override async Task GetUserInfo(GetUserInfoRequest request, IServerStreamWriter<GetUserInfoReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new GetUserInfoReply() { Code = ErrorCode.LostSession });
                return;
            }

            UserInfo userInfo = new UserInfo();
            await userInfo.Load(session);

            Dictionary<string, object> properties = new Dictionary<string, object>();
            properties.Add("Name", userInfo.UserName);
            await Ranking.PutProperty(session.player_id, string.Empty, properties);

            //await Ranking.ProcessRankingReward(session);



            var member = await MemberQuery.GetMember(session.player_id);
            // todo : member.last_play_time 체크로 지급 여부 판단
            var currentTime = DateTime.UtcNow;

            var mails = await MailQuery.GetSystemMail(member.last_play_time);
            foreach(var mail in mails)
            {
                if(mail.recipient != string.Empty)
                {
                    // 받는 사람 목록이 있는 경우 해당 player_id 가 맞는지 비교
                    string[] words = mail.recipient.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                    if (words.Contains(member.player_id) == false)
                        continue;
                }

                var m = new Mail();
                switch(member.language_code)
                {
                    case "ko":
                        m.title = mail.title_ko;
                        m.body = mail.body_ko;
                        break;
                    default:
                        m.title = mail.title_en;
                        m.body = mail.body_en;
                        break;
                }
                m.mail_state = MailState.Send;
                if (mail.item_id != 0 && mail.item_count > 0)
                    m.has_item = true;
                m.item_id = mail.item_id;
                m.item_count = mail.item_count;
                m.sender = mail.sender;
                m.send_time = currentTime;
                m.expiry_time = currentTime.AddDays(mail.expiry_days);
                m.user_no = member.user_no;

                await MailQuery.SendMail(member.member_no, m);
            }

            // member.last_play_time 현재 시간으로 기록 
            await MemberQuery.UpdateMemberLastPlayTime(session.member_no, currentTime);

            await responseStream.WriteAsync(new GetUserInfoReply()
            {
                Code = ErrorCode.Success,
                User = userInfo,
            });

        }

        public override async Task ChangeUserName(ChangeUserNameRequest request, IServerStreamWriter<ChangeUserNameReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.LostSession });
                return;
            }

            //유저 아이디 영문과 숫자 조합만 허용
            //if (Regex.IsMatch(request.UserName, @"^[a-zA-Z0-9]+$") == false)
            if (Regex.IsMatch(request.UserName, @"^[a-zA-Z0-9가-힣]*$") == false)
            {
                Log.Error($"ChangeUserName error {request.UserName}");
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.WrongLetters });
                return;
            }

            if (request.UserName.Length < 2 || request.UserName.Length > 10)
            {
                Log.Error($"ChangeUserName error {request.UserName}");
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.WrongLetters });
                return;
            }

            if (request.UserName.IsLetterOrDigit() == false)
            {
                Log.Error($"ChangeUserName error {request.UserName}");
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.WrongLetters });
                return;
            }

            if (await BannedWordFilter.Check(request.UserName) == false)
            {
                Log.Error($"ChangeUserName error {request.UserName}");
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.WrongLetters });
                return;
            }

            // 유저 디비 업데이트
            var ret = await MemberQuery.UpdateMember(session.member_no, request.UserName);
            if (ret != ErrorCode.Success)
            {
                await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ret });
                return;
            }


            // 세션 캐시 업데이트
            await Session.UpdateSessionLock(session.session_id, delegate (Session s) { s.user_name = request.UserName; });

            await responseStream.WriteAsync(new ChangeUserNameReply() { Code = ErrorCode.Success });
        }

        public override async Task<GameService.DebugCommandReply> DebugCommand(DebugCommandRequest request, ServerCallContext context)
        {
            if (ServerConfiguration.Instance.EnableDebugCommand == false)
            {
                return new DebugCommandReply() { Code = ErrorCode.WrongParam };
            }

            Log.Information($"DebugCommand {request.Cmd}, {request.Param1}, {request.Param2}, {request.Param3}, {request.Param4}");

            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new DebugCommandReply() { Code = ErrorCode.LostSession };
                }

                return await Lobby.DebugCommand.Execute(session, request.Cmd, request.Param1, request.Param2, request.Param3, request.Param4);
            }
            catch (Exception ex)
            {
                Log.Error($"DebugCommand {ex.ToString()}");

                return new DebugCommandReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task GetShop(GetShopRequest request, IServerStreamWriter<GetShopReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new GetShopReply() { Code = ErrorCode.LostSession });
                return;
            }

            var shops = await ShopManager.GetShopLock(session);

            await responseStream.WriteAsync(new GetShopReply() { Code = ErrorCode.Success, UserShops = shops });
        }

        public override async Task BuyItem(BuyItemRequest request, IServerStreamWriter<BuyItemReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new BuyItemReply() { Code = ErrorCode.LostSession });
                return;
            }

            var reply = new BuyItemReply();
            (reply.Code, reply.Item, reply.AccountGoods) = await ShopManager.BuyItem(session, request.ShopItemId, request.ShopId);

            await responseStream.WriteAsync(reply);
        }


        /// <summary>
        /// 미션 정보 얻기
        /// 접속, 게임 종료, 미션창 활성중 리셋, 리셋이후 첫 미션창 오픈, 기타 로비에서 미션 조건 만족시
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task GetMission(GetMissionRequest request, IServerStreamWriter<GetMissionReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new GetMissionReply() { Code = ErrorCode.LostSession });
                return;
            }

            var missions = await MissionManager.GetMissionsLock(session);

            await responseStream.WriteAsync(new GetMissionReply() { Code = ErrorCode.Success, UserMissions = missions });
        }

        /// <summary>
        /// 각 미션 보상
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task GetRewardMission(GetRewardMissionRequest request, IServerStreamWriter<GetRewardMissionReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new GetRewardMissionReply() { Code = ErrorCode.LostSession });
                return;
            }

            var ret = await MissionManager.Reward(session, request.MissonId);

            await responseStream.WriteAsync(new GetRewardMissionReply() { Code = ret });
        }

        /// <summary>
        /// 미션 그룹(or Base) 보상
        /// </summary>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public override async Task GetRewardMissionBase(GetRewardMissionBaseRequest request, IServerStreamWriter<GetRewardMissionBaseReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new GetRewardMissionBaseReply() { Code = ErrorCode.LostSession });
                return;
            }

            var ret = await MissionManager.RewardBase(session, request.MissonBaseId);

            await responseStream.WriteAsync(ret);
        }

        public override async Task UpgradePowerLevel(UpgradePowerLevelRequest request, IServerStreamWriter<UpgradePowerLevelReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            if (session == null)
            {
                await responseStream.WriteAsync(new UpgradePowerLevelReply() { Code = ErrorCode.LostSession });
                return;
            }

            var (ret, goods, character_info) = await CharacterGrowth.UpgradePowerLevel(session, request.CharacterId);

            await responseStream.WriteAsync(new UpgradePowerLevelReply() { Code = ret, AccountGoods = goods, CharacterLevel = character_info.CharacterLevel, CharacterPiece = character_info.CharacterPiece });
        }

        public override async Task SelectFirstCharacter(SelectFirstCharacterRequest request, IServerStreamWriter<SelectFirstCharacterReply> responseStream, ServerCallContext context)
        {
            var session = await context.GetSession();
            var user = await UserCache.GetUser(session.member_no, session.user_no, false);
            if (session == null || user == null)
            {
                await responseStream.WriteAsync(new SelectFirstCharacterReply() { Code = ErrorCode.LostSession });
                return;
            }

            if (session.character_no != 0 || user.character_no != 0)
            {
                await responseStream.WriteAsync(new SelectFirstCharacterReply() { Code = ErrorCode.NotAvailable });
                return;
            }

            if(!ACDC.CharacterSelectData[1].CharacterId.Contains(request.CharacterId))
            {
                await responseStream.WriteAsync(new SelectFirstCharacterReply() { Code = ErrorCode.WrongParam });
                return;
            }

            var character = await CharacterManager.InsertCharacter(session.member_no, session.user_no, session.user_name, session.player_id, request.CharacterId);
            if (character == null)
            {
                await responseStream.WriteAsync(new SelectFirstCharacterReply() { Code = ErrorCode.NotAvailable });
                return;
            }
            await UserCache.UpdateUserLock(session, session.user_no, character.character_no);
            await Session.UpdateSessionLock(session.session_id, delegate (Session s) { s.character_no = character.character_no; });

            History.Info(session.member_no, session.user_no, session.character_no, HistoryLogAction.GainItem, (byte)HistoryLogReason.SelectCharacter, character.character_type, 1, "", "");
            _ = LogProxy.writeActionLog(session, "캐릭터", "최초획득", character.character_type.ToString()).ConfigureAwait(false);

            await responseStream.WriteAsync(new SelectFirstCharacterReply() { Code = ErrorCode.Success, CharacterInfo = CharacterManager.CreateCharacterInfo(character, ACDC.CharacterData[character.character_type]) });
        }

        public override async Task<GameService.GetGameEventsReply> GetGameEvents(GetGameEventsRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetGameEventsReply() { Code = ErrorCode.LostSession };
                }

                var events = await GameEventQuery.Gets(session.member_no, session.user_no);

                // 이벤트 초기화
                if(events.Count == 0)
                {
                    var nextDate = DateTime.UtcNow.AddDays(1).Date;
                    var endTime = new DateTime(nextDate.Year, nextDate.Month, nextDate.Day, 0, 0, 0, DateTimeKind.Utc);

                    var game_event = new Models.GameEvent()
                    {
                        user_no = session.user_no,
                        event_id = 1,
                        reward = 0,
                        occ_time = endTime,
                    };
                    events.Add(game_event);
                    await GameEventQuery.Add(session.member_no, game_event);
                }

                // 이벤트 목록 생성
                var reply = new GetGameEventsReply();
                reply.Code = ErrorCode.Success;
                foreach (var game_event in events)
                {
                    reply.Events.Add(new EventInfo()
                    {
                        EventId = game_event.event_id,
                        IsReward = (game_event.reward == 0 ? false : true),
                        RewardStartTime = DateTime.SpecifyKind(game_event.occ_time, DateTimeKind.Utc).ToTimestamp(),
                        RewardItemId = (int)GameItemId.OpenEventBox,
                    });
                }

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetGameEvents {ex.ToString()}");

                return new GetGameEventsReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GameService.GetRewardEventReply> GetRewardEvent(GetRewardEventRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetRewardEventReply() { Code = ErrorCode.LostSession };
                }


                var reply = new GetRewardEventReply();
                reply.AccountGoods = new Goods();
                reply.Item = new ItemList();
                await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
                {
                    var events = await GameEventQuery.Gets(session.member_no, session.user_no);

                    var gameEvent = events.Where(x => x.event_id == request.EventId).FirstOrDefault();
                    if(gameEvent == null || gameEvent == default(Models.GameEvent))
                    {
                        return new GetRewardEventReply() { Code = ErrorCode.NotExist };
                    }

                    if(gameEvent.reward != 0)
                    {
                        return new GetRewardEventReply() { Code = ErrorCode.AlreadyReward };
                    }

                    if (gameEvent.occ_time > DateTime.UtcNow)
                    {
                        return new GetRewardEventReply() { Code = ErrorCode.NotEnough };
                    }

                    // 보상 지급
                    await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                    await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                    {
                        await Inventory.Insert(session, user, character, (int)GameItemId.OpenEventBox, 1, new LogReason("A_EVENT", gameEvent.event_id.ToString()), reply.Item);
                        reply.AccountGoods.Set(user);
                    }

                    gameEvent.reward = 1;
                    await GameEventQuery.Update(session.member_no, gameEvent);
                }

                reply.Code = ErrorCode.Success;
                return reply;

            }
            catch (Exception ex)
            {
                Log.Error($"GetRewardEvent {ex.ToString()}");

                return new GetRewardEventReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<SelectMapReply> SelectMap(SelectMapRequest request, ServerCallContext context)
        {
            SelectMapReply reply = new SelectMapReply() { Code = ErrorCode.WrongParam };
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    reply.Code = ErrorCode.LostSession;
                    return reply;
                }

                if(request.MapId == session.map_id ||ACDC.MapData[request.MapId] == null)
                    return reply;

                await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                {
                    user.map_id = (byte)request.MapId;
                    user.IsDirty = true;
                }

                await session.UpdateSessionLock(session.character_type, request.MapId, false);
                reply.Code = ErrorCode.Success;
            }
            catch(Exception ex)
            {
                Log.Error($"GetRewardEvent {ex.ToString()}");
            }
            return reply;
        }

        public override async Task<GameService.GetAdvertisementRewardsReply> GetAdvertisementRewards(GetAdvertisementRewardsRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetAdvertisementRewardsReply() { Code = ErrorCode.LostSession };
                }

                var rewards = await AdvertisementRewardQuery.Gets(session.member_no, session.user_no);
                var currentTime = DateTime.UtcNow;
                var reply = new GetAdvertisementRewardsReply();
                reply.Code = ErrorCode.Success;
                foreach (var reward in rewards)
                {
                    var adData = ACDC.AdListData[reward.advertisement_id];
                    if (adData == null || adData == default(JAdListData))
                        continue;

                    if (core.MathHelpers.GetResetTime(adData.ResetTime, reward.occ_time) != core.MathHelpers.GetResetTime(adData.ResetTime, currentTime))
                        continue;

                    reply.AdvertisementRewards.Add(new AdvertisementRewardInfo()
                    {
                        AdvertisementId = reward.advertisement_id,
                        RewardCount = reward.reward,
                    });
                }

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetGameEvents {ex.ToString()}");

                return new GetAdvertisementRewardsReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GameService.RewardAdvertisementReply> RewardAdvertisement(RewardAdvertisementRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new RewardAdvertisementReply() { Code = ErrorCode.LostSession };
                }

                var adData = ACDC.AdListData[request.AdvertisementId];
                if(adData == null || adData == default(JAdListData))
                {
                    return new RewardAdvertisementReply() { Code = ErrorCode.WrongParam };
                }

                bool is_insert = false;

                var reply = new RewardAdvertisementReply();
                reply.AccountGoods = new Goods();
                reply.Item = new ItemList();
                var currentTime = DateTime.UtcNow;
                int RewardCount = 0;
                await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
                {
                    var rewards = await AdvertisementRewardQuery.Gets(session.member_no, session.user_no);
                    var reward = rewards.Where(x => x.advertisement_id == request.AdvertisementId).FirstOrDefault();
                    if (reward == null || reward == default(Models.AdvertisementReward))
                    {
                        is_insert = true;
                        reward = new AdvertisementReward()
                        {
                            user_no = session.user_no,
                            advertisement_id = adData.Id,
                            reward = 0,
                            occ_time = currentTime,
                        };
                    }
                    else
                    {
                        // refresh
                        if(core.MathHelpers.GetResetTime(adData.ResetTime, reward.occ_time) != core.MathHelpers.GetResetTime(adData.ResetTime, currentTime))
                        {
                            reward.occ_time = currentTime;
                            reward.reward = 0;
                        }
                    }

                    if (reward.reward >= adData.ViewLimit)
                    {
                        return new RewardAdvertisementReply() { Code = ErrorCode.OverLimit };
                    }

                    // 보상 지급
                    await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                    await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                    {
                        await Inventory.Insert(session, user, character, adData.ItemId, adData.Count, new LogReason("A_AD", adData.Id.ToString()), reply.Item, null, adData.Id.ToString());
                        reply.AccountGoods.Set(user);
                    }

                    ++reward.reward;
                    RewardCount = reward.reward;
                    reward.occ_time = currentTime;
                    if (is_insert)
                    {
                        await AdvertisementRewardQuery.Add(session.member_no, reward);
                    }
                    else
                    {
                        await AdvertisementRewardQuery.Update(session.member_no, reward);
                    }

                    _ = LogProxy.writeActionLog(session, "광고시청", "광고", adData.Id.ToString()).ConfigureAwait(false);

                }

                reply.Code = ErrorCode.Success;
                reply.RewardCount = RewardCount;
                reply.AdvertisementId = request.AdvertisementId;
                return reply;

            }
            catch (Exception ex)
            {
                Log.Error($"GetRewardEvent {ex.ToString()}");

                return new RewardAdvertisementReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GetMailItemsReply> GetMailItems(GetMailItemsRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetMailItemsReply() { Code = ErrorCode.LostSession };
                }

                var reply = new GetMailItemsReply();
                reply.Item = new ItemList();
                reply.AccountGoods = new Goods();
                bool ret = false;
                (ret, reply.Item, reply.AccountGoods) = await MailBox.GetMailItems(session, request.MessageIds.ToList(), request.CharacterId);
                if (ret==false)
                {
                    reply.Code = ErrorCode.NotExist;
                }

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetMailItems {ex.ToString()}");

                return new GetMailItemsReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GetMailsReply> GetMails(GetMailsRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetMailsReply() { Code = ErrorCode.LostSession };
                }

                long? NextPageKey = request.NextPageKey != string.Empty ? (long?)long.Parse(request.NextPageKey) : null;
                List<MailState> States = request.States.Count != 0 ? request.States.Select(x=> (MailState)System.Enum.Parse(typeof(MailState), x)).ToList() : null;

                var reply = new GetMailsReply();
                reply.Mails = await MailBox.GetMails(session.member_no, session.user_no, request.Count, NextPageKey, States);

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetMailItems {ex.ToString()}");

                return new GetMailsReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<MarkAsReadMailsReply> MarkAsReadMails(MarkAsReadMailsRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new MarkAsReadMailsReply() { Code = ErrorCode.LostSession };
                }

                var reply = new MarkAsReadMailsReply();
                reply.Code = ErrorCode.Success;
                if(await MailBox.MarkAsReadMails(session.player_id, request.MessageIds.ToList()) == false)
                {
                    reply.Code = ErrorCode.WrongParam;
                }

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetMailItems {ex.ToString()}");

                return new MarkAsReadMailsReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<PurchaseInAppReplay> PurchaseInApp(PurchaseInAppRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new PurchaseInAppReplay() { Code = ErrorCode.LostSession };
                }
                
                var(validated, receiptChecker) = await InAppPurchase.IsValidPurchase(request);
                if (validated == false || receiptChecker == null)
                {
                    return new PurchaseInAppReplay() { Code = ErrorCode.WrongParam };
                }

                // todo : ProductId로 상점 아이템을 찾아 구매 처리함  (receiptChecker.ProductId) 

                var reply = new PurchaseInAppReplay();
                (reply.Code, reply.Item, reply.AccountGoods) = await ShopManager.BuyItem(session, 34, 4);

                return reply;
            }
            catch (Exception ex)
            { 
                Log.Error($"PurchaseInApp {ex.ToString()}");

                return new PurchaseInAppReplay() { Code = ErrorCode.WrongParam };
            }
        }


        //////////////////////////////////////////////
        /// Ranking
        public override async Task<GetLeaderboardReply> GetLeaderboard(GetLeaderboardRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {

                    return new GetLeaderboardReply() { Code = ErrorCode.LostSession };
                }

                var leaderboard = await Ranking.GetInfo(request.Info);

                return new GetLeaderboardReply()
                {
                    Code = ErrorCode.Success,
                    Season = new Season()
                    {
                        Type = leaderboard.season.type,
                        ResetDay = leaderboard.season.resetDay,
                        ResetHour = leaderboard.season.resetHour,
                        BeginTime = leaderboard.season.beginTime,
                        EndTime = leaderboard.season.endTime,
                        NextResetTime = leaderboard.season.nextResetTime,
                        Seq = leaderboard.season.seq,
                    }
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetLeaderboard {ex.ToString()}");

                return new GetLeaderboardReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GetRankReply> GetRank(GetRankRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetRankReply() { Code = ErrorCode.LostSession };
                }


                var rank = await Ranking.GetRank(session.player_id, request.Info, request.CharacterId);
                if (rank == null)
                {
                    return new GetRankReply() { Code = ErrorCode.NotExist };
                }


                return new GetRankReply()
                {
                    Code = ErrorCode.Success,
                    Rank = rank.rank,
                    Score = rank.score,
                    Cardinality = rank.cardinality,
                    Highscore = rank.highscore
                };
            }
            catch (Exception ex)
            {
                Log.Error($"GetRank {ex.ToString()}");

                return new GetRankReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GetRankListReply> GetRankList(GetRankListRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if (session == null)
                {
                    return new GetRankListReply() { Code = ErrorCode.LostSession };
                }


                var rankList = await Ranking.GetRankList(session.player_id, request.Info, request.From, request.To, request.ReadMy, request.CharacterId, request.WithoutProperty);
                var reply = new GetRankListReply()
                {
                    Code = ErrorCode.Success,
                    Cardinality = rankList.cardinality,
                    NextResetTime = rankList.nextResetTime,
                    MyRank = request.ReadMy ? rankList.myRank : 0,
                    MyScore = request.ReadMy ? rankList.myScore : 0,
                };

                foreach (var rank in rankList.scores)
                {
                    reply.Scores.Add(new ScoreInfo()
                    {
                        Rank = rank.rank,
                        Score = rank.score,
                        Name = rank.property != null && rank.property.ContainsKey("Name") ? rank.property["Name"] : string.Empty
                    });
                }

                return reply;
            }
            catch (Exception ex)
            {
                Log.Error($"GetRankList {ex.ToString()}");

                return new GetRankListReply() { Code = ErrorCode.WrongParam };
            }
        }

        public override async Task<GetScoreListReply> GetScoreList(GetScoreListRequest request, ServerCallContext context)
        {
            try
            {
                var session = await context.GetSession();
                if(session == null)
                {
                    return new GetScoreListReply() { Code = ErrorCode.LostSession };
                }

                var scoreList = await Ranking.GetScoreList(request.Info, request.WithoutProperty, request.PlayerIds.ToList());
                var reply = new GetScoreListReply()
                {
                    Code = ErrorCode.Success,
                    Cardinality = scoreList.cardinality,
                    SortingType = scoreList.sortingType
                };

                foreach(var score in scoreList.scores)
                {
                    reply.Scores.Add(new ScoreInfo()
                    {
                        Rank = score.rank,
                        Score = score.score,
                        Name = request.WithoutProperty ? string.Empty : score.property["name"]
                    });
                }

                return new GetScoreListReply() { Code = ErrorCode.Success };
            }
            catch(Exception ex)
            {
                Log.Error($"GetScoreList {ex.ToString()}");

                return new GetScoreListReply() { Code = ErrorCode.WrongParam };
            }
        }
        //////////////////////////////////////////////
    }
}
