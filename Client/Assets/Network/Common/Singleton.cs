#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
public abstract class Singleton : MonoBehaviour
{
    private long _createTime = 0;

    public long CreateTime
    {
        get { return _createTime; }
    }

    public void SetCreateTime()
    {
        _createTime = System.DateTime.Now.Ticks;
    }

    public bool IsValidCreateTime()
    {
        return 0 != _createTime;
    }


    /// <summary>
    /// 씬 로드시 삭제 여부를 결정합니다.
    /// </summary>
    public virtual bool destroyOnLoad
    {
        get { return true; }
    }

    public virtual void Init() { }

    //public virtual void Init(Intent it) { }

    //public abstract void OnReset();
}
/// <summary>
/// Inherit from this base class to create a singleton.
/// e.g. public class MyClassName : Singleton<MyClassName> {}
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
//public class Singleton<T> : Singleton where T : Singleton<T>   
{

    private long _createTime = 0;

    public long CreateTime
    {
        get { return _createTime; }
    }

    public void SetCreateTime()
    {
        _createTime = System.DateTime.Now.Ticks;
    }

    public bool IsValidCreateTime()
    {
        return 0 != _createTime;
    }


    /// <summary>
    /// 씬 로드시 삭제 여부를 결정합니다.
    /// </summary>
    public virtual bool destroyOnLoad
    {
        get { return true; }
    }

    public virtual void Init() { }

    //public virtual void Init(Intent it) { }


    // Check to see if we're about to be destroyed.
    private static bool m_ShuttingDown = false;
    //private static object m_Lock = new object();
    private static T m_Instance;

    /// <summary>
    /// Access singleton instance through this propriety.
    /// </summary>
    public static T Instance
    {
        get
        {
			//if( m_ShuttingDown )
			//{
			//	Debug.LogWarning( "[Singleton] Instance '" + typeof( T ) +
			//		"' already destroyed. Returning null." );
			//	return null;
			//}

			//lock (m_Lock)
			//{
			if(m_Instance == null)
            {
                // Search for existing instance.                                        
                m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;

                // Create new instance if one doesn't already exist.
                if(m_Instance == null)
                {
                    // Need to create a new GameObject to attach the singleton to.
                    var singletonObject = new GameObject();
                    m_Instance = singletonObject.AddComponent<T>();
                    singletonObject.name = typeof(T).ToString() + " (Singleton)";

                    // Make instance persistent.
                    DontDestroyOnLoad(singletonObject);
                }
            }

            return m_Instance;
            //}
        }
    }
    //public static T Instance
    //{
    //    get
    //    {
    //        if (m_ShuttingDown)
    //        {
    //            Debug.LogWarning("[Singleton] Instance '" + typeof(T) +
    //                "' already destroyed. Returning null.");
    //            return null;
    //        }

    //        //lock (m_Lock)
    //        //{
    //        if (m_Instance == null)
    //        {
    //            // Search for existing instance.                                        
    //            m_Instance = GameObject.FindObjectOfType(typeof(T)) as T;

    //            // Create new instance if one doesn't already exist.
    //            if (m_Instance == null)
    //            {
    //                GameObject a_gomeObject = null;
    //                //리소스 폴더에 있는지 확인
    //                if (false == SingletonManager.Instance.CachedObject.TryGetValue(typeof(T), out a_gomeObject))
    //                {
    //                    GameObject  go          = new GameObject(typeof(T).Name, typeof(T));
    //                                m_Instance  = go.GetComponent<T>();
    //                                //m_Instance.se
    //                }
    //                else
    //                {
    //                    m_Instance = GameObjectUtils.InstanceObject<T>(a_gomeObject);
    //                }



    //                // Make instance persistent.
    //                DontDestroyOnLoad(m_Instance);
    //            }
    //        }

    //        return m_Instance;
    //        //}
    //    }
    //}


    protected virtual void OnApplicationQuit()
    {
        m_ShuttingDown = true;
    }


    public virtual void OnDestroy()
    {
        m_ShuttingDown = true;
    }
}

#endif