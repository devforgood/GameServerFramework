// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class MapFactory
{
    public static Map Create(int id)
    {
        Map obj = null;
        switch (id)
        {
            
            default: return null;
        }

        Gamedata.Map res = null;
		if(!GameManager.Instance.resource.Maps.TryGetValue(id, out res))
		{
			Debug.LogError($"Map with ID {id} not found in resource Maps.");
			return null;
        }
        obj.gamedata = res;

        return obj;
    }
} 