using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

public partial class JCharacterData
{

    [NonSerialized]
    Dictionary<int, JPowerLevel_TableData> _LevelTable = null;

    [NonSerialized]
    int? _Level = null;

    [NonSerialized]
    int? _MaxLevel = null;

    public Dictionary<int, JPowerLevel_TableData> LevelTable
    {
        get
        {
            if(_LevelTable==null)
            {
                _LevelTable = ACDC.PowerLevel_TableData.Where(x => x.Value.Rare_Id == RareLabel).Select(x => x.Value).ToDictionary(x=>x.PowerLevel, x=>x);
            }
            return _LevelTable;
        }
    }

    public int Level
    {
        get
        {
            if(_Level == null)
            {
                _Level = LevelTable.Min(x => x.Value.PowerLevel);
            }
            return (int)_Level;
        }
    }

    public int MaxLevel
    {
        get
        {
            if (_MaxLevel == null)
            {
                _MaxLevel = LevelTable.Max(x => x.Value.PowerLevel) + 1;
            }
            return (int)_MaxLevel;
        }
    }


    public int GetCharacterHp(int character_power_level)
    {
        return this.HP + ((character_power_level - 1) * this.HP_Lv);
    }

    public int GetStrikingPower(int character_power_level)
    {
        int StrikingPower = 0;
        StrikingPower += ACDC.SkillData[BombID].GetDamage(character_power_level);
        //StrikingPower += ACDC.SkillData[Skill1ID].GetDamage(character_power_level);

        return StrikingPower;
    }

    public bool IsPowerLevelUp(int character_power_level, int characgter_piece)
    {
        JPowerLevel_TableData power_level_data;
        if (LevelTable.TryGetValue(character_power_level, out power_level_data) == false)
        {
            return false;
        }

        if (characgter_piece < power_level_data.Req_Piece)
        {
            return false;
        }
        return true;
    }

}

