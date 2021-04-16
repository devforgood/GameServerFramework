using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JSoundData : IACDataIdentified<int>
{
	public int ID = 0;
	public string name = string.Empty;
	public string SoundPath = string.Empty;
	public float Volume = 0.0f;
	public bool RangeSoundUse = false;
	public int PlayPriority = 0;
	public bool MergePlay = false;
	public int IDENTIFIED => ID;
}
