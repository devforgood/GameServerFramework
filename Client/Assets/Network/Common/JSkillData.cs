using System;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif


public partial class JSkillData
{
    public int GetDamage(int character_power_level)
    {
        return this.Damage + ((character_power_level - 1) * this.DamageLv);
    }
}
