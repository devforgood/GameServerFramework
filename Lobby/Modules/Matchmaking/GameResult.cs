using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Lobby
{
    public class GameResult
    {
        private static TimeSpan game_result_expire = new TimeSpan(0, 10, 0);

        /// <summary>
        /// 게임 결과 기다림
        /// </summary>
        /// <param name="session"></param>
        /// <param name="request"></param>
        /// <param name="responseStream"></param>
        /// <returns></returns>
        public static async Task WaitGameResult(Session session, string channel_id, JMapData map_data, JGameModeData game_mode)
        {
            string key = $"game_result:{channel_id}";
            Log.Information($"WaitGameResult {key}");

            Interlocked.Increment(ref RankMatchmaking.MatchCount);

            var queue = Cache.Instance.GetSubscriber().Subscribe(key);

            var cts = new CancellationTokenSource();
            cts.CancelAfter((int)game_result_expire.TotalMilliseconds);
            try
            {
                var ret = await queue.ReadAsync(cts.Token);

                Interlocked.Decrement(ref RankMatchmaking.MatchCount);

                ServerCommon.GameResult reply = JsonConvert.DeserializeObject<ServerCommon.GameResult>(ret.Message);

                Log.Information($"game result {ret.Message.ToString()}");

                int total_win_medal = 0;
                int total_lose_medal = 0;
                int total_draw_medal = 0;
                int total_mvp_medal = 0;
                int total_rankup_medal = 0;
                await Match.RemoveMatch(reply.match_id);

                var characterNames = new List<string>();
                var characterLevels = new List<int>();


                foreach (var player in reply.player_result)
                {
                    if (player.Value.IsLeave)
                        continue;

                    var last_session = await Session.GetSession(player.Key, false);
                    if (last_session != null)
                    {
                        bool is_changed_character_rank_level = false;
                        await using (var mylock = await RedLock.CreateLockAsync($"lock:session:{last_session.session_id}"))
                        {
                            await using (var user = await UserCache.GetUser(last_session.member_no, last_session.user_no, true, true, true))
                            await using (var character = await CharacterCache.Instance.GetEntity(last_session.member_no, last_session.character_no, true, true, true))
                            {
                                user.play_point += player.Value.play_point;
                                var last_account_battle_score = user.battle_score;
                                UserGrowth.IncreaseBattleScore(user, player.Value.battle_point);
                                var last_battle_score = character.battle_score;
                                var last_rank_level = character.rank_level;
                                CharacterGrowth.IncreaseBattleScore(character, player.Value.battle_point);

                                var (abs, cbs, wc) = await Ranking.Update(last_session, reply.season_no, player.Value.battle_point, player.Value.IsWin);

                                bool is_character_rank_level_up = false;
                                if (last_rank_level != character.rank_level)
                                {
                                    if (last_rank_level < character.rank_level)
                                        is_character_rank_level_up = true;
                                }

                                if(last_battle_score != character.battle_score)
                                {
                                    var rankData = ACDC.RankData.GetRank(character.battle_score);
                                    if (rankData != null)
                                    {
                                        if(last_session.rank != rankData.Rank)
                                        {
                                            is_changed_character_rank_level = true;
                                            last_session.rank = rankData.Rank;
                                        }
                                    }
                                }

                                var last_medal = user.medal;
                                var medal_count = Medal.CalcMedal(user, last_session, reply.is_draw, player.Value, is_character_rank_level_up, game_mode, ref total_win_medal, ref total_lose_medal, ref total_draw_medal, ref total_mvp_medal, ref total_rankup_medal);
                                await Inventory.Insert(last_session, user, character, (int)GameItemId.Medal, medal_count, null);


                                // 배틀에서 넘어온 미션 데이터가 없는 경우 초기화
                                if (player.Value.missions == null)
                                {
                                    player.Value.missions = new Dictionary<int, int>();
                                }
                                player.Value.missions.Increment((int)MissionType.Mission_GetMedal, medal_count);

                                await MissionManager.Progress(last_session, player.Value.missions);

                                History.Info(last_session.member_no, last_session.user_no, last_session.character_no, HistoryLogAction.GainAccountBattleScore, (byte)HistoryLogReason.GameResultReward, (int)user.battle_score, (int)last_account_battle_score, reply.match_id.ToString(), "");
                                History.Info(last_session.member_no, last_session.user_no, last_session.character_no, HistoryLogAction.GainCharacterBattleScore, (byte)HistoryLogReason.GameResultReward, (int)character.battle_score, (int)last_battle_score, reply.match_id.ToString(), "");

                                characterNames.Add(last_session.user_name);
                                characterLevels.Add(character.rank_level);
                            }
                        }

                        //Log.Information($"session:{last_session.session_id}, rank:{last_session.rank}");
                        if (is_changed_character_rank_level)
                        {
                            await Session.UpdateSessionLock(last_session.session_id, delegate (Session s) { s.rank = last_session.rank; });
                        }


                        // 게임 종료 이후 매칭 정보 삭제
                        await MatchUser.RemoveMatchUser(last_session.user_no);
                    }
                    else
                    {
                        Log.Warning($"game result lost session:{player.Key}, msg:{ret.Message}");
                    }
                }

                MatchLog.Info(reply, total_win_medal, total_lose_medal, total_draw_medal, total_mvp_medal, total_rankup_medal);

                string resultTp;
                if (reply.is_draw)
                {
                    resultTp = "26";
                    _ = LogProxy.writeActionLog(session, "플레이", "무승부", map_data.ID.ToString()).ConfigureAwait(false);
                }
                else if (reply.statistics.clear == 1)
                {
                    resultTp = "21";
                    _ = LogProxy.writeActionLog(session, "플레이", "클리어", map_data.ID.ToString()).ConfigureAwait(false);
                }
                else
                {
                    resultTp = "22";
                }

                _ = LogProxy.writeRoundLog(session, game_mode.Name, "", "", resultTp, 0, reply.statistics.start_time, reply.statistics.end_time, characterNames, characterLevels, "").ConfigureAwait(false);


                if(reply.statistics.leave_player > 0)
                    _ = LogProxy.writeActionLog(session, "플레이", "이탈", map_data.ID.ToString(), reply.statistics.leave_player.ToString()).ConfigureAwait(false);

                if(reply.statistics.normal_item > 0)
                    _ = LogProxy.writeActionLog(session, "플레이", "일반아이템획득", map_data.ID.ToString(), reply.statistics.normal_item.ToString()).ConfigureAwait(false);

                if(reply.statistics.tactic_item > 0)
                    _ = LogProxy.writeActionLog(session, "플레이", "전략아이템획득", map_data.ID.ToString(), reply.statistics.tactic_item.ToString()).ConfigureAwait(false);


            }
            catch (OperationCanceledException ex)
            {
                Log.Information($"game result timeout {ex.ToString()}");
            }
        }
    }
}
