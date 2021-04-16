using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JInGameCashEffectData : IACDataIdentified<int>
{
	public int Id = 0;
	public int PriceType = 0;
	public int[] CashValue = null;
	public int IconValue = 0;
	public int IDENTIFIED => Id;
}
