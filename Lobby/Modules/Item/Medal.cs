using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public class Medal
    {
        public static int CalcMedal(Models.User user, Session session, bool IsDraw, ServerCommon.PlayerResult player, bool is_character_rank_level_up, JGameModeData game_mode, ref int win_medal, ref int lose_medal, ref int draw_medal, ref int mvp_medal, ref int rankup_medal)
        {
            var reason = $"A_PLAY_{game_mode.LogName}";
            string sub_reason = "";

            int medal_count = 0;
            // 메달 수량 체크
            // 승패
            if(IsDraw)
            {
                medal_count += game_mode.RewardDrawMedal;
                sub_reason = "3";
            }
            else if (player.IsWin)
            {
                medal_count += game_mode.RewardWinMedal;
                sub_reason = "1";
            }
            else if (player.IsLose)
            {
                medal_count += game_mode.RewardLoseMedal;
                sub_reason = "2";
           }

            // 메달 획득 허용치 체크 및 조정
            medal_count = GetAvailableMedalCount(user, session, medal_count);
            _ = LogProxy.writeResourceLog(session, "n", ((int)GameItemId.Medal).ToString(), medal_count, 0, user.medal+ medal_count, "add", reason, sub_reason).ConfigureAwait(false);

            if (IsDraw)
            {
                draw_medal += medal_count;
            }
            else if (player.IsWin)
            {
                win_medal += medal_count;
            }
            else if (player.IsLose)
            {
                lose_medal += medal_count;
            }

            // MVP
            if (player.IsMvp)
            {
                medal_count += game_mode.RewardMVPMedal;
                mvp_medal += game_mode.RewardMVPMedal;
                _ = LogProxy.writeResourceLog(session, "n", ((int)GameItemId.Medal).ToString(), game_mode.RewardMVPMedal, 0, user.medal + medal_count, "add", reason, "4").ConfigureAwait(false);
            }
            // 케릭터 랭크 레벨업
            if (is_character_rank_level_up)
            {
                medal_count += game_mode.RewardRankupMedal;
                rankup_medal += game_mode.RewardRankupMedal;
                reason = $"A_PLAY_RANKUP";
                _ = LogProxy.writeResourceLog(session, "n", ((int)GameItemId.Medal).ToString(), game_mode.RewardRankupMedal, 0, user.medal + medal_count, "add", reason, "").ConfigureAwait(false);
            }

            return medal_count;
        }

        public static int GetAvailableMedalCount(Models.User user, Session session, int medal_count)
        {
            (user.medal_charge, user.medal_charge_time) = core.MathHelpers.GetMedalCharge(user.medal_charge, user.medal_charge_time, DateTime.UtcNow);
            if (user.medal_charge <= medal_count)
            {
                medal_count = user.medal_charge;
                user.medal_charge = 0;
            }
            else
            {
                user.medal_charge -= medal_count;
            }

            return medal_count;
        }
    }
}
