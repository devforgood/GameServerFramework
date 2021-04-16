using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JGacha_TableData : IACDataIdentified<int>
{
	public int ID = 0;
	public int GroupId = 0;
	public int ItemID = 0;
	public int get_rate = 0;
	public int Count_min = 0;
	public int Count_max = 0;
	public int IDENTIFIED => ID;
}
