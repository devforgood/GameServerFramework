using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JSpellData : IACDataIdentified<int>
{
	public int Index = 0;
	public int SpellType = 0;
	public int SpellSubType = 0;
	public int ActuationPercent = 0;
	public int ApplyObject = 0;
	public float AddStatus = 0.0f;
	public int AddStatusType = 0;
	public float RetentionTime = 0.0f;
	public float TickTime = 0.0f;
	public int ResetID = 0;
	public int BuffID = 0;
	public int StatusType = 0;
	public int effectID = 0;
	public string ToastPopString = string.Empty;
	public int IDENTIFIED => Index;
}
