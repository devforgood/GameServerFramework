using Google.Protobuf;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    /// <summary>
    /// 맵, 랭크 기준 대기열
    /// </summary>
    public class WaitingList
    {
        private static TimeSpan waiting_user_expire = new TimeSpan(0, 0, 10);
        static StackExchange.Redis.IDatabase db => Cache.Instance.GetDatabase();

        public static async Task AddWaitingUser(WaitingUser user, long user_no)
        {
            // 이전에 등록되었던 유저면 삭제
            var u = await db.StringGetAsync($"waiting_user:{user_no}");
            if (u.HasValue)
            {
                var already_exist_user = JsonConvert.DeserializeObject<WaitingUser>(u);

                // 이전에 등록한 맵,랭크와  다를 경우 삭제 후 다시 등록
                // 다르지 않은데 삭제할 경우 매칭 순서가 뒤로 밀림
                if (user.map_id != already_exist_user.map_id || user.rank != already_exist_user.rank)
                {
                    await db.ListRemoveAsync($"waiting_list:{already_exist_user.map_id}:{already_exist_user.rank}", user_no);
                    await db.ListRightPushAsync($"waiting_list:{user.map_id}:{user.rank}", user_no);
                }
            }
            else
            {
                await db.ListRightPushAsync($"waiting_list:{user.map_id}:{user.rank}", user_no);
            }

            if((await db.StringSetAsync($"waiting_user:{user_no}", JsonConvert.SerializeObject(user), waiting_user_expire))==false)
            {
                Log.Error($"AddWaitingUser waiting_user:{user_no}");
            }
        }

        public static async Task<List<long>> GetWaitingUser(int map_id, int rank, int count)
        {
            var users = new List<long>();
            for (int try_count = 0; try_count < 10; ++try_count)
            {
                var user_numbers = await db.ListRangeAsync($"waiting_list:{map_id}:{rank}", 0 + users.Count, count - 1 + users.Count);
                for(int i=0;i< user_numbers.Length;++i)
                {
                    Log.Information($"waiting user {user_numbers[i]}, try count{try_count}");
                    // 타임 아웃 검사
                    if((await db.StringGetAsync($"waiting_user:{user_numbers[i]}")).HasValue)
                    {
                        users.Add((long)user_numbers[i]);
                    }
                    else
                    {
                        // 타임아웃된 유저는 대기열에서 제거
                        await db.ListRemoveAsync($"waiting_list:{map_id}:{rank}", user_numbers[i]);
                    }

                    if (users.Count >= count)
                        return users;
                }

                if(user_numbers.Length < count)
                {
                    return users;
                }
            }
            return users;
        }

        /// <summary>
        /// 이미 대기중인가
        /// </summary>
        /// <param name="user_no"></param>
        /// <returns></returns>
        public static async Task<bool> IsWaitingUser(long user_no)
        {
            return (await db.StringGetAsync($"waiting_user:{user_no}")).HasValue;
        }
     
        /// <summary>
        ///  대기 유저중 일치하는 유저만 삭제
        /// </summary>
        /// <param name="map_id"></param>
        /// <param name="rank"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task<bool> RemoveWaitingUser(long user_no)
        {
            var u = await db.StringGetAsync($"waiting_user:{user_no}");
            if (u.HasValue)
            {
                var exist_user = JsonConvert.DeserializeObject<WaitingUser>(u);
                await db.ListRemoveAsync($"waiting_list:{exist_user.map_id}:{exist_user.rank}", user_no);

                await db.KeyDeleteAsync($"waiting_user:{user_no}");
                return true;
            }
            return false;
        }


        /// <summary>
        ///  count 만큼 데이터 삭제
        /// </summary>
        /// <param name="map_id"></param>
        /// <param name="rank"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task RemoveWaitingUser(int map_id, int rank, int count)
        {
            await db.ListTrimAsync($"waiting_list:{map_id}:{rank}", count, -1);
        }
    }
}
