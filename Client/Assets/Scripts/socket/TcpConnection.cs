using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

/// <summary>
///     Represents a connection that uses the TCP protocol.
/// </summary>
/// <inheritdoc />
public class TcpConnection : IDisposable
{
    public const int HEADER_SIZE = 2;
    /// <summary>
    ///     The socket we're managing.
    /// </summary>
    Socket socket;

    /// <summary>
    ///     Lock for the socket.
    /// </summary>
    System.Object socketLock = new System.Object();


    IPEndPoint RemoteEndPoint;
    public Component Receiver;

    public ConcurrentQueue<object> queue = new ConcurrentQueue<object>();
    
    // 연결 상태 추적
    private bool isConnected = false;
    public bool IsConnected => isConnected;

    /// <summary>
    ///     Creates a new TCP connection.
    /// </summary>
    /// <param name="remoteEndPoint">A <see cref="NetworkEndPoint"/> to connect to.</param>
    public TcpConnection(IPEndPoint remoteEndPoint)
    {
        lock (socketLock)
        {

            //Create a socket
            if (remoteEndPoint.AddressFamily != AddressFamily.InterNetworkV6)
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
            else
            {
                if (!Socket.OSSupportsIPv6)
                    throw new Exception("IPV6 not supported!");

                socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, System.Net.Sockets.ProtocolType.Tcp);
                socket.SetSocketOption(SocketOptionLevel.IPv6, (SocketOptionName)27, false);
            }

            socket.NoDelay = true;
            RemoteEndPoint = remoteEndPoint;
        }
    }

    /// <inheritdoc />
    public void Connect(byte[] bytes = null, int timeout = 5000)
    {
        lock (socketLock)
        {
            //Connect

            try
            {
                IAsyncResult result = socket.BeginConnect(RemoteEndPoint, new AsyncCallback((IAsyncResult ar) =>
                {
                    // Retrieve the socket from the state object.  
                    TcpConnection tcpConnection = (TcpConnection)ar.AsyncState;

                    try
                    {
                        // Complete the connection.  
                        tcpConnection.socket.EndConnect(ar);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"An exception occured while EndConnect {e}");
                        
                        // 연결 실패 시에도 OnDisconnected 이벤트 발생
                        if (tcpConnection.Receiver != null)
                        {
                            var session = tcpConnection.Receiver as Session;
                            if (session != null)
                            {
                                Debug.Log("연결 실패 이벤트를 큐에 추가합니다.");
                                tcpConnection.queue.Enqueue("OnDisconnected");
                            }
                        }
                        return;
                    }

                    Debug.Log($"Socket connected to {tcpConnection.socket.RemoteEndPoint.ToString()}");

                    // 연결 상태 설정
                    tcpConnection.isConnected = true;

                    // 연결 완료 이벤트 호출
                    if (tcpConnection.Receiver != null)
                    {
                        // Unity의 메인 스레드에서 실행하기 위해 통합 큐 사용
                        var session = tcpConnection.Receiver as Session;
                        if (session != null)
                        {
                            Debug.Log("연결 완료 이벤트를 큐에 추가합니다.");
                            tcpConnection.queue.Enqueue("OnConnected");
                        }
                        else
                        {
                            Debug.LogError("Receiver가 Session 타입이 아닙니다.");
                        }
                    }
                    else
                    {
                        Debug.LogError("Receiver가 null입니다.");
                    }

                    //Start receiving data
                    try
                    {
                        StartWaitingForHeader(BodyReadCallback);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"An exception occured while initiating the first receive operation. {e}");
                        return;
                    }

                }), this);
            }
            catch (Exception e)
            {
                throw new Exception("Could not connect as an exception occured.", e);
            }


        }
    }

    /// <inheritdoc />
    /// <remarks>
    ///     <include file="DocInclude/common.xml" path="docs/item[@name='Connection_SendBytes_General']/*" />
    ///     <para>
    ///         The sendOption parameter is ignored by the TcpConnection as TCP only supports FragmentedReliable 
    ///         communication, specifying anything else will have no effect.
    ///     </para>
    /// </remarks>
    public void SendBytes(byte[] bytes)
    {
        //Write the bytes to the socket
        lock (socketLock)
        {
            try
            {
                socket.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, null, null);
            }
            catch (Exception e)
            {
                HandleDisconnect(e);
                throw e;
            }
        }
    }

    /// <summary>
    ///     Called when a 4 byte header has been received.
    /// </summary>
    /// <param name="bytes">The 4 header bytes read.</param>
    /// <param name="callback">The callback to invoke when the body has been received.</param>
    void HeaderReadCallback(byte[] bytes, Action<byte[]> callback)
    {
        //Get length 
        int length = GetLengthFromBytes(bytes);

        //Begin receiving the body
        try
        {
            StartWaitingForBytes(length, callback);
        }
        catch (Exception e)
        {
            HandleDisconnect(new Exception($"An exception occured while initiating a body receive operation. {e}"));
        }
    }

    /// <summary>
    ///     Callback for when a body has been read.
    /// </summary>
    /// <param name="bytes">The data bytes received by the connection.</param>
    void BodyReadCallback(byte[] bytes)
    {
        //Begin receiving from the start
        try
        {
            StartWaitingForHeader(BodyReadCallback);
        }
        catch (Exception e)
        {
            HandleDisconnect(new Exception($"An exception occured while initiating a header receive operation. {e}"));
        }


        //Fire DataReceived event
        queue.Enqueue(bytes);
    }


    /// <summary>
    ///     Starts this connections waiting for the header.
    /// </summary>
    /// <param name="callback">The callback to invoke when the body has been read.</param>
    void StartWaitingForHeader(Action<byte[]> callback)
    {
        StartWaitingForBytes(HEADER_SIZE, (bytes) => HeaderReadCallback(bytes, callback));
    }

    /// <summary>
    ///     Waits for the specified amount of bytes to be received.
    /// </summary>
    /// <param name="length">The number of bytes to receive.</param>
    /// <param name="callback">The callback </param>
    void StartWaitingForBytes(int length, Action<byte[]> callback)
    {
        StateObject state = new StateObject(length, callback);

        StartWaitingForChunk(state);
    }

    /// <summary>
    ///     Waits for the next chunk of data from this socket.
    /// </summary>
    /// <param name="state">The StateObject for the receive operation.</param>
    void StartWaitingForChunk(StateObject state)
    {
        lock (socketLock)
        {
            socket.BeginReceive(state.buffer, state.totalBytesReceived, state.buffer.Length - state.totalBytesReceived, SocketFlags.None, ChunkReadCallback, state);
        }
    }

    /// <summary>
    ///     Called when a chunk has been read.
    /// </summary>
    /// <param name="result"></param>
    void ChunkReadCallback(IAsyncResult result)
    {
        int bytesReceived;

        //End the receive operation
        try
        {
            lock (socketLock)
                bytesReceived = socket.EndReceive(result);
        }
        catch (ObjectDisposedException)
        {
            //If the socket's been disposed then we can just end there.
            return;
        }
        catch (Exception e)
        {
            HandleDisconnect(new Exception($"An exception occured while completing a chunk read operation. {e}"));
            return;
        }

        StateObject state = (StateObject)result.AsyncState;

        state.totalBytesReceived += bytesReceived;      //TODO threading issues on state?

        //Exit if receive nothing
        if (bytesReceived == 0)
        {
            HandleDisconnect();
            return;
        }

        //If we need to receive more then wait for more, else process it.
        if (state.totalBytesReceived < state.buffer.Length)
        {
            try
            {
                StartWaitingForChunk(state);
            }
            catch (Exception e)
            {
                HandleDisconnect(new Exception($"An exception occured while initiating a chunk receive operation. {e}"));
                return;
            }
        }
        else
            state.callback.Invoke(state.buffer);
    }

    /// <summary>
    ///     Called when the socket has been disconnected at the remote host.
    /// </summary>
    /// <param name="e">The exception if one was the cause.</param>
    void HandleDisconnect(Exception e = null)
    {
        Debug.LogError($"HandleDisconnect {e}");
        
        // 연결 상태 설정
        isConnected = false;
        
        // 연결 해제 이벤트 호출
        if (Receiver != null)
        {
            var session = Receiver as Session;
            if (session != null)
            {
                Debug.Log("연결 해제 이벤트를 큐에 추가합니다.");
                queue.Enqueue("OnDisconnected");
            }
        }
    }



    /// <summary>
    ///     Returns the length from a length header.
    /// </summary>
    /// <param name="bytes">The bytes received.</param>
    /// <returns>The number of bytes.</returns>
    static int GetLengthFromBytes(byte[] bytes)
    {
        if (bytes.Length < HEADER_SIZE)
            throw new IndexOutOfRangeException("Not enough bytes passed to calculate length.");

        return BitConverter.ToInt16(bytes, 0);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (socket.Connected)
            socket.Shutdown(SocketShutdown.Send);
        socket.Close();
    }
}

