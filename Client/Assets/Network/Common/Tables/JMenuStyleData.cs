using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMenuStyleData : IACDataIdentified<int>
{
	public int Id = 0;
	public string Title = string.Empty;
	public string MenuStyle = string.Empty;
	public bool IsShowBackButton = false;
	public int IDENTIFIED => Id;
}
