using Lidgren.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

using uint16_t = System.UInt16;

namespace core
{
    public enum ErrorCode : byte
    {
        Success,
        Auth,
        Wait,
    }

    public enum PacketType : byte
    {
        kHello,
        kWelcome,
        kState,
        kInput,
        kStartPlay,
        kRPC,
        kReadyPlay,

        // PacketTypeLengthBits 초과하면 안됨
        MaxPacketType = 16,
    }

    public class NetworkManager
    {
        public static readonly int PacketTypeLengthBits = MathHelpers.countBits((int)PacketType.MaxPacketType - 1);

        /// <summary>
        /// Global instance of NetworkManagerServer
        /// </summary>
#if _USE_THREAD_STATIC
        [ThreadStatic]
#endif
        public static NetworkManager Instance;


        // 프레임당 최대 수신 패킷량
        public const int kMaxPacketsPerFrameCount = 3;
        //static readonly int kMaxBufferSize = 1500;



        protected NetPeer mNetPeer;

        WeightedTimedMovingAverage mBytesReceivedPerSecond;
        WeightedTimedMovingAverage mBytesSentPerSecond;


        int mBytesSentThisFrame;

        float mDropPacketChance;
        float mSimulatedLatency;

        class ReceivedPacket
        {
            public ReceivedPacket(float inReceivedTime, NetIncomingMessage inInputMemoryBitStream)
            {
                mReceivedTime = inReceivedTime;
                mPacketBuffer = inInputMemoryBitStream;
            }

            public float GetReceivedTime() { return mReceivedTime; }
            public NetIncomingMessage GetPacketBuffer() { return mPacketBuffer; }

            float mReceivedTime;
            NetIncomingMessage mPacketBuffer;
        };

        Queue<ReceivedPacket> mPacketQueue = new Queue<ReceivedPacket>();
        //Stack<ReceivedPacket> mPacketQueue = new Stack<ReceivedPacket>();

        protected Dictionary<int, NetGameObject>[] mNetworkIdToGameObjectMap = new Dictionary<int, NetGameObject>[] { new Dictionary<int, NetGameObject>(), };



        public NetworkManager()
        {
            mBytesSentThisFrame = 0;
            mDropPacketChance = 0.0f;
            mSimulatedLatency = 0.0f;

        }
        ~NetworkManager()
        {

        }

        public void Init(byte worldCount)
        {
            if (worldCount > 0)
            {
                mNetworkIdToGameObjectMap = new Dictionary<int, NetGameObject>[worldCount];
                for (int i = 0; i < worldCount; ++i)
                {
                    mNetworkIdToGameObjectMap[i] = new Dictionary<int, NetGameObject>();
                }
            }


            mBytesReceivedPerSecond = new WeightedTimedMovingAverage(1.0f);
            mBytesSentPerSecond = new WeightedTimedMovingAverage(1.0f);
        }

        protected bool CheckWorldId(byte world_id)
        {
            if(mNetworkIdToGameObjectMap.Length <= world_id)
            {
                return false;
            }
            return true;
        }

        public virtual bool ReadPacket(NetIncomingMessage inInputStream, System.Net.IPEndPoint inFromAddress) { return true; }
        public virtual void ProcessPacket() { }


        public virtual void ProcessInternalMessage(string msg) { }

        public virtual void OnConnected() { }

        public virtual void OnDisconnected(NetIncomingMessage inInputStream) { }

        public virtual float GetRoundTripTimeClientSide() { return 0.0f; }


        public void ProcessIncomingPackets()
        {
            ProcessQueuedPackets();

            ProcessPacket();

            UpdateBytesSentLastFrame();

        }

        string GetProtocol(NetIncomingMessage inInputStream)
        {
            return inInputStream.isTcp ? "TCP" : "UDP";
        }

