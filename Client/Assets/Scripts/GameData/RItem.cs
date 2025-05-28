using UnityEngine;

[System.Serializable]
public class RItem
{
    
    public int id;
    
    public string type;
    
    public double heal;
    
    public string name_id;
    
    public string desc_id;
    

    // UnityEngine.JsonUtility를 이용한 역직렬화
    public static RItem FromJson(string json)
    {
        return JsonUtility.FromJson<RItem>(json);
    }
}