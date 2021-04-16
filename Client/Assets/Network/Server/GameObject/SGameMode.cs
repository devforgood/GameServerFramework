using core;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
using Newtonsoft.Json;
#endif
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    public class SGameMode : GameMode
    {
        public new static NetGameObject StaticCreate(byte worldId) { return NetworkManagerServer.sInstance.RegisterAndReturn(new SGameMode(), worldId); }

        //public override void HandleDying()
        //{

        //    NetworkManagerServer.sInstance.UnregisterGameObject(this);
        //}

        //public override bool HandleCollisionWithActor(Actor inActor)
        //{

        //    return false;
        //}

        //public override void Update()
        //{
        //}

        protected SGameMode()
        {
        }
        public override IGameMode CreateGameMode(GameModeType mode)
        {
            switch (mode)
            {
                case core.GameModeType.BaseStruggle:
                    return new core.BaseStruggle();
                case core.GameModeType.FreeForAll:
                    return new core.FreeForAll();
                case core.GameModeType.KillTheKing:
                    return new SKillTheKing();
                case core.GameModeType.TeamDeathmatch:
                    return new core.TeamDeathmatch();
            }
            return null;
        }

        protected override void Dirty(uint state)
        {
            LogHelper.LogInfo($"dirty game mode {GetNetworkId()}");

            NetworkManagerServer.sInstance.SetStateDirty(GetNetworkId(), WorldId, state);
        }
        protected override void DirtyExcept(int playerId, uint state)
        {
            LogHelper.LogInfo($"dirty except game mode {GetNetworkId()}");

            NetworkManagerServer.sInstance.SetStateDirtyExcept(playerId, GetNetworkId(), WorldId, state);
        }

        public override void Update()
        {
            base.Update();

            if(state == GameModeState.Init && is_timeout_init==false && InitTime + InitTimeout < Timing.sInstance.GetFrameStartTime())
            {
                is_timeout_init = true;
                TimeoutInit();
            }
            else if(state == GameModeState.Ready && ReadyTime + ReadyTimeout < Timing.sInstance.GetFrameStartTime())
            {
                Log.Information($"GameMode Game Ready Timeout {GetNetworkId()}, timestamp:{Timing.sInstance.GetFrameStartTime()}, ReadyTime:{ReadyTime}, ReadyTimeout:{ReadyTimeout}");
                StartPlay(World.Instance(WorldId).playerList.Where(x=>((SActor)x.Value).IsReady==true).Select(x=>x.Key).ToList());
            }
            else if (state == GameModeState.Play && (RemainTime > 0) == false)
            {
                game_mode.OnClose(WorldId, this, CloseType.Timeout);
            }
        }

        protected override void EndGameServerside(Team winTeam, bool isDraw, long matchId, CloseType closeType)
        {
            try
            {
                ServerCommon.GameResult gameResult = new ServerCommon.GameResult();
                gameResult.player_result = new Dictionary<string, ServerCommon.PlayerResult>();

                gameResult.win_team = (int)winTeam;
                gameResult.is_draw = isDraw;
                gameResult.match_id = matchId;
                gameResult.statistics = statistics;
                gameResult.statistics.map_id = mMapData.ID;
                gameResult.statistics.leave_player = GetEntries().Where(x => x.Leave).Count();
                gameResult.statistics.clear = closeType == CloseType.Clear ? 1 : 0;
                gameResult.statistics.play_time = (int)(Timing.sInstance.GetFrameStartTime() - StartTime);
                gameResult.statistics.start_time = StartEpochTime;
                gameResult.statistics.end_time = DateTime.UtcNow.ToEpochTime();

                foreach (var player in GetEntries())
                {
                    var playerResult = new ServerCommon.PlayerResult();
                    playerResult.play_point = player.GetScore();
                    if (isDraw)
                    {
                        playerResult.battle_point = mGameModeData.RewardBattleScoreDraw;
                    }
                    else if (player.GetTeam() == winTeam)
                    {
                        playerResult.IsWin = true;
                        playerResult.battle_point = mGameModeData.RewardBattleScoreWin;
                        player.Missions.Increment((int)MissionType.Mission_Victory, 1);
                    }
                    else
                    {
                        playerResult.IsLose = true;
                        playerResult.battle_point = mGameModeData.RewardBattleScoreLose;
                    }

                    if (player.GetPlayerId() == MVPPlayerId)
                    {
                        playerResult.IsMvp = true;
                        playerResult.battle_point += mGameModeData.RewardBattleScoreMvp;
                    }

                    if (player.Pause)
                    {
                        playerResult.battle_point = mGameModeData.AbuseBattleScore;
                    }
                    else
                    {
                        // 어뷰징이 아닌 유저만 나감 처리
                        playerResult.IsLeave = player.Leave;
                    }

                    playerResult.missions = player.Missions;

                    gameResult.player_result.Add(player.GetSessionId(), playerResult);
                }

                if (channel_id != "")
                {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else

                    string key = $"game_result:{channel_id}";
                    Cache.sInstance.cache.GetSubscriber().PublishAsync(key, JsonConvert.SerializeObject(gameResult)).ConfigureAwait(false);
                    Log.Information($"PubGameRessult {key}");
#endif
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
            }
        }
        public override void OnTrigger(int playerId, PlayPointID id)
        {
            int addPoint = 0;
            var info = ACDC.PlayPointData[(int)id];
            if (info != null)
                addPoint = info.Point;

            var player = mEntries.Where(x => x.GetPlayerId() == playerId).FirstOrDefault();
            if(player==null || player == default(Entry))
            {
                return;
            }
            player.SetScore(player.GetScore() + addPoint);

            if(id == PlayPointID.EnemyKill || id == PlayPointID.KillTheKing)
            {
                ++player.KillCount;
            }

            game_mode.OnTrigger(WorldId, playerId, id);

            LogHelper.LogInfo($"PlayPoint cur:{player.GetScore()}, add:{addPoint}");
        }

        public string channel_id;
        bool is_timeout_init = false;

    }
}
