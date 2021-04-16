using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JCharacterInfoData : IACDataIdentified<int>
{
	public int Id = 0;
	public string NameKey = string.Empty;
	public string HeroDescription = string.Empty;
	public string RareLabel = string.Empty;
	public int RareId = 0;
	public string RareLabelColor = string.Empty;
	public string BattleTypeSpriteName = string.Empty;
	public string HpSpriteName = string.Empty;
	public string AttackSpriteName = string.Empty;
	public string UnionTypeSpriteName = string.Empty;
	public string SkillSpriteName = string.Empty;
	public string BattleTypeStringKey = string.Empty;
	public string HpStringKey = string.Empty;
	public int HpNum = 0;
	public int HpLv = 0;
	public string AttackStringKey = string.Empty;
	public int AttackNum = 0;
	public int NoArkrId = 0;
	public string SkillStringKey = string.Empty;
	public string SkillEfficacyKey = string.Empty;
	public int SkillNum = 0;
	public int SkillAtkId = 0;
	public string AttackNameKey = string.Empty;
	public string AttackDescriptionKey = string.Empty;
	public string SkillNameKey = string.Empty;
	public string SkillDescriptionKey = string.Empty;
	public string RequireDescriptionKey = string.Empty;
	public string CharacterPath = string.Empty;
	public string CharacterSpriteName = string.Empty;
	public int IDENTIFIED => Id;
}
