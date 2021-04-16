using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JCommonPopupData : IACDataIdentified<string>
{
	public string ID = string.Empty;
	public bool IsBaseTable = false;
	public string title_string = string.Empty;
	public string title_icon = string.Empty;
	public int popup_type = 0;
	public string main_string = string.Empty;
	public string rightbtn_string = string.Empty;
	public string leftbtn_string = string.Empty;
	public int device_backbtn = 0;
	public string IDENTIFIED => ID;
}
