using System;
using System.Collections.Generic;

public class DebugCommandData
{
    public static Dictionary<int, JDebugCommandData> dataMap = new Dictionary<int, JDebugCommandData>();

    public static void LoadData()
    {
        try
        {
            var list = JsonManager.LoadJsonArray<JDebugCommandData>("JsonData", "DebugCommand");
            foreach (var data in list)
            {
                dataMap[data.ID] = data;
            }
        }
        catch (Exception ex)
        {

        }

    }

    public static JDebugCommandData GetData(int id)
    {
        if (dataMap == null || dataMap.Count ==0)
            LoadData();

        JDebugCommandData data = null;
        dataMap.TryGetValue(id, out data);
        return data;
    }
}
