using UnityEngine;

[System.Serializable]
public class RSkill
{
    
    public int id;
    
    public string type;
    
    public string effect;
    
    public double value;
    
    public double cooldown;
    
    public double damage;
    
    public double mana_cost;
    
    public string name_id;
    
    public string desc_id;
    

    // UnityEngine.JsonUtility를 이용한 역직렬화
    public static RSkill FromJson(string json)
    {
        return JsonUtility.FromJson<RSkill>(json);
    }
}