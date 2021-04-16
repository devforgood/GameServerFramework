using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JGacha_Box_BaseData : IACDataIdentified<int>
{
	public int ID = 0;
	public int[] reward_item_group_id = null;
	public int bonus_rate = 0;
	public int bonus_group_id = 0;
	public string Openbox_Sprite = string.Empty;
	public int IDENTIFIED => ID;
}
