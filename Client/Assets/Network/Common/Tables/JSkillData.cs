using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

[System.Serializable]
public partial class  JSkillData : IACDataIdentified<int>
{
	public int skillId = 0;
	public string skillName = string.Empty;
	public string componentType = string.Empty;
	public int[] DamageTarget = null;
	public int CooltimeType = 0;
	public int skillTarget = 0;
	public int skillKind = 0;
	public int skillType = 0;
	public float[] skillRange = null;
	public int[] projectileNum = null;
	public int LaunchType = 0;
	public float LaunchInter = 0.0f;
	public int LaunchForm = 0;
	public float projectileInter = 0.0f;
	public float durationTime = 0.0f;
	public int SiegeAtk = 0;
	public int SiegeType = 0;
	public int Damage = 0;
	public int DamageLv = 0;
	public float installCoolTime = 0.0f;
	public int soundID = 0;
	public int applySoundID = 0;
	public int linkSkillId = 0;
	public int[] spellId = null;
	public int projectileID = 0;
	public int explosionID = 0;
	public int[] motionEffectID = null;
	public int pathGuideID = 0;
	public int rangeGuideID = 0;
	public int LaunchAngle = 0;
	public bool IsNoMove = false;
	public int IDENTIFIED => skillId;
}
