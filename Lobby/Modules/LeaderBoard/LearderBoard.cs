using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.Modules.LeaderBoard
{
    public class Rank
    {
        public string player_id;
        public long rank;
        public double score;
        public string custom_data;
    };

    public class LearderBoard
    {

        public static async Task NextLeaderBoardSeason(string leader_board_name)
        {
            await Cache.Instance.GetDatabase().StringIncrementAsync($"{leader_board_name}:season_no");
        }

        public static async Task<string> GetLeaderBoardSeasonName(string leader_board_name)
        {
            var season_no = await Cache.Instance.GetDatabase().StringGetAsync($"{ leader_board_name}:season_no");
            if (season_no.HasValue == false)
                return leader_board_name;

            return $"{leader_board_name}:{season_no}";
        }

        public static async Task<double> Accumulate(string leader_board_name, string player_id, double score, string custom_data)
        {
            leader_board_name = await GetLeaderBoardSeasonName(leader_board_name);

            double new_score_of_member = 0;
            if (score < 0)
                new_score_of_member = await Cache.Instance.GetDatabase().SortedSetDecrementAsync(leader_board_name, player_id, score);
            else
                new_score_of_member = await Cache.Instance.GetDatabase().SortedSetIncrementAsync(leader_board_name, player_id, score);

            if (custom_data != string.Empty)
            {
                await Cache.Instance.GetDatabase().StringSetAsync($"{leader_board_name}:custom_data:{player_id}", custom_data);
            }

            return new_score_of_member;
        }

        public static async Task<List<Rank>> GetRankList(string leader_board_name, long skip, long take)
        {
            leader_board_name = await GetLeaderBoardSeasonName(leader_board_name);

            var rank_list = await Cache.Instance.GetDatabase().SortedSetRangeByScoreAsync(leader_board_name, double.NegativeInfinity, double.PositiveInfinity, StackExchange.Redis.Exclude.None
                , StackExchange.Redis.Order.Descending, skip, take);


            var result = new List<Rank>();
            foreach (var player_id in rank_list)
            {
                result.Add(await _GetRank(leader_board_name, player_id));
            }

            return result;
        }
        private static async Task<Rank> _GetRank(string leader_board_name, string player_id)
        {
            var r = new Rank();
            r.player_id = player_id;
            r.score = (double)await Cache.Instance.GetDatabase().SortedSetScoreAsync(leader_board_name, player_id);
            r.rank = (long)await Cache.Instance.GetDatabase().SortedSetRankAsync(leader_board_name, player_id, StackExchange.Redis.Order.Descending);
            r.custom_data = await Cache.Instance.GetDatabase().StringGetAsync($"{leader_board_name}:custom_data:{player_id}");
            return r;
        }

        public static async Task<Rank> GetRank(string leader_board_name, string player_id)
        {
            leader_board_name = await GetLeaderBoardSeasonName(leader_board_name);
            return await _GetRank(leader_board_name, player_id);
        }
    }
}
