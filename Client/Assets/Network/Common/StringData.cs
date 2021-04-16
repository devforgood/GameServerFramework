using System.Collections.Generic;
using System.Text;

/// <summary>
/// 패치 이후 스트링 데이타
/// </summary>
public class StringData
{
    static Dictionary<string, JStringData> _StringDatas = new Dictionary<string, JStringData>();

    protected static string _currLanguage = string.Empty;
   
    public static void SetLanguage(string langCode)
    {
        if (_currLanguage.Equals(langCode) == true)
            return;

        _currLanguage = langCode;
    }

    public static string CurrLanguage
    {
        get { return _currLanguage; }
    }

    public static void LoadStringData()
    {
        var StringDatas = JsonManager.LoadJsonArray<JStringData>("JsonData", "StringTable_AfterLobby");
        foreach (var data in StringDatas)
        {
            _StringDatas[data.KEY] = data;
        }
    }

    public static string GetStringData(string key)
    {
        JStringData data = null;
        StringData._StringDatas.TryGetValue(key.Trim(), out data);

        if (data == null)
        {
#if UNITY_EDITOR
            return string.Format("KEY : {0}", key);
#else
            return "";
#endif
        }

        switch(StringData.CurrLanguage)
        {
            case "KO":
                return data.Kor;
            case "EN":
                return data.Eng;
            default:
                return data.Kor;
        }
    }
}