        public void ProcessQueuedPackets()
        {
            if (mNetPeer == null)
                return;

            int totalReadByteCount = 0;

            NetIncomingMessage im;
            bool isRecycle = true;

            //LogHelper.LogInfo($"ProcessQueuedPackets local:{mNetPeer.Port}");

            while ((im = mNetPeer.ReadMessage()) != null)
            {
                isRecycle = true;
                // handle incoming message
                switch (im.MessageType)
                {
                    case NetIncomingMessageType.DebugMessage:
                    case NetIncomingMessageType.ErrorMessage:
                    case NetIncomingMessageType.WarningMessage:
                    case NetIncomingMessageType.VerboseDebugMessage:
                        string text = im.ReadString();
                        LogHelper.LogInfo(text);
                        break;

                    case NetIncomingMessageType.StatusChanged:
                        {
                            NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                            string reason = im.ReadString();
                            LogHelper.LogInfo($"UDP status {im.SenderConnection.m_remoteEndPoint} {status} {reason}");

                            if (status == NetConnectionStatus.Connected)
                            {
                                OnConnected();
                                //LogHelper.LogInfo("Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                OnDisconnected(im);
                            }

                            //UpdateConnectionsList();
                        }
                        break;
                    case NetIncomingMessageType.TcpStatusChanged:
                        {
                            NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                            string reason = im.ReadString();
                            LogHelper.LogInfo("TCP status " + im.SenderEndPoint + " " + status + ": " + reason);

                            if (status == NetConnectionStatus.Connected)
                            {
                                //LogHelper.LogInfo("Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
                                OnConnected();
                            }
                            else if (status == NetConnectionStatus.Disconnected)
                            {
                                OnDisconnected(im);
                            }

                            //UpdateConnectionsList();
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        //LogHelper.LogInfo($"Recv from : {im.SenderEndPoint}, bytes: {im.LengthBytes}, local:{mNetPeer.Port}, sessionID:{im.GetSessionId()}, protocol:{GetProtocol(im)}" );

                        totalReadByteCount += im.LengthBytes;

                        //if(im.isTcp)
                        isRecycle = ReadPacket(im, im.SenderEndPoint);

                        break;
                    case NetIncomingMessageType.UnconnectedData:
                        string msg = im.ReadString();
                        LogHelper.LogInfo(msg);
                        ProcessInternalMessage(msg);
                        break;
                    default:
                        LogHelper.LogInfo("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes " + im.DeliveryMethod + "|" + im.SequenceChannel);
                        break;
                }

                if(isRecycle)
                    mNetPeer.Recycle(im);
            }

            if (totalReadByteCount > 0)
            {
                mBytesReceivedPerSecond.UpdatePerSecond((float)(totalReadByteCount));
            }
        }

        public void UpdateBytesSentLastFrame()
        {
            if (mBytesSentThisFrame > 0)
            {
                mBytesSentPerSecond.UpdatePerSecond((float)(mBytesSentThisFrame));

                mBytesSentThisFrame = 0;
            }
        }

        public void AddToNetworkIdToGameObjectMap(NetGameObject inGameObject, byte inWorldId)
        {
            mNetworkIdToGameObjectMap[inWorldId][inGameObject.GetNetworkId()] = inGameObject;
        }

        public void RemoveFromNetworkIdToGameObjectMap(NetGameObject inGameObject, byte inWorldId)
        {
            mNetworkIdToGameObjectMap[inWorldId].Remove(inGameObject.GetNetworkId());
        }

        public WeightedTimedMovingAverage GetBytesReceivedPerSecond() { return mBytesReceivedPerSecond; }
        public WeightedTimedMovingAverage GetBytesSentPerSecond() { return mBytesSentPerSecond; }

        public void SetDropPacketChance(float inChance) { mDropPacketChance = inChance; }
        public void SetSimulatedLatency(float inLatency) { mSimulatedLatency = inLatency; }

        public NetGameObject GetGameObject(int inNetworkId, byte inWorldId)
        {
            NetGameObject gameObjectIt = null;
            if (mNetworkIdToGameObjectMap[inWorldId].TryGetValue(inNetworkId, out gameObjectIt) == true)
            {
                return gameObjectIt;
            }
            else
            {
                return null;
            }
        }


    }
}

