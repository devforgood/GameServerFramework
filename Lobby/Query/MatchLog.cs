using System;
using System.Threading.Tasks;
using Serilog;

namespace Lobby
{
    public class MatchLog
    {
        public static void Info(ServerCommon.GameResult result, int win_medal, int lose_medal, int draw_medal, int mvp_medal, int rankup_medal)
        {
            using (var context = new Lobby.Models.LogContext(0))
            {
                try
                {
                    context.match_log.Add(new Lobby.Models.MatchLog()
                    {
                        match_id = result.match_id,
                        map_id = result.statistics.map_id,
                        leave_player = result.statistics.leave_player,
                        result = result.is_draw?1:0,
                        clear = result.statistics.clear,

                        fall_death = result.statistics.fall_death,
                        attacked_death = result.statistics.attacked_death,
                        train_death = result.statistics.train_death,
                        other_death = result.statistics.other_death,

                        normal_item = result.statistics.normal_item,
                        tactic_item = result.statistics.tactic_item,

                        play_time = result.statistics.play_time,

                        win_medal = win_medal,
                        lose_medal = lose_medal,
                        draw_medal = draw_medal,
                        mvp_medal = mvp_medal,
                        rankup_medal = rankup_medal,

                        submit_time = DateTime.UtcNow,
                    });

                    context.SaveChangesAsync().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    Log.Error($"{e.ToString()}");
                }
            }
        }
    }
}
