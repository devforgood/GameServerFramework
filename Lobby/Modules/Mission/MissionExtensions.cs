using System;
using System.Collections.Generic;
using System.Linq;

namespace Lobby
{
    public static partial class MissionExtensions
    {
        /// <summary>
        /// 현재 미션이 유효한지 확인
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        public static bool IsAvailable(this Models.Mission mission)
        {
            if (mission.mission_base_id == (int)MissionBase.Daily)
            {
                if (DateTime.UtcNow.Date != mission.occ_time.Date)
                {
                    return false;
                }
            }
            else if (mission.mission_base_id == (int)MissionBase.Weekly)
            {
                if (core.MathHelpers.weekDiff(DateTime.UtcNow, mission.occ_time) != 0)
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 미션 보상을 받을 수 있는지 여부 확인
        /// </summary>
        /// <param name="mission"></param>
        /// <returns></returns>
        public static bool CheckConditionReward(this Models.Mission mission)
        {
            foreach (var m in mission.Missions())
            {
                if (((ValueTuple<int, int, bool, int>)m).Item1 == 0)
                {
                    break;
                }
                var mission_id = ((ValueTuple<int, int, bool, int>)m).Item1;
                var mission_progress = ((ValueTuple<int, int, bool, int>)m).Item2;

                var mission_info_data = ACDC.Mission_infoData[mission_id];
                if (mission_info_data == null || mission_info_data == default(JMission_infoData))
                {
                    break;
                }

                if(mission_progress < mission_info_data.Mission_Value)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 미션 갱신
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="mission_base_id"></param>
        /// <param name="user_no"></param>
        public static void Refresh(this Models.Mission mission, int mission_base_id, long user_no)
        {
            var mission_base = ACDC.Mission_BaseData[mission_base_id];

            mission.Clear();
            mission.mission_base_id = mission_base_id;
            mission.occ_time = DateTime.UtcNow;
            mission.mission_reward = false;
            mission.user_no = user_no;

            var mission_id_list = ACDC.Mission_infoData.Where(x =>x.Value.group_id == mission_base.group_id).Select(x=>x.Key).ToList();
            for(int i=0;i< mission_base.count;++i)
            {

                var rand_value = core.ThreadSafeRandom.Get().Next(0, mission_id_list.Count);

                mission.SetMissionId(i, mission_id_list[rand_value]);

                mission_id_list.RemoveAt(rand_value);

                if (mission_id_list.Count <= 0)
                    break;
            }
        }

        /// <summary>
        /// 미션 진행
        /// </summary>
        /// <param name="mission"></param>
        /// <param name="mission_id"></param>
        /// <param name="mission_progress"></param>
        /// <returns></returns>
        public static bool Progress(this Models.Mission mission, int mission_id, int mission_progress)
        {
            foreach(var m in mission.Missions())
            {
                if (((ValueTuple<int, int, bool, int>)m).Item1 == 0)
                {
                    break;
                }
                else if (((ValueTuple<int, int, bool, int>)m).Item1 == mission_id)
                {
                    mission.IncreProgress(((ValueTuple<int, int, bool, int>)m).Item4, mission_progress, ACDC.Mission_infoData[mission_id].Mission_Value);
                    return true;
                }
            }
            return false;
        }

        public static bool ProgressAlter(this Models.Mission mission, int mission_id, int mission_progress)
        {
            foreach (var m in mission.Missions())
            {
                if (((ValueTuple<int, int, bool, int>)m).Item1 == 0)
                {
                    break;
                }
                else if (((ValueTuple<int, int, bool, int>)m).Item1 == mission_id)
                {
                    mission.SetProgress(((ValueTuple<int, int, bool, int>)m).Item4, mission_progress);
                    return true;
                }
            }
            return false;
        }
    }
}
