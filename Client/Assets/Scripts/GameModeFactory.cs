// This file is auto-generated. Do not modify directly.

using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameModeDataManager
{
    private static Dictionary<int, Gamedata.GameMode> _data = new Dictionary<int, Gamedata.GameMode>();
    
    public static void Initialize()
    {
        _data.Clear();
        // 데이터 로딩은 GameManager에서 처리됨
    }
    
    public static Gamedata.GameMode Get(int id)
    {
        if (_data.TryGetValue(id, out var data))
        {
            return data;
        }
        
        Debug.LogWarning($"GameMode with ID {id} not found.");
        return null;
    }
    
    public static Dictionary<int, Gamedata.GameMode> GetAll()
    {
        return new Dictionary<int, Gamedata.GameMode>(_data);
    }
    
    public static void SetData(Dictionary<int, Gamedata.GameMode> data)
    {
        _data = data;
    }
    
    public static bool Contains(int id)
    {
        return _data.ContainsKey(id);
    }
    
    public static int Count()
    {
        return _data.Count;
    }
} 