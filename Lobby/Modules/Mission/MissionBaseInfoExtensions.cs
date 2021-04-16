using GameService;
using System;
using System.Collections.Generic;
using System.Text;

namespace Lobby
{
    public static partial class MissionBaseInfoExtensions
    {
        /// <summary>
        /// 각 미션 정보를 리턴 메시지에 담기
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="mission_id"></param>
        /// <param name="mission_progress"></param>
        /// <param name="mission_reward"></param>
        public static void Add(this MissionBaseInfo mission, int mission_id, int mission_progress, bool mission_reward)
        {
            if (mission_id != 0)
            {
                mission.Missions.Add(new MissionInfo()
                {
                    MissionId = mission_id,
                    MissionProgress= mission_progress,
                    MissionReward = mission_reward,
                });
            }
        }
    }
}
