using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JRankingRewardData : IACDataIdentified<int>
{
	public int ID = 0;
	public int RankingStart = 0;
	public int RankingEnd = 0;
	public string RankingID = string.Empty;
	public int GameItemID = 0;
	public int RewardCount = 0;
	public int IDENTIFIED => ID;
}
