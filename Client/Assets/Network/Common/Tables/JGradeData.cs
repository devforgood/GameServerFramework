using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JGradeData : IACDataIdentified<int>
{
	public int ID = 0;
	public int Grade = 0;
	public int GradeLevel = 0;
	public int AccountBS = 0;
	public string GradeIcon = string.Empty;
	public int GradeRewardId = 0;
	public int IDENTIFIED => ID;
}
