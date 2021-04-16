using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JAdListData : IACDataIdentified<int>
{
	public int Id = 0;
	public int UseAdType = 0;
	public bool Skip = false;
	public int ItemId = 0;
	public int Count = 0;
	public int ViewLimit = 0;
	public bool Con_View = false;
	public string ResetTime = string.Empty;
	public string UseScene = string.Empty;
	public int IDENTIFIED => Id;
}
