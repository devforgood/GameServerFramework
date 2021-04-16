using UnityEngine;
using System.Collections.Generic;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{

    protected static T _instance = null;
    public static T Instance
    {
        get
        {
#if UNITY_EDITOR
            if (Application.isPlaying == false)
            {
                return null;
            }
#endif
            if (_instance == null)
            {
                _instance = GameObject.FindObjectOfType(typeof(T)) as T;

                if (_instance == null)
                    _instance = new GameObject(typeof(T).ToString()).AddComponent<T>();
            }
            return _instance;
        }
    }

    protected void Awake() { _instance = this.transform.GetComponent<T>(); }
    public virtual void Init() { }
    public virtual void Destroy() { Destroy(this); }

}