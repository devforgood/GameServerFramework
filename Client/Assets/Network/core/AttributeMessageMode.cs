﻿using Lidgren.Network;
using System;
using System.IO;
using System.Reflection;

namespace core
{
    /// <summary>
    /// Represents the length of a var int encoded hash
    /// Note that the HashSize does not say anything about the actual final output due to the var int encoding
    /// It just says how many bytes the maximum will be
    /// </summary>
    public enum HashSize
    {
        /// <summary>
        /// Two byte hash
        /// </summary>
        VarIntTwoBytes,
        /// <summary>
        /// Four byte hash
        /// </summary>
        VarIntFourBytes,
        /// <summary>
        /// Eight byte hash
        /// </summary>
        VarIntEightBytes
    }

    /// <summary>
    /// Delegate definition for performance RPC's.
    /// </summary>
    public delegate void RpcDelegate(int clientId, NetBuffer stream);

    internal class ReflectionMethod
    {
        private MethodInfo method;
        private Type[] parameterTypes;
        private object[] parameterRefs;

        public ReflectionMethod(MethodInfo methodInfo)
        {
            method = methodInfo;
            ParameterInfo[] parameters = methodInfo.GetParameters();
            parameterTypes = new Type[parameters.Length];
            parameterRefs = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                parameterTypes[i] = parameters[i].ParameterType;
            }
        }

        internal object Invoke(object instance, NetBuffer stream)
        {
                for (int i = 0; i < parameterTypes.Length; i++)
                {
                    parameterRefs[i] = stream.ReadObjectPacked(parameterTypes[i]);
                }

                return method.Invoke(instance, parameterRefs);
        }
    }

    /// <summary>
    /// Attribute used on methods to me marked as ServerRPC
    /// ServerRPC methods can be requested from a client and will execute on the server
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ServerRPC : Attribute
    {
        /// <summary>
        /// Whether or not the ServerRPC should only be run if executed by the owner of the object
        /// </summary>
        public bool RequireOwnership = true;
        internal ReflectionMethod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }

    /// <summary>
    /// Attribute used on methods to me marked as ClientRPC
    /// ClientRPC methods can be requested from the server and will execute on a client
    /// Remember that a host is a server and a client
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = true, AllowMultiple = false)]
    public class ClientRPC : Attribute
    {
        internal ReflectionMethod reflectionMethod;
        internal RpcDelegate rpcDelegate;
    }
}
