using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace core
{
    public class TeamDeathmatch : IGameMode
    {
        public GameModeType GetMode() { return GameModeType.TeamDeathmatch; }

        public bool Write(NetOutgoingMessage inOutputStream)
        {
            throw new NotImplementedException();
        }

        public bool Read(NetIncomingMessage inInputStream)
        {
            throw new NotImplementedException();
        }
        public bool WriteGameResult(NetOutgoingMessage inOutputStream, GameMode gameMode)
        {
            bool didSucceed = true;


            return didSucceed;
        }
        public bool ReadGameResult(NetIncomingMessage inInputStream, GameMode gameMode)
        {
            bool didSucceed = true;

            return didSucceed;
        }

        public bool WriteState(NetOutgoingMessage inOutputStream, Actor actor)
        {
            return true;
        }

        public bool TakableDamage(Entry entryA, Entry entryB)
        {
            return true;
        }
        public void OnClose(byte worldId, GameMode gameMode, CloseType closeType)
        {

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
