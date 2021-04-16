using core;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GmTool.Modules.LeaderBoard
{
    public class LeaderBoardScheduler
    {

        public static async Task Run()
        {
            DateTime currentTime = DateTime.UtcNow;
            using (var context = new GmTool.Models.CommonContext())
            {
                var leaderboards = await context.leader_board.AsNoTracking().ToListAsync();
                foreach(var leaderboard in leaderboards)
                {
                    // 아직 시작되지 않은 리더보드
                    if (currentTime < leaderboard.submit_time)
                        continue;

                    int diff = 0;
                    switch(leaderboard.season_period)
                    {
                        case Lobby.SeasonPeriod.Hourly:
                            diff = (int)Math.Abs((currentTime - leaderboard.submit_time).TotalHours);
                            break;
                        case Lobby.SeasonPeriod.Daily:
                            diff = (int)Math.Abs((currentTime - leaderboard.submit_time).TotalDays);
                            break;
                        case Lobby.SeasonPeriod.Weekly:
                            diff = (int)currentTime.WeekDifference(leaderboard.submit_time);
                            break;
                        case Lobby.SeasonPeriod.Monthly:
                            diff = (int)currentTime.MonthDifference(leaderboard.submit_time);
                            break;
                        default:
                            continue;
                    }

                    var current_season_no = diff + 1;
                    var season_no = await Cache.Instance.GetDatabase().StringGetAsync($"{leaderboard.leader_board_name}:season_no");
                    if (season_no.HasValue == false)
                    {
                        await Cache.Instance.GetDatabase().StringSetAsync($"{leaderboard.leader_board_name}:season_no", current_season_no);
                    }
                    else
                    {
                        // 기존 시즌 넘버와 현재 시즌 넘버가 다르면 다음 시즌으로 갱신 및 시즌 보상
                        if(current_season_no != season_no)
                        {
                            var rewards = await context.leader_board_reward.AsNoTracking().Where(x => x.leader_board_no == leaderboard.leader_board_no).ToListAsync();
                            foreach (var reward in rewards)
                            {
                                Log.Information($"reward  item_id : {reward.item_id}, item_count : {reward.item_count}");
                            }

                            await Cache.Instance.GetDatabase().StringSetAsync($"{leaderboard.leader_board_name}:season_no", current_season_no);
                        }
                    }
                }

            }
        }
    }
}
