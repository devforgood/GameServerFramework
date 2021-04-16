using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class MatchInstanceId
    {
        public static async Task<long> GetMatchInstanceId()
        {
            return await Cache.Instance.GetDatabase().StringIncrementAsync("match_instance_id");
        }
    }
}
