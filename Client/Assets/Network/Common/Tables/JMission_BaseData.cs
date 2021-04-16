using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMission_BaseData : IACDataIdentified<int>
{
	public int id = 0;
	public bool Enable = false;
	public int count = 0;
	public int group_id = 0;
	public int reward_item_id = 0;
	public int IDENTIFIED => id;
}
