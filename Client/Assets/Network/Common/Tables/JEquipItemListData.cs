using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JEquipItemListData : IACDataIdentified<int>
{
	public int Id = 0;
	public bool ItemRank = false;
	public int UniqueId = 0;
	public int ItemId = 0;
	public int ItemLevel = 0;
	public int ReqPowerLevel = 0;
	public int TypeSprite = 0;
	public int[] UseCharacter = null;
	public string ItemInfoStringKey = string.Empty;
	public string TargetTable = string.Empty;
	public string TargetColumnName = string.Empty;
	public float StatusValue = 0.0f;
	public int SpellId = 0;
	public int IDENTIFIED => Id;
}
