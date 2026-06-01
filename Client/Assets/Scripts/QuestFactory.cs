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
			Debug.LogError($"Quest with ID {id} not found in resource Quests.");
			return null;
        }

        
        obj = new Quest();
        

        obj.gamedata = res;

        return obj;
    }
} 