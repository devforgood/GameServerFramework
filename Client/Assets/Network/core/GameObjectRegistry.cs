using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
#endif

namespace core
{
    public class GameObjectRegistry
    {
        public delegate NetGameObject GameObjectCreationFunc(byte worldId);

        Dictionary<UInt32, GameObjectCreationFunc> mNameToGameObjectCreationFunctionMap = new Dictionary<UInt32, GameObjectCreationFunc>();
        Dictionary<UInt32, GameObjectCreationFunc> mNameToGameObjectCreationFunctionMapInHost = new Dictionary<UInt32, GameObjectCreationFunc>();
        public bool IsHost = false;

        Dictionary<int, Stack<NetGameObject>> gameObjectPool;

        /// <summary>
        /// Global instance of GameObjectRegistry
        /// </summary>
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        public static GameObjectRegistry sInstance = new GameObjectRegistry();

        public static void StaticInit()
        {
            sInstance = new GameObjectRegistry();
        }

        public void RegisterCreationFunction(UInt32 inFourCCName, GameObjectCreationFunc inCreationFunction, bool is_server)
        {
            if (IsHost == true && is_server == true)
                mNameToGameObjectCreationFunctionMapInHost[inFourCCName] = inCreationFunction;
            else
                mNameToGameObjectCreationFunctionMap[inFourCCName] = inCreationFunction;
        }

        public NetGameObject CreateGameObject(UInt32 inFourCCName, bool is_server, byte worldId = 0, bool is_use_pool = true)
        {
            NetGameObject gameObject = null;
            // 풀링된 데이터가 있는지 확인
            if (gameObjectPool != null && is_use_pool == true)
            {
                Stack<NetGameObject> pool = null;
                if (gameObjectPool.TryGetValue((int)inFourCCName, out pool) &&  pool.Count > 0)
                {
                    gameObject = pool.Pop();
                }
                else
                {
                    Debug.Log($"not enough :{(GameObjectClassId)inFourCCName}");
                }
            }

            if(gameObject == null)
            {
                if(IsHost == true && is_server == true)
                    gameObject = mNameToGameObjectCreationFunctionMapInHost[inFourCCName](worldId);
                else
                    gameObject = mNameToGameObjectCreationFunctionMap[inFourCCName](worldId);
            }

            //no error checking- if the name isn't there, exception!
            //GameObjectCreationFunc creationFunc = mNameToGameObjectCreationFunctionMap[inFourCCName];

            //var gameObject = creationFunc(worldId);

            //should the registry depend on the world? this might be a little weird
            //perhaps you should ask the world to spawn things? for now it will be like this
            World.Instance(worldId).AddGameObject(gameObject);

            return gameObject;
        }

        public void RegisterAll(System.Reflection.Assembly assembly, string prefix, bool is_server, bool is_pool = false, Dictionary<int, int> object_count = null)
        {
            if (assembly == null)
                assembly = System.Reflection.Assembly.GetExecutingAssembly();


            if (is_pool)
            {
                gameObjectPool = new Dictionary<int, Stack<NetGameObject>>();
            }


            foreach (var value in Enum.GetValues(typeof(core.GameObjectClassId)))
            {
                var class_name = $"{prefix}{(core.GameObjectClassId)value}";
                var gameObjectType = assembly.GetType(class_name);
                if (gameObjectType == null)
                    continue;

                var func = (GameObjectCreationFunc)Delegate.CreateDelegate(typeof(GameObjectCreationFunc), null, gameObjectType.GetMethod("StaticCreate"));
                RegisterCreationFunction((uint)(int)value, func, is_server);

                if (is_pool)
                {
                    var count = 0;
                    if (object_count.TryGetValue((int)((core.GameObjectClassId)value), out count))
                    {
                        gameObjectPool[(int)((core.GameObjectClassId)value)] = new Stack<core.NetGameObject>();
                        for (int i = 0; i < count; ++i)
                        {
                            var gameObject = func(World.DefaultWorldIndex);
                            gameObjectPool[(int)((core.GameObjectClassId)value)].Push((core.NetGameObject)gameObject);
                        }
                    }
                }
            }
        }
    }
}
