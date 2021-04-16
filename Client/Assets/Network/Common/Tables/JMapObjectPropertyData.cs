using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMapObjectPropertyData : IACDataIdentified<int>
{
	public int ID = 0;
	public string Name = string.Empty;
	public int ObjectId = 0;
	public int Materialtype = 0;
	public int SoundId1 = 0;
	public int SoundId2 = 0;
	public string _비고 = string.Empty;
	public int IDENTIFIED => ID;
}
