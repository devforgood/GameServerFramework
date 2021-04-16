using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMotionEffectData : IACDataIdentified<int>
{
	public int ID = 0;
	public string BoneName = string.Empty;
	public bool IsLoop = false;
	public int effectID_localActorTeam = 0;
	public int effectID_otherActorTeam = 0;
	public bool IsFollowBone = false;
	public bool IsAttachBone = false;
	public int IDENTIFIED => ID;
}
