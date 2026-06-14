// This file is auto-generated. Do not modify directly.

using System;
using UnityEngine;

public static class MonsterDataFactory
{
    public static MonsterData Create(int id)
    {
        MonsterData obj = null;
        Gamedata.MonsterData res = null;
		if(!GameManager.Instance.resource.MonsterDatas.TryGetValue(id, out res))
		{
			Debug.LogError($"MonsterData with ID {id} not found in resource MonsterData.");
			return null;
        }

        
        obj = new MonsterData();
        

        obj.gamedata = res;

        return obj;
    }
} 