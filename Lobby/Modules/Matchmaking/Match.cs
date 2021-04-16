using GameService;
using Google.Protobuf;
using Serilog;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class Match
    {
        private static TimeSpan match_expire = new TimeSpan(0, 10, 0);

        /// <summary>
        /// 매칭완료를 다른 유저에 알리기 위해 기록
        /// </summary>
        /// <param name="reply"></param>
        /// <returns></returns>
        public static async Task SaveMatch(long match_id, StartPlayReply reply)
        {
            string reply_str = new JsonFormatter(new JsonFormatter.Settings(true)).Format(reply);
            Log.Information($"Save match_id:{match_id}, StartPlay Reply {reply_str}");

            await Cache.Instance.GetDatabase().StringSetAsync($"match:{match_id}", reply_str, match_expire);

            Log.Information($"StartPlay SaveMatch complete. match_id:{match_id}");
        }

        public static async Task<(bool, StartPlayReply)> LoadMatch(long match_id)
        {
            var value = await Cache.Instance.GetDatabase().StringGetAsync($"match:{match_id}");
            if (value.HasValue)
            {
                Log.Information($"Load match_id:{match_id}, StartPlay Reply {(string)value}");
                StartPlayReply reply = JsonParser.Default.Parse<StartPlayReply>(value);
                return (true, reply);
            }
            return (false, null);
        }

        public static async Task RemoveMatch(long match_id)
        {
            Log.Information($"RemoveMatch {match_id}");
            await Cache.Instance.GetDatabase().KeyDeleteAsync($"match:{match_id}");
        }
    }
}
