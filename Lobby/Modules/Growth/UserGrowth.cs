using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public class UserGrowth
    {
        public static void IncreaseBattleScore(Models.User user, int battle_score)
        {
            int last_account_battle_score;
            //bool is_grade_up = false;

            last_account_battle_score = user.battle_score;
            if (battle_score < 0)
            {
                user.battle_score += battle_score;
                if (user.battle_score < 0)
                    user.battle_score = 0;
            }
            else
            {
                user.battle_score += battle_score;
            }

            var grade_data = ACDC.GradeData.GetGrade(user.battle_score);
            if (grade_data != null)
            {
                if (user.user_grade < grade_data.ID)
                {
                    user.user_grade = grade_data.ID;
                    //is_grade_up = true;
                }
            }
        }
    }
}
