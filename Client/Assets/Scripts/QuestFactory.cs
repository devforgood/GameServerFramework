// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class QuestFactory
{
    public static Quest Create(int id)
    {
        Quest obj = null;
        Gamedata.Quest res = null;
		if(!GameManager.Instance.resource.Quests.TryGetValue(id, out res))
		{
			Debug.LogError($"Quest with ID {id} not found in resource Quest.");
			return null;
        }

        
        switch (res.CodeName)
        {
            
            case "LimitedTimeQuest": obj = new LimitedTimeQuest(); break;
            
            case "MainQuest": obj = new MainQuest(); break;
            
            case "RepeatedQuest": obj = new RepeatedQuest(); break;
            
            default:
                
                return null;
                
        }
        

        obj.gamedata = res;

        return obj;
    }
} 