using GameService;
using Google.Protobuf.Collections;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lobby
{
    public enum MissionUserAction
    {
        None,
        GetItem,
    }

    public class MissionManager
    {
        /// <summary>
        /// 미션 정보 얻기
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public static async Task<UserMissions> GetMissionsLock(Session session)
        {
            var userMissions = new UserMissions();
            List<Models.Mission> missions;

            missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
            if (await Refresh(missions, session, true))
            {
                await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
                {
                    missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
                    await Refresh(missions, session, false);
                }
            }

            if (missions.Count > 0)
            {
                foreach (var mission in missions)
                {
                    var mission_base = new MissionBaseInfo();
                    foreach (var m in mission.Missions())
                    {
                        mission_base.Add(((ValueTuple<int, int, bool, int>)m).Item1, ((ValueTuple<int, int, bool, int>)m).Item2, ((ValueTuple<int, int, bool, int>)m).Item3);
                    }

                    mission_base.MissionBaseId = mission.mission_base_id;
                    mission_base.MissionBaseReward = mission.mission_reward;

                    userMissions.MissionBaseList.Add(mission_base);
                }
            }

            return userMissions;
        }

        /// <summary>
        /// 기획 테이블 기반 초기 데이터 및 날짜로 인한 데이터 정리
        /// </summary>
        /// <param name="missions"></param>
        /// <param name="user_no"></param>
        /// <param name="is_only_check"></param>
        /// <returns></returns>
        public static async Task<bool> Refresh(List<Models.Mission> missions, Session session, bool is_only_check)
        {
            // 유효하지 않은 미션 리셋
            foreach (var mission_base_data in ACDC.Mission_BaseData)
            {
                if (mission_base_data.Value.Enable == false)
                    continue;

                var db_mission_base = missions.Where(x => x.mission_base_id == mission_base_data.Value.id).FirstOrDefault();
                if (db_mission_base != null && db_mission_base != default(Models.Mission))
                {
                    if (db_mission_base.IsAvailable() == false)
                    {
                        if (is_only_check)
                        {
                            return true;
                        }
                        else
                        {
                            db_mission_base.Refresh(mission_base_data.Value.id, session.user_no);
                            await MissionCache.Instance.UpdateEntity(session.member_no, db_mission_base);
                        }
                    }
                }
                else
                {
                    if (is_only_check)
                    {
                        return true;
                    }
                    else
                    {
                        db_mission_base = new Models.Mission();
                        db_mission_base.Refresh(mission_base_data.Value.id, session.user_no);
                        missions.Add(db_mission_base);

                        await MissionCache.Instance.InsertEntity(session.member_no, db_mission_base);
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 미션 진행 (다중)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="mission_progress"></param>
        /// <returns></returns>
        public static async Task Progress(Session session, Dictionary<int, int> mission_progress)
        {
            var missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
            await Refresh(missions, session, false);
            foreach (var mission in missions)
            {
                bool is_update = false;
                foreach (var progress in mission_progress)
                {
                    if(mission.Progress(progress.Key, progress.Value))
                    {
                        is_update = true;
                    }
                }
                if (is_update)
                {
                    await MissionCache.Instance.UpdateEntity(session.member_no, mission);
                }
            }
        }

        /// <summary>
        /// 미션 진행 (단일)
        /// </summary>
        /// <param name="session"></param>
        /// <param name="mission_id"></param>
        /// <param name="mission_progress"></param>
        /// <returns></returns>
        public static async Task Progress(Session session, int mission_id, int mission_progress)
        {
            var missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
            await Refresh(missions, session, false);
            foreach (var mission in missions)
            {
                if (mission.Progress(mission_id, mission_progress))
                {
                    await MissionCache.Instance.UpdateEntity(session.member_no, mission);
                }
            }
        }

        public static async Task ProgressAlter(Session session, int mission_id, int mission_progress)
        {
            var missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
            await Refresh(missions, session, false);
            foreach (var mission in missions)
            {
                if (mission.ProgressAlter(mission_id, mission_progress))
                {
                    await MissionCache.Instance.UpdateEntity(session.member_no, mission);
                }
            }
        }

        /// <summary>
        /// 로비에서 발생하는 미션 이벤트 트리거
        /// </summary>
        /// <param name="session"></param>
        /// <param name="action"></param>
        /// <param name="param1"></param>
        /// <param name="param2"></param>
        /// <returns></returns>
        public static async Task OnTrigger(Session session, MissionUserAction action, int param1, int param2)
        {
            switch (action)
            {
                case MissionUserAction.GetItem:
                    {
                        JMission_infoData mission = null;
                        ACDC.Mission_infoData.TryGetValue((int)MissionType.Mission_OpenBox_N, out mission);
                        if (mission != null && mission.Mission_TargetId == param1)
                        {
                            await Progress(session, (int)MissionType.Mission_OpenBox_N, param2);
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// 각 미션 보상
        /// </summary>
        /// <param name="session"></param>
        /// <param name="mission_id"></param>
        /// <returns></returns>
        public static async Task<ErrorCode> Reward(Session session, int mission_id)
        {
            Log.Error($"Mission Reward {mission_id}, {session.user_name}");
            var mission_info_data = ACDC.Mission_infoData[mission_id];
            if (mission_info_data == null || mission_info_data == default(JMission_infoData))
            {
                return ErrorCode.WrongParam;
            }
            List<Models.Mission> missions;
            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                missions = await MissionCache.Instance.GetEntities(session.member_no, session.user_no, true);
                await Refresh(missions, session, false);

                if (missions.Count > 0)
                {
                    foreach (var mission in missions)
                    {
                        foreach (var m in mission.Missions())
                        {
                            // 동일 미션 찾기
                            if (((ValueTuple<int, int, bool, int>)m).Item1 == mission_id)
                            {
                                // 조건 달성 체크
                                if (((ValueTuple<int, int, bool, int>)m).Item2 < mission_info_data.Mission_Value)
                                {
                                    return ErrorCode.NotEnough;
                                }

                                // 보상을 이미 받았는지 체크
                                if (((ValueTuple<int, int, bool, int>)m).Item3 == true)
                                {
                                    return ErrorCode.AlreadyReward;
                                }

                                // 보상 지급 완료 상태 저장
                                mission.SetReward(((ValueTuple<int, int, bool, int>)m).Item4, true);
                                await MissionCache.Instance.UpdateEntity(session.member_no, mission);

                                await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                                await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                                {
                                    await Inventory.Insert(session, user, character, mission_info_data.Reward_Id, mission_info_data.Reward_Value, new LogReason("A_MISSION", mission_info_data.MissionType.ToString()));
                                }

                                return ErrorCode.Success;
                            }
                        }
                    }
                }
            }

            return ErrorCode.WrongParam;
        }

        /// <summary>
        /// 미션 그룹(or Base 예 일일, 주간) 보상. 
        /// </summary>
        /// <param name="session"></param>
        /// <param name="mission_base_id"></param>
        /// <returns></returns>
        public static async Task<GetRewardMissionBaseReply> RewardBase(Session session, int mission_base_id)
        {
            ItemList itemList = new ItemList();
            Goods AccountGoods = new Goods();

            Log.Information($"Mission RewardBase {mission_base_id}, {session.user_name}");
            var mission_base_data = ACDC.Mission_BaseData[mission_base_id];
            if (mission_base_data == null || mission_base_data == default(JMission_BaseData))
            {
                return new GetRewardMissionBaseReply() { Code = ErrorCode.WrongParam };
            }

            await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{session.session_id}"))
            {
                await using (var mission = await MissionCache.Instance.GetEntity(session.member_no, session.user_no, mission_base_id, true, true, false))
                {
                    // 해당 미션이 리셋 되었는지 확인
                    if(mission.IsAvailable()==false)
                    {
                        return new GetRewardMissionBaseReply() { Code = ErrorCode.NotEnough };
                    }

                    // 이미 보상을 받았는지 확인
                    if(mission.mission_reward == true)
                    {
                        return new GetRewardMissionBaseReply() { Code = ErrorCode.AlreadyReward };
                    }

                    // 보상 조건을 달성 했는지 확인
                    if(mission.CheckConditionReward() == false)
                    {
                        return new GetRewardMissionBaseReply() { Code = ErrorCode.NotEnough };
                    }

                    // 보상 지급
                    await using (var user = await UserCache.GetUser(session.member_no, session.user_no, true, true, false))
                    await using (var character = await CharacterCache.Instance.GetEntity(session.member_no, session.character_no, true, true, false))
                    {
                        await Inventory.Insert(session, user, character, mission_base_data.reward_item_id, 1, new LogReason("A_MISSION", mission_base_data.id.ToString()), itemList);
                        AccountGoods.Set(user);
                    }

                    // 보상 받았음 기록
                    mission.mission_reward = true;
                    mission.IsDirty = true;
                }
            }
            return new GetRewardMissionBaseReply() { Code = ErrorCode.Success, Item = itemList, AccountGoods = AccountGoods };
        }
    }
}
