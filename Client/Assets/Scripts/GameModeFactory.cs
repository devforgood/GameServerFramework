// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class GameModeFactory
{
    public static GameMode Create(int id)
    {
        GameMode obj = null;
        switch (id)
        {
            
            default: return null;
        }

        Gamedata.GameMode res = null;
		if(!GameManager.Instance.resource.GameModes.TryGetValue(id, out res))
		{
			Debug.LogError($"GameMode with ID {id} not found in resource GameModes.");
			return null;
        }
        obj.gamedata = res;

        return obj;
    }
} 