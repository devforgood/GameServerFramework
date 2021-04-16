using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JCharacterData : IACDataIdentified<int>
{
	public int ID = 0;
	public string Remark = string.Empty;
	public bool Enable = false;
	public bool PayFree = false;
	public int RareLabel = 0;
	public int UnionType = 0;
	public int BattleType = 0;
	public int Grade = 0;
	public int Damage = 0;
	public int Damage_Lv = 0;
	public int HP = 0;
	public int HP_Lv = 0;
	public float Speed = 0.0f;
	public float Speed_Lim = 0.0f;
	public float Set_Value = 0.0f;
	public float Set_Value_Lv = 0.0f;
	public int Set_Value_Lim = 0;
	public float Exp_W_Area_Lv = 0.0f;
	public float Exp_L_Area_Lv = 0.0f;
	public int Scool_Limit = 0;
	public int BombID = 0;
	public int Bomb1_Ammo = 0;
	public float Bomb1_installCoolTime = 0.0f;
	public int Skill1ID = 0;
	public int Skill2ID = 0;
	public int Skill3ID = 0;
	public int[] SpawnSpellIDs = null;
	public int Sound_Atk = 0;
	public int Sound_Skill = 0;
	public int Sound_Hit = 0;
	public int Sound_Die = 0;
	public string Char_Path = string.Empty;
	public int IDENTIFIED => ID;
}
