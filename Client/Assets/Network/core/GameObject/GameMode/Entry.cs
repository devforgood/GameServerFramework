using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


namespace core
{
    // 게임 모드에서 사용하는 참가자 정보
    public class Entry
    {
        public Entry() { }

        public Entry(int inPlayerID, string inSessionId, Team team, int inNetworkId)
        {
            mNetworkId = inNetworkId;
            mPlayerId = inPlayerID;
            mSessionId = inSessionId;
            mTeam = team;
            SetScore(0);
            KillCount = 0;

            Missions = new Dictionary<int, int>();
            IsLeave = true;
        }

        public Team GetTeam() { return mTeam; }
        public int GetPlayerId() { return mPlayerId; }
        public string GetFormattedNameScore() { return mFormattedNameScore; }
        public int GetScore() { return mScore; }

        public string GetSessionId() { return mSessionId; }

        public void SetScore(int inScore)
        {
            mScore = inScore;
            mFormattedNameScore = $"{mSessionId} {mScore}";
        }

        public bool Write(NetOutgoingMessage inOutputStream)
        {
            bool didSucceed = true;

            inOutputStream.Write((uint)mTeam, GameMode.MaxTeamBits);
            inOutputStream.Write(mPlayerId);
            inOutputStream.Write(mSessionId);
            inOutputStream.Write(mScore);
            inOutputStream.Write(KillCount);
            inOutputStream.Write(IsPause);
            inOutputStream.Write(IsLeave);

            //LogHelper.LogInfo($"team{mTeam}, playerid{mPlayerId}, playername{mPlayerName}, score{mScore}");

            return didSucceed;
        }
        public bool Read(NetIncomingMessage inInputStream)
        {
            bool didSucceed = true;

            mTeam = (Team)inInputStream.ReadUInt32(GameMode.MaxTeamBits);

            mPlayerId = inInputStream.ReadInt32();

            mSessionId = inInputStream.ReadString();

            int score = inInputStream.ReadInt32();
            if (didSucceed)
            {
                SetScore(score);
            }

            KillCount = inInputStream.ReadInt32();
            IsPause = inInputStream.ReadBoolean();
            IsLeave = inInputStream.ReadBoolean();

            LogHelper.LogInfo($"team:{mTeam}, playerid:{mPlayerId}, sessionId:{mSessionId}, score:{score}, kill:{KillCount}, pause:{IsPause}, leave:{IsLeave}");
            return didSucceed;
        }
        //public static UInt32 GetSerializedSize();

        public bool Leave
        {
            get { return IsLeave; }
            set { IsLeave = value; }
        }

        public bool Pause 
        { 
            get { return IsPause; } 
            set { IsPause = value; }
        }

        Team mTeam;

        int mPlayerId;
        string mSessionId;
        public int mNetworkId;

        int mScore;

        string mFormattedNameScore;

        public int seat;
        public int KillCount;

        bool IsLeave = false;
        bool IsPause = false;

        public Dictionary<int, int> Missions;
    };

}
