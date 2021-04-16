using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JEffectData : IACDataIdentified<int>
{
	public int ID = 0;
	public string EffectName = string.Empty;
	public string ResourcePath = string.Empty;
	public int SoundID = 0;
	public float Duration = 0.0f;
	public bool PreLoad = false;
	public bool Pooling = false;
	public bool SelfDestory = false;
	public int IDENTIFIED => ID;
}
