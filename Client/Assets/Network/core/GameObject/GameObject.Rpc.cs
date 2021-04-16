using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace core
{
    public partial class NetGameObject
    {
        protected bool IsClient => IsRunning && Engine.sInstance.IsClient;
        private bool IsRunning => Engine.sInstance.IsRunning;

        protected bool IsServer => IsRunning && Engine.sInstance.IsServer;

        int OwnerClientId => Engine.sInstance.ServerClientId;

        public delegate NetBuffer OnCreateRpcPacket(int clientId);
        public delegate void OnSend(int clientId, NetBuffer inOutputStream);

        public static OnCreateRpcPacket CreateRpcPacketClient;
        public static OnSend SendClient;
        public static OnCreateRpcPacket CreateRpcPacketServer;
        public static OnSend SendServer;

        #region MESSAGING_SYSTEM

#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static Dictionary<NetGameObject, Dictionary<ulong, ClientRPC>> CachedClientRpcs = new Dictionary<NetGameObject, Dictionary<ulong, ClientRPC>>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static Dictionary<NetGameObject, Dictionary<ulong, ServerRPC>> CachedServerRpcs = new Dictionary<NetGameObject, Dictionary<ulong, ServerRPC>>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static Dictionary<Type, MethodInfo[]> Methods = new Dictionary<Type, MethodInfo[]>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static Dictionary<ulong, string> HashResults = new Dictionary<ulong, string>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static Dictionary<MethodInfo, ulong> methodInfoHashTable = new Dictionary<MethodInfo, ulong>();
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        private static StringBuilder methodInfoStringBuilder = new StringBuilder();

        public static void InitStatic()
        {
            CachedClientRpcs = new Dictionary<NetGameObject, Dictionary<ulong, ClientRPC>>();
            CachedServerRpcs = new Dictionary<NetGameObject, Dictionary<ulong, ServerRPC>>();
            Methods = new Dictionary<Type, MethodInfo[]>();
            HashResults = new Dictionary<ulong, string>();
            methodInfoHashTable = new Dictionary<MethodInfo, ulong>();
            methodInfoStringBuilder = new StringBuilder();
        }

        private ulong HashMethodName(string name)
        {
            // todo : 함수를 구분하는 해시값이 64bit로 패킷 최적화가 필요해 보임
            HashSize mode = HashSize.VarIntEightBytes;

            if (mode == HashSize.VarIntTwoBytes)
                return name.GetStableHash16();
            if (mode == HashSize.VarIntFourBytes)
                return name.GetStableHash32();
            if (mode == HashSize.VarIntEightBytes)
                return name.GetStableHash64();

            return 0;
        }

        private ulong HashMethod(MethodInfo method)
        {
            if (methodInfoHashTable.ContainsKey(method))
            {
                return methodInfoHashTable[method];
            }
            else
            {
                ulong val = HashMethodName(GetHashableMethodSignature(method));

                methodInfoHashTable.Add(method, val);

                return val;
            }
        }

        private string GetHashableMethodSignature(MethodInfo method)
        {
            methodInfoStringBuilder.Length = 0;
            methodInfoStringBuilder.Append(method.Name);

            ParameterInfo[] parameters = method.GetParameters();

            for (int i = 0; i < parameters.Length; i++)
            {
                methodInfoStringBuilder.Append(parameters[i].ParameterType.Name);
            }

            return methodInfoStringBuilder.ToString();
        }

        private MethodInfo[] GetNetworkedBehaviorChildClassesMethods(Type type, List<MethodInfo> list = null)
        {
            if (list == null)
            {
                list = new List<MethodInfo>();
                list.AddRange(type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            }
            else
            {
                list.AddRange(type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance));
            }

            if (type.BaseType != null && type.BaseType != typeof(NetGameObject))
            {
                return GetNetworkedBehaviorChildClassesMethods(type.BaseType, list);
            }
            else
            {
                return list.ToArray();
            }
        }

        public void RemoveCacheAttributes()
        {
            if(CachedClientRpcs != null)
                CachedClientRpcs.Remove(this);
            if(CachedServerRpcs != null)
                CachedServerRpcs.Remove(this);
        }

        protected void CacheAttributes()
        {
            Type type = GetType();

            CachedClientRpcs[this] = new Dictionary<ulong, ClientRPC>();
            CachedServerRpcs[this] = new Dictionary<ulong, ServerRPC>();

            MethodInfo[] methods;
            if (Methods.ContainsKey(type)) methods = Methods[type];
            else
            {
                methods = GetNetworkedBehaviorChildClassesMethods(type);
                Methods.Add(type, methods);
            }

            for (int i = 0; i < methods.Length; i++)
            {
                if (methods[i].IsDefined(typeof(ServerRPC), true))
                {
                    ServerRPC[] attributes = (ServerRPC[])methods[i].GetCustomAttributes(typeof(ServerRPC), true);
                    if (attributes.Length > 1)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Having more than 1 ServerRPC attribute per method is not supported.");
                    }

                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(ulong) && parameters[1].ParameterType == typeof(NetBuffer) && methods[i].ReturnType == typeof(void))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i].Name);
                    }
                    else
                    {
                        if (methods[i].ReturnType != typeof(void)
                            //&& !SerializationHelper.IsTypeSupported(methods[i].ReturnType)
                            )
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Invalid return type of RPC. Has to be either void or RpcResponse<T> with a serializable type");
                        }

                        attributes[0].reflectionMethod = new ReflectionMethod(methods[i]);
                    }

                    ulong nameHash = HashMethodName(methods[i].Name);

                    if (HashResults.ContainsKey(nameHash) && HashResults[nameHash] != methods[i].Name)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{methods[i].Name}\" collides with the method \"{HashResults[nameHash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                    }
                    else if (!HashResults.ContainsKey(nameHash))
                    {
                        HashResults.Add(nameHash, methods[i].Name);
                    }
                    CachedServerRpcs[this].Add(nameHash, attributes[0]);


                    if (methods[i].GetParameters().Length > 0)
                    {
                        // Alloc justification: This is done only when first created. We are still allocing a whole NetworkedBehaviour. Allocing a string extra is NOT BAD
                        // As long as we dont alloc the string every RPC invoke. It's fine
                        string hashableMethodSignature = GetHashableMethodSignature(methods[i]);

                        ulong methodHash = HashMethodName(hashableMethodSignature);

                        if (HashResults.ContainsKey(methodHash) && HashResults[methodHash] != hashableMethodSignature)
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{hashableMethodSignature}\" collides with the method \"{HashResults[methodHash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                        }
                        else if (!HashResults.ContainsKey(methodHash))
                        {
                            HashResults.Add(methodHash, hashableMethodSignature);
                        }
                        CachedServerRpcs[this].Add(methodHash, attributes[0]);
                    }
                }

                if (methods[i].IsDefined(typeof(ClientRPC), true))
                {
                    ClientRPC[] attributes = (ClientRPC[])methods[i].GetCustomAttributes(typeof(ClientRPC), true);
                    if (attributes.Length > 1)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Having more than 1 ClientRPC attribute per method is not supported.");
                    }

                    ParameterInfo[] parameters = methods[i].GetParameters();
                    if (parameters.Length == 2 && parameters[0].ParameterType == typeof(ulong) && parameters[1].ParameterType == typeof(NetBuffer) && methods[i].ReturnType == typeof(void))
                    {
                        //use delegate
                        attributes[0].rpcDelegate = (RpcDelegate)Delegate.CreateDelegate(typeof(RpcDelegate), this, methods[i].Name);
                    }
                    else
                    {
                        if (methods[i].ReturnType != typeof(void)
                            //&& !SerializationHelper.IsTypeSupported(methods[i].ReturnType)
                            )
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogWarning("Invalid return type of RPC. Has to be either void or RpcResponse<T> with a serializable type");
                        }

                        attributes[0].reflectionMethod = new ReflectionMethod(methods[i]);
                    }


                    ulong nameHash = HashMethodName(methods[i].Name);

                    if (HashResults.ContainsKey(nameHash) && HashResults[nameHash] != methods[i].Name)
                    {
                        if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{methods[i].Name}\" collides with the method \"{HashResults[nameHash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                    }
                    else if (!HashResults.ContainsKey(nameHash))
                    {
                        HashResults.Add(nameHash, methods[i].Name);
                    }
                    CachedClientRpcs[this].Add(nameHash, attributes[0]);


                    if (methods[i].GetParameters().Length > 0)
                    {
                        // Alloc justification: This is done only when first created. We are still allocing a whole NetworkedBehaviour. Allocing a string extra is NOT BAD
                        // As long as we dont alloc the string every RPC invoke. It's fine
                        string hashableMethodSignature = GetHashableMethodSignature(methods[i]);

                        ulong methodHash = HashMethodName(hashableMethodSignature);

                        if (HashResults.ContainsKey(methodHash) && HashResults[methodHash] != hashableMethodSignature)
                        {
                            if (LogHelper.CurrentLogLevel <= LogLevel.Error) LogHelper.LogError($"Hash collision detected for RPC method. The method \"{hashableMethodSignature}\" collides with the method \"{HashResults[methodHash]}\". This can be solved by increasing the amount of bytes to use for hashing in the NetworkConfig or changing the name of one of the conflicting methods.");
                        }
                        else if (!HashResults.ContainsKey(methodHash))
                        {
                            HashResults.Add(methodHash, hashableMethodSignature);
                        }
                        CachedClientRpcs[this].Add(methodHash, attributes[0]);
                    }
                }
            }
        }

        public object OnRemoteServerRPC(ulong hash, int senderClientId, NetBuffer stream)
        {
            if (!CachedServerRpcs.ContainsKey(this) || !CachedServerRpcs[this].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning($"ServerRPC request method not found {hash}");
                return null;
            }

            return InvokeServerRPCLocal(hash, senderClientId, stream);
        }

        public object OnRemoteClientRPC(ulong hash, int senderClientId, NetBuffer stream)
        {
            if (!CachedClientRpcs.ContainsKey(this) || !CachedClientRpcs[this].ContainsKey(hash))
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("ClientRPC request method not found");
                return null;
            }

            return InvokeClientRPCLocal(hash, senderClientId, stream);
        }

        private object InvokeServerRPCLocal(ulong hash, int senderClientId, NetBuffer stream)
        {
            if (!CachedServerRpcs.ContainsKey(this) || !CachedServerRpcs[this].ContainsKey(hash))
                return null;

            ServerRPC rpc = CachedServerRpcs[this][hash];

            if (rpc.RequireOwnership && senderClientId != mNetworkId)
            {
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only owner can invoke ServerRPC that is marked to require ownership");
                return null;
            }


            if (rpc.reflectionMethod != null)
            {

                return rpc.reflectionMethod.Invoke(this, stream);
            }

            if (rpc.rpcDelegate != null)
            {
                rpc.rpcDelegate(senderClientId, stream);
            }

            return null;

        }

        private object InvokeClientRPCLocal(ulong hash, int senderClientId, NetBuffer stream)
        {
            if (!CachedClientRpcs.ContainsKey(this) || !CachedClientRpcs[this].ContainsKey(hash))
                return null;

            ClientRPC rpc = CachedClientRpcs[this][hash];

            if (rpc.reflectionMethod != null)
            {
                return rpc.reflectionMethod.Invoke(this, stream);
            }

            if (rpc.rpcDelegate != null)
            {
                rpc.rpcDelegate(senderClientId, stream);
            }

            return null;

        }

        //Technically boxed writes are not needed. But save LOC for the non performance sends.
        internal void SendServerRPCBoxed(ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {
            var writer = CreateRpcPacketClient(Engine.sInstance.ServerClientId);
            writer.Write(NetworkId);
            writer.Write(hash);

            //core.LogHelper.LogInfo($"SendServerRPCBoxed {hash}");

            for (int i = 0; i < parameters.Length; i++)
            {
                writer.WriteObjectPacked(parameters[i]);
            }

#if UNITY_EDITOR && USE_CLIENT_STATE_RECORD
            RecordRPC(hash, parameters);
#endif

            SendServerRPCPerformance(hash, writer, channel, security);
        }

        internal RpcResponse<T> SendServerRPCBoxedResponse<T>(ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {
            var writer = CreateRpcPacketClient(Engine.sInstance.ServerClientId);
            writer.Write(NetworkId);
            writer.Write(hash);

            //core.LogHelper.LogInfo($"SendServerRPCBoxed {hash}");

            for (int i = 0; i < parameters.Length; i++)
            {
                writer.WriteObjectPacked(parameters[i]);
            }

#if UNITY_EDITOR && USE_CLIENT_STATE_RECORD
            RecordRPC(hash, parameters);
#endif

            return SendServerRPCPerformanceResponse<T>(hash, writer, channel, security);

        }

        internal void SendClientRPCBoxedToClient(ulong hash, int clientId, string channel, SecuritySendFlags security, params object[] parameters)
        {
            var writer = CreateRpcPacketServer(clientId);
            writer.Write(NetworkId);
            writer.Write(hash);

            for (int i = 0; i < parameters.Length; i++)
            {
                writer.WriteObjectPacked(parameters[i]);
            }
            SendClientRPCPerformance(hash, clientId, writer, channel, security);

        }

        internal RpcResponse<T> SendClientRPCBoxedResponse<T>(ulong hash, int clientId, string channel, SecuritySendFlags security, params object[] parameters)
        {
            var writer = CreateRpcPacketServer(clientId);
            writer.Write(NetworkId);
            writer.Write(hash);

            for (int i = 0; i < parameters.Length; i++)
            {
                writer.WriteObjectPacked(parameters[i]);
            }

            return SendClientRPCPerformanceResponse<T>(hash, clientId, writer, channel, security);

        }

        internal void SendClientRPCBoxed(ulong hash, List<int> clientIds, string channel, SecuritySendFlags security, params object[] parameters)
        {
            SendClientRPCPerformance(hash, clientIds, channel, security, parameters);

        }

        internal void SendClientRPCBoxedToEveryoneExcept(int clientIdToIgnore, ulong hash, string channel, SecuritySendFlags security, params object[] parameters)
        {

        }

        internal void SendServerRPCPerformance(ulong hash, NetBuffer messageStream, string channel, SecuritySendFlags security)
        {
            //core.LogHelper.LogInfo($"SendServerRPCPerformance {hash}");

            if (!IsClient && IsRunning)
            {
                //We are ONLY a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server and host can invoke ServerRPC");
                return;
            }



            SendClient(Engine.sInstance.ServerClientId, messageStream);
        }

        internal RpcResponse<T> SendServerRPCPerformanceResponse<T>(ulong hash, NetBuffer messageStream, string channel, SecuritySendFlags security)
        {
            //core.LogHelper.LogInfo($"SendServerRPCPerformanceResponse {hash}");

            if (!IsClient && IsRunning)
            {
                //We are ONLY a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only server and host can invoke ServerRPC");
                return null;
            }

            SendClient(Engine.sInstance.ServerClientId, messageStream);

            return null;
        }

        internal void SendClientRPCPerformance(ulong hash, List<int> clientIds, string channel, SecuritySendFlags security, params object[] parameters)
        {
            //core.LogHelper.LogInfo($"SendClientRPCPerformance {hash}");

            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }

            for (int i = 0; i < clientIds.Count; i++)
            {


                var messageStream = CreateRpcPacketServer(clientIds[i]);
                messageStream.Write(NetworkId);
                messageStream.Write(hash);

                for (int j = 0; j < parameters.Length; j++)
                {
                    messageStream.WriteObjectPacked(parameters[j]);
                }

                SendServer(clientIds[i], messageStream);
            }
        }

        internal void SendClientRPCPerformance(ulong hash, NetBuffer messageStream, int clientIdToIgnore, string channel, SecuritySendFlags security)
        {
            //core.LogHelper.LogInfo($"SendClientRPCPerformance {hash}");

            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }


            SendServer(Engine.sInstance.ServerClientId, messageStream);
        }

        internal void SendClientRPCPerformance(ulong hash, int clientId, NetBuffer messageStream, string channel, SecuritySendFlags security)
        {
            //core.LogHelper.LogInfo($"SendClientRPCPerformance {hash}");

            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return;
            }

            SendServer(clientId, messageStream);
        }

        internal RpcResponse<T> SendClientRPCPerformanceResponse<T>(ulong hash, int clientId, NetBuffer messageStream, string channel, SecuritySendFlags security)
        {
            //core.LogHelper.LogInfo($"SendClientRPCPerformanceResponse {hash}");

            if (!IsServer && IsRunning)
            {
                //We are NOT a server.
                if (LogHelper.CurrentLogLevel <= LogLevel.Normal) LogHelper.LogWarning("Only clients and host can invoke ClientRPC");
                return null;
            }

            SendServer(clientId, messageStream);

            return null;
        }
#endregion

#if UNITY_EDITOR && USE_CLIENT_STATE_RECORD
        void RecordRPC(ulong hash, object[] parameters)
        {
            if (ACScrambleBattle.GetState().State == EACBattleState.PLAY)
            {
                System.IO.FileStream fs = new System.IO.FileStream($"client_record", System.IO.FileMode.Append, System.IO.FileAccess.Write);
                System.IO.BinaryWriter bw = new System.IO.BinaryWriter(fs);
                var buff = new NetBuffer();
                bw.Write((int)core.PacketType.kRPC);
                bw.Write(hash);
                for (int i = 0; i < parameters.Length; i++)
                {
                    buff.WriteObjectPacked(parameters[i]);
                }
                bw.Write(buff.LengthBits);
                bw.Write(buff.Data.Length);
                bw.Write(buff.Data);
                bw.Close();
                fs.Close();
            }
        }
#endif
    }
}
