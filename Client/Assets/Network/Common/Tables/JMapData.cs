using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMapData : IACDataIdentified<int>
{
	public int ID = 0;
	public string Name = string.Empty;
	public string ResourcePath = string.Empty;
	public string ResourceDataPath = string.Empty;
	public int GameMode = 0;
	public int MapInfo = 0;
	public int DieFall = 0;
	public int IDENTIFIED => ID;
}
