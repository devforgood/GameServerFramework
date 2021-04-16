using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace core
{
    // 기지 쟁탈전
    public class BaseStruggle : IGameMode
    {
        public GameModeType GetMode() { return GameModeType.BaseStruggle; }

        public bool Write(NetOutgoingMessage inOutputStream)
        {
            bool didSucceed = true;


            return didSucceed;
        }
        public bool Read(NetIncomingMessage inInputStream)
        {
            bool didSucceed = true;

            return didSucceed;
        }

        public bool WriteGameResult(NetOutgoingMessage inOutputStream, GameMode gameMode)
        {
            bool didSucceed = true;

            inOutputStream.Write((byte)gameMode.WinTeam, GameMode.MaxTeamBits);
            inOutputStream.Write((byte)gameMode.LoseTeam, GameMode.MaxTeamBits);
            inOutputStream.Write(gameMode.IsDraw);
            inOutputStream.Write(gameMode.MVPPlayerId);

            return didSucceed;
        }
        public bool ReadGameResult(NetIncomingMessage inInputStream, GameMode gameMode)
        {
            bool didSucceed = true;

            gameMode.WinTeam = (core.Team)inInputStream.ReadByte(GameMode.MaxTeamBits);
            gameMode.LoseTeam = (core.Team)inInputStream.ReadByte(GameMode.MaxTeamBits);
            gameMode.IsDraw = inInputStream.ReadBoolean();
            gameMode.MVPPlayerId = inInputStream.ReadInt32();

            return didSucceed;
        }

        public bool WriteState(NetOutgoingMessage inOutputStream, Actor actor)
        {
            return true;
        }

        public bool TakableDamage(Entry entryA, Entry entryB)
        {
            return entryA.GetTeam() != entryB.GetTeam();
        }
        public void OnClose(byte worldId, GameMode gameMode, CloseType closeType)
        {
            if (GameMode.EndAble == false)
                return;

            LogHelper.LogInfo($"WorldId{worldId}, BaseStruggle closeType:{closeType} TeamA{World.Instance(worldId).GetCastle(Team.TeamA)?.hp}, TeamB{World.Instance(worldId).GetCastle(Team.TeamB)?.hp}");

            if (World.Instance(worldId).GetCastle(Team.TeamA)?.hp > World.Instance(worldId).GetCastle(Team.TeamB)?.hp)
            {
                World.Instance(worldId).GameMode.EndGame(Team.TeamA, Team.TeamB, false, closeType);
            }
            else if (World.Instance(worldId).GetCastle(Team.TeamA)?.hp < World.Instance(worldId).GetCastle(Team.TeamB)?.hp)
            {
                World.Instance(worldId).GameMode.EndGame(Team.TeamB, Team.TeamA, false, closeType);
            }
            else if (gameMode.GetTeamScore(Team.TeamA) > gameMode.GetTeamScore(Team.TeamB))
            {
                World.Instance(worldId).GameMode.EndGame(Team.TeamA, Team.TeamB, false, closeType);
            }
            else if (gameMode.GetTeamScore(Team.TeamA) < gameMode.GetTeamScore(Team.TeamB))
            {
                World.Instance(worldId).GameMode.EndGame(Team.TeamB, Team.TeamA, false, closeType);
            }
            else
            {
                World.Instance(worldId).GameMode.EndGame(Team.TeamA, Team.TeamA, true, closeType);
            }
        }

        public void OnTrigger(byte worldId, int playerId, PlayPointID id)
        {

        }

        public void OnStartPlay(GameMode gameMode)
        {

        }

        public void SetModeData(object data)
        {

        }
    }
}
