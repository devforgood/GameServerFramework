using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public interface IGameMode
    {
        GameModeType GetMode();
        bool Write(NetOutgoingMessage inOutputStream);
        bool Read(NetIncomingMessage inInputStream);


        bool WriteGameResult(NetOutgoingMessage inOutputStream, GameMode gameMode);
        bool ReadGameResult(NetIncomingMessage inInputStream, GameMode gameMode);

        bool WriteState(NetOutgoingMessage inOUtputStream, Actor actor);

        /// <summary>
        /// entryA에서 entryB로 데미지를 줄수 있는지 여부
        /// </summary>
        /// <param name="entryA"></param>
        /// <param name="entryB"></param>
        /// <returns></returns>
        bool TakableDamage(Entry entryA, Entry entryB);

        void OnStartPlay(GameMode gameMode);
        /// <summary>
        /// 플레이 타임 종료
        /// </summary>
        void OnClose(byte worldId, GameMode gameMode, CloseType closeType);

        void OnTrigger(byte worldId, int playerId, PlayPointID id);

        void SetModeData(object data);
    }
}
