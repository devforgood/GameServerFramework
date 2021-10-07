using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class Session
    {
        private static readonly TimeSpan duplicate_session_expire = new TimeSpan(0, 15, 0);
        private static readonly TimeSpan session_expire = new TimeSpan(2, 0, 0);
        private static readonly int max_try_count = 10;

        public string session_id;
        public string player_id;
        public long member_no;
        public int selected_character;
        public int[] characters;
        public int coin;
        public string nickname;
        public int best_record;
        public string device_uid;
        public int[] in_app_purchase;
        public string country;

        public Session()
        {
            session_id = Guid.NewGuid().ToString();
        }

        public static async Task<string> GetSessionId(string player_id)
        {
            var session_id = await Cache.Instance.GetDatabase().StringGetAsync($"player_id:{player_id}");
            if (session_id.HasValue == false)
                return string.Empty;

            return session_id;
        }

        public static async Task<Session> GetSession(string session_id, bool touch = true)
        {
            var ret = await Cache.Instance.GetDatabase().StringGetAsync($"session:{session_id}");
            if (ret.HasValue == false)
                return null;

            var session = JsonConvert.DeserializeObject<Session>(ret);

            if (touch == true)
            {
                await session.Touch();
            }

            return session;
        }

        public async Task Touch()
        {
            await Cache.Instance.GetDatabase().KeyExpireAsync($"session:{session_id}", session_expire);
            await Cache.Instance.GetDatabase().KeyExpireAsync($"player_id:{player_id}", session_expire);
        }

        public static async Task<bool> SetSession(Session session, bool is_first = false)
        {
            if (is_first)
            {
                int try_count = 0;
                string session_value = "";
                for (try_count = 0; try_count < max_try_count; ++try_count)
                {
                    // 혹시 발생할 session 키값 중복 발생시 재시도
                    session_value = JsonConvert.SerializeObject(session);
                    if (await Cache.Instance.GetDatabase().StringSetAsync($"session:{session.session_id.ToString()}", session_value, session_expire, When.NotExists))
                        break;

                    // 재발급
                    session.session_id = Guid.NewGuid().ToString();
                }

                if (try_count == max_try_count)
                {
                    return false;
                }
            }
            else
            {
                await Cache.Instance.GetDatabase().StringSetAsync($"session:{session.session_id.ToString()}", JsonConvert.SerializeObject(session), session_expire);
            }

            await Cache.Instance.GetDatabase().StringSetAsync($"player_id:{session.player_id}", session.session_id.ToString(), session_expire);

            return true;
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
        public static async Task<bool> ClearSession(string session_id)
        {
            return await Cache.Instance.GetDatabase().KeyDeleteAsync($"session:{session_id}");
        }

        //public static async Task<bool> UpdateSession(Login.Member member)
        //{
        //    var session_id = await Session.GetSessionId(member.player_id);
        //    if (string.IsNullOrEmpty(session_id))
        //        return false;

        //    var session = await Session.GetSession(session_id);
        //    if (session == null)
        //        return false;

        //    session.selected_character = member.selected_character;
        //    session.characters = JsonConvert.DeserializeObject<int[]>(member.characters);
        //    session.coin = member.coin;
        //    session.nickname = member.nickname;
        //    session.best_record = member.best_record;
        //    bool ret = await Session.SetSession(session);

        //    return ret;
        //}
    }
}
