using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JGameItemData : IACDataIdentified<int>
{
	public int id = 0;
	public string NameString = string.Empty;
	public string SpriteName = string.Empty;
	public int Item_Type = 0;
	public int LinkId = 0;
	public string ItemBgColor = string.Empty;
	public int IDENTIFIED => id;
}
