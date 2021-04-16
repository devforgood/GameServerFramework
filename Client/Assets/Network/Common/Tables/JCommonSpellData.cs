using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JCommonSpellData : IACDataIdentified<int>
{
	public int ID = 0;
	public int IDENTIFIED => ID;
}
