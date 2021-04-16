using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JItemData : IACDataIdentified<int>
{
	public int ID = 0;
	public string ItemIcon = string.Empty;
	public int[] SpellID = null;
	public string ResourcePath = string.Empty;
	public int SoundID = 0;
	public int ItemType = 0;
	public int IDENTIFIED => ID;
}
