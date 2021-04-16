using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JInGameUIData : IACDataIdentified<int>
{
	public int ID = 0;
	public string ResourcePath = string.Empty;
	public int IDENTIFIED => ID;
}
