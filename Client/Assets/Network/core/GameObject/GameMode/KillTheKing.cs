using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace core
{
    public class KillTheKing : IGameMode
    {
        protected int MaxKillCount = 3;

        protected Dictionary<Team, List<int>> kingPlayerList = new Dictionary<Team, List<int>>();
        protected int kingPlayerId_TeamA = 0;
        protected int kingPlayerId_TeamB = 0;
        protected int lastKilledKingPlayerId = 0;

        public KillTheKing()
        {
            kingPlayerList[Team.TeamA] = new List<int>();
            kingPlayerList[Team.TeamB] = new List<int>();
        }

        public GameModeType GetMode() { return GameModeType.KillTheKing; }

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
            if (actor.StateServerSide == ActorState.Ghost && actor.GetPlayerId() == lastKilledKingPlayerId)
            {
                inOutputStream.Write(true);

                inOutputStream.Write(actor.killPlayerId);
            }
            else
            {
                inOutputStream.Write(false);
            }
            return true;
        }

        public bool TakableDamage(Entry entryA, Entry entryB)
        {
            return entryA.GetTeam() != entryB.GetTeam();
        }

        public static int GetNextKingPlayerId(List<int> players)
        {
            if (players.Count == 0)
                return 0;
            return players[core.MathHelpers.GetRandomInt(players.Count)];
        }

        public virtual void OnTrigger(byte worldId, int playerId, PlayPointID id)
        {
            
        }

        public virtual void OnStartPlay(GameMode gameMode)
        {

        }

        public virtual void OnClose(byte worldId, GameMode gameMode, CloseType closeType)
        {

        }

        public virtual void SetModeData(object data)
        {

        }

        public bool IsKing(int playerId)
        {
            return playerId == kingPlayerId_TeamA || playerId == kingPlayerId_TeamB;
        }
    }
}
