// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class ItemFactory
{
    public static Item Create(int id)
    {
        Item obj = null;
        switch (id)
        {
            
            default: return null;
        }

        Gamedata.Item res = null;
		if(!GameManager.Instance.resource.Items.TryGetValue(id, out res))
		{
			Debug.LogError($"Item with ID {id} not found in resource Items.");
			return null;
        }
        obj.gamedata = res;

        return obj;
    }
} 