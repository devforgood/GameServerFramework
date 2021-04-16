using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class UserCache
    {
        private static readonly TimeSpan session_expire = new TimeSpan(2, 0, 0);

        public static async Task UpdateUserLock(Session session, long user_no, long character_no)
        {
            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                // 유저 캐시 업데이트
                var user = await UserCache.GetUser(session.member_no, user_no, false);
                if (user != null && user != default(Models.User))
                {
                    user.character_no = character_no;
                    await UserCache.SetUser(user);
                }
                // 유저 디비 업데이트
                await UserQuery.UpdateUser(session.member_no, user_no, character_no);
            }
        }

        public static async Task<Models.User> GetUser(long member_no, long user_no, bool is_read_db, bool is_dirty_update = false, bool is_dirty = false)
        {
            Models.User user = null;
            var db = Cache.Instance.GetDatabase();
            var ret = await db.StringGetAsync($"user:{user_no}");
            if (ret.HasValue == true)
            {
                user = JsonConvert.DeserializeObject<Models.User>(ret);
            }
            else if (is_read_db)
            {
                user = await UserQuery.GetUser(member_no, user_no);
                await db.StringSetAsync($"user:{user_no}", JsonConvert.SerializeObject(user), session_expire);
            }

            if (user == null)
                return null;

            if(is_dirty_update)
            {
                user.updater = async () =>
                {
                    await SetUser(user);
                    await UserQuery.UpdateUser(member_no, user);
                };
            }

            user.IsDirty = is_dirty;

            return user;
        }

        public static async Task<bool> SetUser(Models.User user)
        {
            try
            {
                return await Cache.Instance.GetDatabase().StringSetAsync($"user:{user.user_no}", JsonConvert.SerializeObject(user), session_expire);
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
            return false;
        }
    }
}
