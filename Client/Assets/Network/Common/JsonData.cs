using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif
using core;

public class JsonData
{
    public static JsonData Instance = new JsonData();

    public Dictionary<string, string> OriginalData = new Dictionary<string, string>();
    public Dictionary<int, JMetaTableData> MetaTable = new Dictionary<int, JMetaTableData>();
    // map_id, spawn list
    public Dictionary<int, List<JSpawnData>> SpawnPositions = new Dictionary<int, List<JSpawnData>>();

    bool isLoaded = false;


    public void LoadOriginalData(Dictionary<string, string> Scripts = null)
    {
        var metaData = JsonManager.LoadJsonArray<JMetaTableData>("JsonData", "MetaTable");
        foreach (var data in metaData)
        {
            MetaTable[data.ID] = data;
            if (data.ServerSync)
            {
                OriginalData[data.TableName] = JsonManager.ReadFile("JsonData", data.TableName);
                if (Scripts != null)
                    Scripts[data.TableName] = OriginalData[data.TableName];
            }
            else
            {
                if (Scripts != null)
                    Scripts[data.TableName] = JsonManager.ReadFile("JsonData", data.TableName);
            }
        }
    }

    void LoadData(string tableName)
    {
        Debug.Log($"LoadData {tableName}");
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var dataObject = assembly.GetType("ACDC");
        var members = dataObject.GetMember(tableName);
        var member = members[0];
        var p = ((System.Reflection.PropertyInfo)member);
        var o = p.GetValue(dataObject);
        var m = p.PropertyType.GetMethod("LoadData");
        //Debug.Log($"LoadData function set {tableName}");

        m.Invoke(o, new object[] { });

        //Debug.Log($"LoadData load done {tableName}");
    }

    public void LoadData(bool isServer, bool isLoadSpawn)
    {
        if (isLoaded)
        {
            return;
        }

        // 메타 테이블 기반으로 테이블들 로드
        foreach(var tableInfo in MetaTable)
        {
            if (isServer)
            {
                if (tableInfo.Value.EnableServer == false)
                    continue;
            }
            else
            {
                if (tableInfo.Value.EnableClient == false)
                    continue;
            }

            LoadData($"{tableInfo.Value.TableName}Data");
        }


        // 인게임 맵 스폰 포인트 로드
        if (isLoadSpawn)
        {
            foreach (var map in ACDC.MapData)
            {
                try
                {
                    var spawn_position = new List<JSpawnData>();
                    var mMapData = JsonManager.LoadJsonArray<JMapObjectData>("JsonData", map.Value.ResourceDataPath);
                    foreach (var block in mMapData)
                    {
                        core.World.AddSpawnPosition(spawn_position, block);
                    }

                    spawn_position.SortSpawnPosition();
                    SpawnPositions.Add(map.Key, spawn_position);
                }
                catch(Exception ex)
                {
                    LogHelper.LogError(ex.ToString());
                }
            }
        }

        isLoaded = true;
    }
}
