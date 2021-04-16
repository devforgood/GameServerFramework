using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JCharacterSelectData : IACDataIdentified<int>
{
	public int Id = 0;
	public int[] CharacterId = null;
	public int IDENTIFIED => Id;
}
