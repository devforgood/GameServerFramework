// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class LevelFactory
{
    public static Level Create(int id)
    {
        Level obj = null;
        Gamedata.Level res = null;
		if(!GameManager.Instance.resource.Levels.TryGetValue(id, out res))
		{
			Debug.LogError($"Level with ID {id} not found in resource Level.");
			return null;
        }

        
        obj = new Level();
        

        obj.gamedata = res;

        return obj;
    }
} 