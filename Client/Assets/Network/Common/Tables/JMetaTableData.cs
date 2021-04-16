using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JMetaTableData : IACDataIdentified<int>
{
	public int ID = 0;
	public string TableName = string.Empty;
	public bool EnableClient = false;
	public bool EnableServer = false;
	public bool ServerSync = false;
	public int IDENTIFIED => ID;
}
