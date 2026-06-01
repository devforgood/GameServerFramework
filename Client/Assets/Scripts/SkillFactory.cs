// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class SkillFactory
{
    public static Skill Create(int id)
    {
        Skill obj = null;
        Gamedata.Skill res = null;
		if(!GameManager.Instance.resource.Skills.TryGetValue(id, out res))
		{
			Debug.LogError($"Skill with ID {id} not found in resource Skills.");
			return null;
        }

        
        switch (res.CodeName)
        {
            
            case "JumpSkill": obj = new JumpSkill(); break;
            
            case "NormalAttackSkill": obj = new NormalAttackSkill(); break;
            
            default:
                
                return null;
                
        }
        

        obj.gamedata = res;

        return obj;
    }
} 