using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JShopListData : IACDataIdentified<int>
{
	public int Id = 0;
	public bool Enable = false;
	public int ResetType = 0;
	public int[] ProductGroupId = null;
	public int IDENTIFIED => Id;
}
