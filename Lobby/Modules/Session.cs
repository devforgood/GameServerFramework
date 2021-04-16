using GameService;
using Newtonsoft.Json;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Lobby
{
    public class Session : ServerCommon.Session
    {
        private static readonly TimeSpan session_expire = new TimeSpan(2, 0, 0);
        private static readonly TimeSpan duplicate_session_expire = new TimeSpan(0, 15, 0);
        private static readonly int max_try_count = 10;

        static readonly int defaultMinRank = 1;
        static readonly int defaultMaxRank = 20;

        static readonly byte defaultUserMapId = 2;

        public Session()
        {
            session_id = GetNewSessionId();
        }

        public static string GetNewSessionId()
        {
            return Guid.NewGuid().ToString();
        }

        public bool IsChangeRequest(StartPlayRequest request)
        {
            return character_type != (byte)request.SelectedCharacter || map_id != request.MapId;
        }

        public async Task UpdateSessionLock(int selectedCharacter, int mapId, bool IsFirstRequest)
        {
            // 이전에 선택한 케릭터 다르면 세션에 업데이트를 한다.
            if (character_type != selectedCharacter || map_id != mapId  || IsFirstRequest )
            {
                if(IsFirstRequest)
                {
                    // 매칭 검색 조건 범위 초기화
                    min_rank = rank;
                    max_rank = rank;
                    first_request_time = DateTime.UtcNow;
                }

                character_type = (byte)selectedCharacter;
                map_id = mapId;

                await Session.UpdateSessionLock(session_id, delegate (Session s)
                {
                    s.min_rank = min_rank;
                    s.max_rank = max_rank;
                    s.rank = rank;
                    s.first_request_time = first_request_time;
                    s.character_type = character_type;
                    s.character_no = character_no;
                    s.map_id = map_id;
                });
            }
        }

        public async Task WideningRangeRankLock()
        {
            // 매칭 랭크 min/max 폭 증가
            int last_min_rank = this.min_rank;
            int last_max_rank = this.max_rank;

            if (this.rank - this.min_rank < 5)
            {
                this.min_rank -= 1;
                if (this.min_rank < defaultMinRank)
                    this.min_rank = defaultMinRank;
            }
            if (this.max_rank - this.rank < 5)
            {
                this.max_rank += 1;
                if (this.max_rank > defaultMaxRank)
                    this.max_rank = defaultMaxRank;
            }

            if (this.min_rank != last_min_rank || this.max_rank != last_max_rank)
            {
                //Log.Information($"WideningRangeRankLock session:{session_id}, rank:{this.rank}, min:{this.min_rank}, max:{this.max_rank}");

                await Session.UpdateSessionLock(session_id, delegate (Session s)
                {
                    s.min_rank = min_rank;
                    s.max_rank = max_rank;
                });
            }
        }


        public static async Task<long> GiveTempUserNo()
        {
            // todo : 디비에 저장된 user_no 로 변경 예정
            return await Cache.Instance.GetDatabase().StringIncrementAsync("temp_user_no");
        }

        private static async Task<bool> TryInsertSession(Session session)
        {
            var db = Cache.Instance.GetDatabase();
            int try_count = 0;
            string session_value = "";
            for (try_count = 0; try_count < max_try_count; ++try_count)
            {
                // 혹시 발생할 session 키값 중복 발생시 재시도
                session_value = JsonConvert.SerializeObject(session);
                if (await db.StringSetAsync($"session:{session.session_id.ToString()}", session_value, session_expire, When.NotExists))
                    break;

                // 재발급
                session.session_id = GetNewSessionId();
            }

            if (try_count == max_try_count)
            {
                Log.Error($"TryInsertSession try count over {session_value}");
                return false;
            }

            await db.StringSetAsync($"session_user:{session.user_no}", session.session_id.ToString(), session_expire);

            return true;
        }

        public static async Task<(bool, string)> GetCandidateName()
        {
            bool bSuccess = false;
            string user_id = "";
            foreach (var candidateName in ACDC.UsernameData)
            {
                user_id = candidateName.Value.name;

                var user_info = new ServerCommon.UserInfo();
                user_info.user_id = user_id;
                //user_info.user_no = user_no;
                if (await Cache.Instance.GetDatabase().StringSetAsync($"user_id:{user_id}", JsonConvert.SerializeObject(user_info), null, When.NotExists) == true)
                {
                    bSuccess = true;
                    break;
                }
            }
            return (bSuccess, user_id);
        }

        public static async Task<(Models.Member, Models.User, bool, bool)> GetMemberUser(string player_id)
        {
            Models.Member last_member = null;
            Models.User last_user = null;
            bool hasMember = false;
            bool hasUser = false;
            last_member = await MemberQuery.GetMember(player_id);
            if (last_member != default(Models.Member))
            {
                hasMember = true;

                last_user = await UserQuery.GetUser(last_member.member_no, last_member.user_no);
                if (last_user != default(Models.User))
                {
                    hasUser = true;
                }
            }
            return (last_member, last_user, hasMember, hasUser);
        }

        public static async Task<bool> IsDuplicateTimeout(string session_id)
        {
            TimeSpan? timeToLive = await Cache.Instance.GetDatabase().KeyTimeToLiveAsync($"session:{session_id}");
            if (timeToLive != null)
            {
                if (session_expire - timeToLive.Value > duplicate_session_expire)
                {
                    return true;
                }
            }
            else
            {
                return true;
            }
            return false;
        }

        public static async Task<(Session, ErrorCode, string)> LoginSession(string endpoint, string player_id, string device_uid, bool check_device_uid, Models.Member member)
        {
            var db = Cache.Instance.GetDatabase();
            bool hasMember = false;
            bool hasUser = false;
            Models.User user = null;

            if (player_id == "")
            {
                // 게스트 계정 발급, prefix 'G'
                player_id = $"G{Guid.NewGuid().ToString("N")}";
            }
            else
            {
                Models.Member last_member = null;
                Models.User last_user = null;

                // 캐시 확인
                var ret = await db.StringGetAsync($"player_id:{player_id}");
                if (ret.HasValue == false)
                {
                    // 디비 확인
                    (last_member, last_user, hasMember, hasUser) = await GetMemberUser(player_id);
                }
                else
                {
                    string session_id = ret;

                    var timeout = await IsDuplicateTimeout(session_id);

                    // 세션이 만료되지 않았다면 가져와서 사용
                    var last_session = await GetSession(session_id, false);
                    if (last_session != null)
                    {
                        // SystemInfo.deviceUniqueIdentifier 비교
                        if (check_device_uid 
                            && string.IsNullOrEmpty(last_session.device_uid)==false // 이전 로그인이 어드민 무시
                            && last_session.device_uid != device_uid) 
                        {
                            Log.Information($"LoginSession diff last : {last_session.device_uid}, new : {device_uid}, timeout : {timeout}");

                            if (timeout)
                            {
                                // 이전 세션을 클리어 
                                await ClearSession(session_id);

                                //캐싱된 데이터를 사용하지 않고 로그인 처리
                                (last_member, last_user, hasMember, hasUser) = await GetMemberUser(player_id);
                            }
                            else
                            {
                                return (null, ErrorCode.DuplicateLogin, player_id);

                            }
                        }
                        else
                        {
                            await last_session.Touch();
                            return (last_session, ErrorCode.Success, player_id);
                        }
                    }
                    else
                    {
                        (last_member, last_user, hasMember, hasUser) = await GetMemberUser(player_id);
                    }
                }

                if(hasMember)
                {
                    member = last_member;
                }

                if(hasUser)
                {
                    user = last_user;
                }

            }

            if (hasMember == false)
            {
                member.user_no = 0;
                member.player_id = player_id;

                member = await MemberQuery.AddMember(member);
            }


            if (hasUser == false)
            {
                user = new Models.User()
                {
                    user_no = member.member_no,
                    character_no = 0,
#if USE_TEMP_SET_NICKNAME
                    user_name = Guid.NewGuid().ToString("N"), // todo 임시코드
#endif
                    map_id = defaultUserMapId,
                };

#if USE_TEMP_SET_NICKNAME
                // todo : 임시 닉네임 설정
                // 유저로부터 입력 받도록 수정이 필요
                (var ret, var user_name) = await GetCandidateName();
                if (ret == true)
                {
                    user.user_name = user_name;
                }
#endif

                user = await UserQuery.AddUser(member.member_no, user);
            }

            if (member.user_no != user.user_no)
            {
                // member 와 user 연결
                // member테이블에 데이터는 있는데 user테이블에 데이터가 없는 경우
                await MemberQuery.UpdateMember(member.member_no, user.user_no);
            }


            int character_rank_level = defaultMinRank;
            if (user.character_no != 0)
            {
                var charac = await CharacterCache.Instance.GetEntity(member.member_no, user.character_no, true, false, false);
                if(charac != null && charac != default(Models.Character))
                {
                    var rankData = ACDC.RankData.GetRank(charac.battle_score);
                    if (rankData != null)
                    {
                        character_rank_level = rankData.Rank;
                    }
                }
            }

            var session = new Session()
            {
                remote_endpoint = endpoint,
                user_no = user.user_no,
                user_name = member.user_name,
                player_id = player_id,
                member_no = member.member_no,
                character_no = user.character_no,
                rank = character_rank_level,
                device_uid = device_uid,
                map_id = user.map_id
            };

            bool result = await TryInsertSession(session);
            if (result == false)
            {
                Log.Error($"CreateSession 1 try count over player_id:{player_id}");
                return (null, ErrorCode.TryCountOver, player_id);
            }

            await db.StringSetAsync($"player_id:{player_id}", session.session_id.ToString(), session_expire);


            return (session, ErrorCode.Success, player_id);
        }


        public static async Task UpdateSessionLock(string session_id, Action<Session> action)
        {
            // 세션 캐시 업데이트
            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session_id}"))
            {
                // reload
                var session = await Session.GetSession(session_id);
                action(session);
                await Session._UpdateSession(session);
            }
        }


        private static async Task<Session> _UpdateSession(Session session)
        {
            var db = Cache.Instance.GetDatabase();

            await db.StringSetAsync($"session:{session.session_id.ToString()}", JsonConvert.SerializeObject(session), session_expire);
            await db.KeyExpireAsync($"session_user:{session.user_no}", session_expire);
            await db.KeyExpireAsync($"player_id:{session.player_id}", session_expire);

            return session;
        }

        public async Task Touch()
        {
            await Cache.Instance.GetDatabase().KeyExpireAsync($"session:{session_id}", session_expire);
            await Cache.Instance.GetDatabase().KeyExpireAsync($"session_user:{user_no}", session_expire);
            await Cache.Instance.GetDatabase().KeyExpireAsync($"player_id:{player_id}", session_expire);
        }

        public static async Task<Session> GetSession(string session_id, bool touch = true)
        {
            var db = Cache.Instance.GetDatabase();
            var key = $"session:{session_id}";
            var ret = await db.StringGetAsync(key);
            if (ret.HasValue == false)
                return null;

            var session = JsonConvert.DeserializeObject<Session>(ret);

            if (touch == true)
            {
                await session.Touch();
            }

            return session;
        }

        public static async Task<bool> ClearSession(string session_id)
        {
            return await Cache.Instance.GetDatabase().KeyDeleteAsync($"session:{session_id}");
        }

        public static async Task<Session> GetSession(long user_no, bool touch = true)
        {
            var db = Cache.Instance.GetDatabase();
            var key = $"session_user:{user_no}";
            var ret = await db.StringGetAsync(key);
            if (ret.HasValue == false)
                return null;

            return await GetSession(ret, touch);
        }

        public static async Task<bool> IsAvailableSesssion(long user_no)
        {
            return (await Cache.Instance.GetDatabase().StringGetAsync($"session_user:{user_no}")).HasValue;

        }

        public async Task<bool> SelectCharacter(int characterId)
        {
            var characterData = ACDC.CharacterData[characterId];
            if (characterData == null)
            {
                return false;
            }

            if(characterData.Enable == false)
            {
                return false;
            }

            if (character_type != characterId)
            {
                var character = await CharacterCache.Instance.GetEntity(member_no, user_no, characterId, true, false, false);
                if (character == null || character == default(Models.Character))
                {
                    return false;
                    // todo : 테스트용으로 아직 보유하지 않은 케릭터로 플레이가 가능하도록 처리
                    //character = await CharacterCache.Instance.InsertEntity(new Models.Character()
                    //{
                    //    user_no = user_no,
                    //    character_type = characterId,
                    //    character_level = characterData.Level,
                    //});
                }

                character_no = character.character_no;

                // 캐릭터 변경시 매칭 랭크도 해당 케릭터 매칭 랭크로 변경
                var rankData = ACDC.RankData.GetRank(character.battle_score);
                if (rankData != null)
                {
                    rank = rankData.Rank;
                }
            }
            return true;
        }

    }
}
