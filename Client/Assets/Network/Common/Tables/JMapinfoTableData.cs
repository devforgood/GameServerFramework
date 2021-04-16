using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMapinfoTableData : IACDataIdentified<int>
{
	public int ID = 0;
	public string ModeName = string.Empty;
	public string ModeSprite = string.Empty;
	public string ModeIconSprite = string.Empty;
	public string ModeMapIconSprite = string.Empty;
	public string ModeMapName = string.Empty;
	public string ModeDesc = string.Empty;
	public string ModePlayCount = string.Empty;
	public string MapDescription = string.Empty;
	public int IDENTIFIED => ID;
}
