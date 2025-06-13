// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class SkillFactory
{
    public static Skill Create(int id)
    {
        Skill obj = null;
        switch (id)
        {
            
            case 2: obj = new JumpSkill(); break;
            
            case 1: obj = new NormalAttackSkill(); break;
            
            default: return null;
        }

        Gamedata.Skill res = null;
		if(!GameManager.Instance.resource.Skills.TryGetValue(id, out res))
		{
			Debug.LogError($"Skill with ID {id} not found in resource Skills.");
			return null;
        }
        obj.gamedata = res;

        return obj;
    }
} 