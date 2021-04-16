using System;
using System.Collections.Generic;
using System.Text;

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
#else
namespace BulletSharp
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type t)
        {

        }
    }
}
#endif