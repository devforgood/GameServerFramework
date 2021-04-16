using core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using ServerCommon;

namespace Server
{
    class SKillTheKing : KillTheKing
    {
        public SKillTheKing()
        {
        }

        KillTheKingInfo KillTheKingInfo;

        public override void OnTrigger(byte worldId, int playerId, PlayPointID id)
        {
            if(IsKing(playerId))
            {
                if (id == PlayPointID.PlayerDeath)
                {
                    GameMode gameMode = World.Instance(worldId).GameMode;
                    Team team = gameMode.GetEntry(playerId).GetTeam();
                    if (kingPlayerList[team].Count >= MaxKillCount)
                    {
                        lastKilledKingPlayerId = playerId;
                        switch (team)
                        {
                            case Team.TeamA:
                                gameMode.EndGame(Team.TeamB, Team.TeamA, false, CloseType.Clear);
                                break;
                            case Team.TeamB:
                                gameMode.EndGame(Team.TeamA, Team.TeamB, false, CloseType.Clear);
                                break;
                        }
                        return;
                    }
                    int nextKing = playerId;
                    var players = World.Instance(worldId).playerList;
                    List<Entry> entries = gameMode.GetEntries().Where(x =>
                       x.GetPlayerId() != playerId &&
                       x.GetTeam() == team &&
                       !kingPlayerList[team].Contains(x.GetPlayerId())).ToList();

                    if (entries.Count == 0)
                        entries = gameMode.GetEntries().Where(x => x.GetTeam() == team).ToList();

                    nextKing = GetNextKingPlayerId(entries.Select(x => x.GetPlayerId()).ToList());
                    
                    Entry entry = gameMode.GetEntries().Where(x => x.GetPlayerId() == nextKing).FirstOrDefault();

                    gameMode.InvokeClientRpc(gameMode.SwitchKing, players.Keys.ToList(), entry.GetTeam(), playerId, entry.GetPlayerId());
                    SetKing(worldId, team, nextKing);
                }
                else if (id == PlayPointID.PlayerReborn)
                {
                    SetKingSpell(worldId, playerId);
                }
            }
        }

        public override void OnStartPlay(GameMode gameMode)
        {
            SetKing(gameMode.WorldId, Team.TeamA, KillTheKingInfo.king_player_id_A);
            SetKing(gameMode.WorldId, Team.TeamB, KillTheKingInfo.king_player_id_B);
        }

        void SetKing(byte worldId, Team team, int playerId)
        {
            kingPlayerList[team].Add(playerId);
            switch(team)
            {
                case Team.TeamA: kingPlayerId_TeamA = playerId; break;
                case Team.TeamB: kingPlayerId_TeamB = playerId; break;
            }
            SetKingSpell(worldId, playerId);
        }

        public override void SetModeData(object data)
        {
            KillTheKingInfo = ((InternalMessage)data).kill_the_king_info;
        }

        public void SetKingSpell(byte worldId, int playerId)
        {
            Entry entry = World.Instance(worldId).GameMode.GetEntry(playerId);
            if (entry == null)
            {
                LogHelper.LogError($"Can't find entry in SetKingSpell playerId : {playerId}");
                return;
            }

            NetGameObject netGameObject = NetworkManager.Instance.GetGameObject(entry.mNetworkId, worldId);
            if (netGameObject == null || !(netGameObject is SActor))
            {
                LogHelper.LogError($"netGameObject == null or netGameObject is not SActor playerId : {playerId}, worldId : {worldId}");
                return;
            }
            
            SActor actor = netGameObject as SActor;
            var modeData = ACDC.GameModeData[(int)GetMode()];
            for (int i = 0; i < modeData.ModeSpellIDs.Length; ++i)
            {
                actor.AddSpell(ACDC.SpellData[modeData.ModeSpellIDs[i]], 0);
            }

            if(actor.StateServerSide != ActorState.Ghost)
                actor.ResetHealth(actor.GetCharacterHp(), null);
        }

        public override void OnClose(byte worldId, GameMode gameMode, CloseType closeType)
        {
            if (GameMode.EndAble == false)
                return;

            LogHelper.LogInfo($"WorldId{worldId}, BaseStruggle closeType:{closeType} TeamA{World.Instance(worldId).GetCastle(Team.TeamA)?.hp}, TeamB{World.Instance(worldId).GetCastle(Team.TeamB)?.hp}");

            if (kingPlayerList[Team.TeamA].Count < kingPlayerList[Team.TeamB].Count)
            {
                gameMode.EndGame(Team.TeamA, Team.TeamB, false, closeType);
            }
            else if (kingPlayerList[Team.TeamA].Count > kingPlayerList[Team.TeamB].Count)
            {
                gameMode.EndGame(Team.TeamB, Team.TeamA, false, closeType);
            }
            else if (kingPlayerList[Team.TeamA].Count == kingPlayerList[Team.TeamB].Count)
            {
                float teamAHpPercent = GetKingPlayerHpPercent(worldId, kingPlayerId_TeamA);
                float teamBHpPercent = GetKingPlayerHpPercent(worldId, kingPlayerId_TeamB);

                int teamScoreA = 0;
                int teamScoreB = 0;
                if (teamAHpPercent > teamBHpPercent)
                {
                    gameMode.EndGame(Team.TeamA, Team.TeamB, false, closeType);
                }
                else if (teamAHpPercent < teamBHpPercent)
                {
                    gameMode.EndGame(Team.TeamB, Team.TeamA, false, closeType);
                }
                else if ((teamScoreA = gameMode.GetTeamScore(Team.TeamA)) > (teamScoreB = gameMode.GetTeamScore(Team.TeamB)))
                {
                    gameMode.EndGame(Team.TeamA, Team.TeamB, false, closeType);
                }
                else if (teamScoreA < teamScoreB)
                {
                    gameMode.EndGame(Team.TeamB, Team.TeamA, false, closeType);
                }
                else
                {
                    gameMode.EndGame(Team.TeamA, Team.TeamA, true, closeType);
                }
            }
            else
            {
                gameMode.EndGame(Team.TeamA, Team.TeamA, true, closeType);
            }
        }

        float GetKingPlayerHpPercent(byte worldId, int playerId)
        {
            float ret = 0;
            var actor = World.Instance(worldId).GameMode.GetActor(playerId);
            if (actor != null)
            {
                ret = actor.GetHealth() / actor.GetCharacterHp();
            }
            return ret;
        }
    }
}
