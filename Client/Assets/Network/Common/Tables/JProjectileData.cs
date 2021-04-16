using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JProjectileData : IACDataIdentified<int>
{
	public int ID = 0;
	public string projectileType = string.Empty;
	public float speedRatio = 0.0f;
	public float[] range = null;
	public int effectID_localActorTeam = 0;
	public int effectID_otherActorTeam = 0;
	public int Shot_SoundID = 0;
	public int IDENTIFIED => ID;
}
