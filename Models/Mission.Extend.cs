using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Lobby.Models
{
    public partial class Mission : DirtyUpdate, IBaseModel
    {
        public System.Collections.IEnumerable Missions()
        {
            yield return (mission_id_1, mission_progress_1, mission_reward_1, 0);
            yield return (mission_id_2, mission_progress_2, mission_reward_2, 1);
            yield return (mission_id_3, mission_progress_3, mission_reward_3, 2);
            yield return (mission_id_4, mission_progress_4, mission_reward_4, 3);
            yield return (mission_id_5, mission_progress_5, mission_reward_5, 4);
        }

        public void SetMissionId(int index, int mission_id)
        {
            switch (index)
            {
                case 0: mission_id_1 = mission_id; break;
                case 1: mission_id_2 = mission_id; break;
                case 2: mission_id_3 = mission_id; break;
                case 3: mission_id_4 = mission_id; break;
                case 4: mission_id_5 = mission_id; break;
            }
        }
        public void IncreProgress(int index, int mission_progress, int max_mission_progress)
        {
            switch (index)
            {
                case 0: mission_progress_1 += mission_progress; if (mission_progress_1 > max_mission_progress) mission_progress_1 = max_mission_progress; break;
                case 1: mission_progress_2 += mission_progress; if (mission_progress_2 > max_mission_progress) mission_progress_2 = max_mission_progress; break;
                case 2: mission_progress_3 += mission_progress; if (mission_progress_3 > max_mission_progress) mission_progress_3 = max_mission_progress; break;
                case 3: mission_progress_4 += mission_progress; if (mission_progress_4 > max_mission_progress) mission_progress_4 = max_mission_progress; break;
                case 4: mission_progress_5 += mission_progress; if (mission_progress_5 > max_mission_progress) mission_progress_5 = max_mission_progress; break;
            }
        }

        public void SetProgress(int index, int mission_progress)
        {
            switch (index)
            {
                case 0: mission_progress_1 = mission_progress; break;
                case 1: mission_progress_2 = mission_progress; break;
                case 2: mission_progress_3 = mission_progress; break;
                case 3: mission_progress_4 = mission_progress; break;
                case 4: mission_progress_5 = mission_progress; break;
            }
        }
        public void SetReward(int index, bool reward)
        {
            switch (index)
            {
                case 0: mission_reward_1 = reward; break;
                case 1: mission_reward_2 = reward; break;
                case 2: mission_reward_3 = reward; break;
                case 3: mission_reward_4 = reward; break;
                case 4: mission_reward_5 = reward; break;
            }
        }

        public void Copy(Mission m)
        {
            mission_base_id = m.mission_base_id;
            occ_time = m.occ_time;
            mission_reward = m.mission_reward;

            mission_base_id = m.mission_base_id;
            occ_time = m.occ_time;
            mission_reward = m.mission_reward;

            mission_id_1 = m.mission_id_1;
            mission_progress_1 = m.mission_progress_1;
            mission_reward_1 = m.mission_reward_1;

            mission_id_2 = m.mission_id_2;
            mission_progress_2 = m.mission_progress_2;
            mission_reward_2 = m.mission_reward_2;

            mission_id_3 = m.mission_id_3;
            mission_progress_3 = m.mission_progress_3;
            mission_reward_3 = m.mission_reward_3;

            mission_id_4 = m.mission_id_4;
            mission_progress_4 = m.mission_progress_4;
            mission_reward_4 = m.mission_reward_4;

            mission_id_5 = m.mission_id_5;
            mission_progress_5 = m.mission_progress_5;
            mission_reward_5 = m.mission_reward_5;
        }

        public void Clear()
        {
            this.Copy(new Mission());
        }

        public long GetUserNo()
        {
            return user_no;
        }

        public int GetKey()
        {
            return mission_base_id;
        }

        public long GetValue()
        {
            return mission_no;
        }

        public void SetUpdater(Func<Task> func)
        {
            updater = func;
        }

        public void SetDirty(bool isDirty)
        {
        }
    }
}
