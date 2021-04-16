using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JShopItemListData : IACDataIdentified<int>
{
	public int Id = 0;
	public int GroupId = 0;
	public int ItemId = 0;
	public string PriceSprite = string.Empty;
	public int PriceType = 0;
	public int PriceValue = 0;
	public int[] Quantity = null;
	public int PurchaseLimitedCount = 0;
	public int LinkId = 0;
	public string logName = string.Empty;
	public int ShopItemType = 0;
	public int IDENTIFIED => Id;
}
