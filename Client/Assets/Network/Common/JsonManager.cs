using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#else
using Newtonsoft.Json;
#endif 

public class JsonManager
{

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID

    //Usage:
    //YouObject[] objects = JsonHelper.getJsonArray<YouObject> (jsonString);
    public static T[] getJsonArray<T>(string json)
    {
        string newJson = "{ \"array\": " + json + "}";
        Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
        return wrapper.array;
    }
    //Usage:
    //string jsonString = JsonHelper.arrayToJson<YouObject>(objects);
    public static string arrayToJson<T>(T[] array)
    {
        Wrapper<T> wrapper = new Wrapper<T>();
        wrapper.array = array;
        return JsonUtility.ToJson(wrapper);
    }

    [Serializable]
    private class Wrapper<T>
    {
        public T[] array;
    }

    public static string ObjectToJson(object obj)
    {
        return JsonUtility.ToJson(obj);
    }

    public static T JsonToObject<T>(string jsonData)
    {
        return JsonUtility.FromJson<T>(jsonData);
    }

    public static string RLoadJsonFile(string resourcesLoadPath, string fileName)
    {
        TextAsset targetFile = Resources.Load<TextAsset>($"{resourcesLoadPath}/{fileName}");
        return targetFile.text;
    }

    public static T LoadJsonFile<T>(string loadPath, string fileName)
    {
        return JsonUtility.FromJson<T>(LoadJsonFile(loadPath, fileName));
    }

    public static T[] LoadJsonArray<T>(string loadPath, string fileName)
    {
        return JsonManager.getJsonArray<T>(JsonManager.RLoadJsonFile(loadPath, fileName));
    }

    public static string ReadFile(string loadPath, string fileName)
    {
        return JsonManager.RLoadJsonFile(loadPath, fileName);
    }

    public static T[] LoadJsonArray<T>(string jsonData)
    {
        return JsonManager.getJsonArray<T>(jsonData);
    }


#else

    public static object LoadJsonFilebyJsonConvert(string loadPath, string fileName)
    {
        return JsonConvert.DeserializeObject(LoadJsonFile(loadPath, fileName));
    }

    public static List<T> LoadJsonArray<T>(string loadPath, string fileName)
    {
        return JsonConvert.DeserializeObject<List<T>>(LoadJsonFile(loadPath, fileName));
    }

    public static string ReadFile(string loadPath, string fileName)
    {
        return LoadJsonFile(loadPath, fileName);
    }

    public static  List<T> LoadJsonArray<T>(string jsonData)
    {
        return  JsonConvert.DeserializeObject<List<T>>(jsonData);
    }

#endif


    public static void CreateJsonFile(string createPath, string fileName, string jsonData)
    {
        FileStream fileStream = new FileStream($"{createPath}/{fileName}.json", FileMode.Create);
        byte[] data = Encoding.UTF8.GetBytes(jsonData);
        fileStream.Write(data, 0, data.Length);
        fileStream.Close();
    }

    public static string LoadJsonFile(string loadPath, string fileName)
    {
        FileStream fileStream = new FileStream($"{loadPath}/{fileName}.json", FileMode.Open, FileAccess.Read);
        byte[] data = new byte[fileStream.Length];
        fileStream.Read(data, 0, data.Length);
        fileStream.Close();
        string jsonData = Encoding.UTF8.GetString(data);
        //Debug.Log(jsonData);
        return jsonData;
    }




}
