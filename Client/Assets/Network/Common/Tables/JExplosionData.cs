using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JExplosionData : IACDataIdentified<int>
{
	public int ID = 0;
	public int type = 0;
	public float[] range = null;
	public int method = 0;
	public float tickTime = 0.0f;
	public float time = 0.0f;
	public int effectID_localActorTeam = 0;
	public int effectID_otherActorTeam = 0;
	public int Explosion_SoundID = 0;
	public int IDENTIFIED => ID;
}
