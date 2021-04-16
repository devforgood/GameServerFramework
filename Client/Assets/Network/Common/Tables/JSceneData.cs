using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JSceneData : IACDataIdentified<string>
{
	public string SceneName = string.Empty;
	public bool UseFadeInOut = false;
	public bool IsBackable = false;
	public int MenuIdx = 0;
	public string IDENTIFIED => SceneName;
}
