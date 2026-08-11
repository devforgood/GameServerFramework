// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class NpcFactory
{
    public static Npc Create(int id)
    {
        Npc obj = null;
        Gamedata.Npc res = null;
		if(!GameManager.Instance.resource.Npcs.TryGetValue(id, out res))
		{
			Debug.LogError($"Npc with ID {id} not found in resource Npc.");
			return null;
        }

        
        obj = new Npc();
        

        obj.gamedata = res;

        return obj;
    }
} 