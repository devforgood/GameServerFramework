using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMission_infoData : IACDataIdentified<int>
{
	public int Id = 0;
	public int group_id = 0;
	public int MissionType = 0;
	public string Missiontype_Sprite = string.Empty;
	public string Mission_Name = string.Empty;
	public string Mission_Target = string.Empty;
	public int Mission_Value = 0;
	public int Mission_TargetId = 0;
	public string Reward_Sprite = string.Empty;
	public int Reward_Id = 0;
	public int Reward_Value = 0;
	public int IDENTIFIED => Id;
}
