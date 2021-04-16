using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JGameModeData : IACDataIdentified<int>
{
	public int ID = 0;
	public string Name = string.Empty;
	public string LogName = string.Empty;
	public int PlayerCount = 0;
	public int TeamCount = 0;
	public int PlayTime = 0;
	public int RewardBattleScoreWin = 0;
	public int RewardBattleScoreLose = 0;
	public int RewardBattleScoreDraw = 0;
	public int RewardBattleScoreMvp = 0;
	public int AbuseBattleScore = 0;
	public int RewardWinMedal = 0;
	public int RewardLoseMedal = 0;
	public int RewardDrawMedal = 0;
	public int RewardMVPMedal = 0;
	public int RewardRankupMedal = 0;
	public int[] ModeSpellIDs = null;
	public string LeaderboardId = string.Empty;
	public int IDENTIFIED => ID;
}
