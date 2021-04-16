using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JStringTable_AfterLobbyData : IACDataIdentified<string>
{
	public string KEY = string.Empty;
	public string Kor = string.Empty;
	public string Eng = string.Empty;
	public string IDENTIFIED => KEY;
}
