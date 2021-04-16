using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JDebugCommandData : IACDataIdentified<int>
{
	public int ID = 0;
	public string Command = string.Empty;
	public string Description = string.Empty;
	public string Target = string.Empty;
	public string Apply = string.Empty;
	public string Param1 = string.Empty;
	public string DefaultValue1 = string.Empty;
	public string Param2 = string.Empty;
	public string DefaultValue2 = string.Empty;
	public string Param3 = string.Empty;
	public string DefaultValue3 = string.Empty;
	public string Param4 = string.Empty;
	public string DefaultValue4 = string.Empty;
	public int IDENTIFIED => ID;
}
