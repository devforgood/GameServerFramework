using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JPowerLevel_TableData : IACDataIdentified<int>
{
	public int Id = 0;
	public int Rare_Id = 0;
	public int PowerLevel = 0;
	public int Pricetype = 0;
	public string Price_Sprite = string.Empty;
	public int Req_Piece = 0;
	public int Req_PriceValue = 0;
	public int IDENTIFIED => Id;
}
