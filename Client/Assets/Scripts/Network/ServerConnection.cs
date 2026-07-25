using syncnet;
using System;
using System.Collections.Generic;
using UnityEngine;

// 서버와의 '연결 자체'만 담당한다: 소켓 수명(연결/자동 재접속), 하트비트(ping/pong),
// 송신과 메시지 id, 요청-응답 콜백 짝짓기, 수신 큐 접근.
//
// 게임 내용(액터/스킬/맵)은 전혀 모른다 — 받은 바이트를 어떻게 해석할지는 Session 이 라우팅한다.
// MonoBehaviour 가 아니라 Session 이 소유하는 순수 객체이고, 프레임 진행은 Tick() 으로 받는다.
public class ServerConnection
{
    // TcpConnection 은 Receiver 를 Component 로 요구하고 내부에서 Session 으로 캐스팅한다.
    // 그래서 소유자(Session) 컴포넌트를 그대로 넘겨받아 등록한다.
    private readonly Component receiver;
    // 주소는 접속 시점에 읽는다. 생성(Awake)이 GameManager 초기화보다 앞설 수 있기 때문이다.
    private readonly Func<string> serverAddress;

    private TcpConnection connection;
    private bool isConnected = false;

    // 자동 재접속
    private bool isReconnecting = false;
    private float reconnectDelay = 3f;
    private float lastReconnectTime = 0f;
    private int reconnectAttempts = 0;
    private int maxReconnectAttempts = 5;

    // 하트비트(ping/pong)
    private int seq = 1;
    private float pingInterval = 5f;
    private float lastPingTime = 0f;
    private float pingTimeout = 10f;
    private float lastPongTime = 0f;
    private bool pingEnabled = true;

    // 요청-응답 짝짓기. 보낸 메시지 id 로 응답 콜백을 찾아 1회 실행한다.
    private int lastMessageId = 0;
    private readonly Dictionary<int, Action<GameMessage>> responses = new Dictionary<int, Action<GameMessage>>();

    /// <summary>연결 성립(최초/재접속 공통). 로그인 등 접속 후 절차를 여기에 건다.</summary>
    public event Action Connected;

    /// <summary>연결 해제. 재접속 시도는 이 클래스가 알아서 한다.</summary>
    public event Action Disconnected;

    public bool IsConnected => isConnected;

    public ServerConnection(Component receiver, Func<string> serverAddress)
    {
        this.receiver = receiver;
        this.serverAddress = serverAddress;
    }

    public void Connect()
    {
        string address = serverAddress();
        Debug.Log($"서버 연결 시작: {address}");
        connection = new TcpConnection(core.NetworkHelper.CreateIPEndPoint(address));
        connection.Receiver = receiver;
        Debug.Log("TcpConnection 생성 완료, 연결 시도 중...");
        connection.Connect();
        Debug.Log("Connect() 호출 완료");
    }

    public int NextMessageId()
    {
        ++lastMessageId;
        if (lastMessageId <= 0)
            lastMessageId = 1;
        return lastMessageId;
    }

    /// <summary>메시지 전송. response 를 주면 같은 메시지 id 의 응답이 올 때 1회 호출된다.</summary>
    public void Send(byte[] msg, Action<GameMessage> response = null)
    {
        if (!isConnected)
        {
            Debug.LogWarning("서버에 연결되지 않았습니다. 메시지 전송을 건너뜁니다.");
            return;
        }

        // TcpConnection의 실제 연결 상태도 확인
        if (connection != null && !connection.IsConnected)
        {
            Debug.LogWarning("TcpConnection이 연결되지 않았습니다. 메시지 전송을 건너뜁니다.");
            return;
        }

        connection.SendBytes(MakeHeader(msg));
        connection.SendBytes(msg);
        if (response != null)
            responses.Add(lastMessageId, response);
    }

    /// <summary>수신 큐에서 항목 하나를 꺼낸다(byte[] 는 패킷, string 은 연결 이벤트).</summary>
    public bool TryDequeue(out object item)
    {
        item = null;
        return connection != null && connection.queue.TryDequeue(out item);
    }

    /// <summary>큐로 전달된 연결 이벤트("OnConnected"/"OnDisconnected") 처리.</summary>
    public void HandleQueuedEvent(string eventName)
    {
        Debug.Log($"이벤트 처리: {eventName}");
        switch (eventName)
        {
            case "OnConnected": OnConnected(); break;
            case "OnDisconnected": OnDisconnected(); break;
        }
    }

    /// <summary>응답 콜백 실행(요청 id 가 붙은 메시지만). 라우팅 뒤에 호출한다.</summary>
    public void CompleteResponse(GameMessage msg)
    {
        if (msg.Id > 0 && responses.ContainsKey(msg.Id))
        {
            responses[msg.Id](msg);
            responses.Remove(msg.Id);
        }
    }

    /// <summary>Pong 수신 — 하트비트 타이머를 갱신한다.</summary>
    public void OnPong()
    {
        lastPongTime = Time.time;
        Debug.Log("Pong 수신");
    }

    /// <summary>프레임 진행: 재접속 타이머와 하트비트(주기 전송/타임아웃)를 돌린다.</summary>
    public void Tick()
    {
        if (connection != null && connection.IsConnected != isConnected)
            Debug.Log($"연결 상태 변경 - isConnected: {isConnected} -> {connection.IsConnected}");

        if (isReconnecting && Time.time - lastReconnectTime >= reconnectDelay)
            AttemptReconnect();

        if (isConnected && pingEnabled)
        {
            if (Time.time - lastPingTime >= pingInterval)
                SendPing();

            CheckPingTimeout();
        }
    }

    /// <summary>최대 시도 횟수를 넘겨 자동 재접속이 멈춘 뒤 수동으로 다시 시도한다.</summary>
    public void ManualReconnect()
    {
        reconnectAttempts = 0;
        isReconnecting = false;
        StartReconnect();
    }

    private void OnConnected()
    {
        isConnected = true;
        Debug.Log($"서버에 연결되었습니다. TcpConnection.IsConnected: {connection?.IsConnected}");

        if (isReconnecting)
        {
            isReconnecting = false;
            reconnectAttempts = 0;
            Debug.Log("재접속 성공!");
        }

        lastPongTime = Time.time; // 연결 성공 시 하트비트 타이머 초기화
        Connected?.Invoke();
    }

    private void OnDisconnected()
    {
        bool wasConnected = isConnected;
        isConnected = false;
        Debug.Log(wasConnected ? "서버와의 연결이 해제되었습니다." : "서버 연결에 실패했습니다.");

        Disconnected?.Invoke();

        if (!isReconnecting && reconnectAttempts < maxReconnectAttempts)
            StartReconnect();
        else if (reconnectAttempts >= maxReconnectAttempts)
            Debug.LogError($"최대 재접속 시도 횟수({maxReconnectAttempts})에 도달했습니다. 수동으로 재접속해주세요.");
        else
            Debug.Log("이미 재접속 중입니다.");
    }

    private void StartReconnect()
    {
        if (isReconnecting) return;

        isReconnecting = true;
        reconnectAttempts++;
        lastReconnectTime = Time.time;

        Debug.Log($"자동 재접속 시도 {reconnectAttempts}/{maxReconnectAttempts} - {reconnectDelay}초 후 시도");

        // 기존 연결 정리
        if (connection != null)
        {
            connection.Dispose();
            connection = null;
        }
    }

    private void AttemptReconnect()
    {
        if (!isReconnecting) return;

        Debug.Log($"재접속 시도 중... ({reconnectAttempts}/{maxReconnectAttempts})");
        Connect();
        lastReconnectTime = Time.time; // 다음 시도까지 대기
    }

    private void SendPing()
    {
        if (!isConnected || !pingEnabled) return;

        lastPingTime = Time.time;
        byte[] body = PacketFactory.CreatePingMessage(seq++);
        connection.SendBytes(MakeHeader(body));
        connection.SendBytes(body);
        Debug.Log("Ping 전송");
    }

    private void CheckPingTimeout()
    {
        if (!isConnected || !pingEnabled || isReconnecting) return;

        float timeSinceLastPong = Time.time - lastPongTime;
        if (timeSinceLastPong > pingTimeout)
        {
            Debug.LogWarning($"Ping 타임아웃! 마지막 Pong 수신 후 {timeSinceLastPong:F1}초 경과");
            OnDisconnected(); // 연결 끊김으로 간주하고 재접속 시작
        }
    }

    // 2byte 헤더 = body 길이
    private byte[] MakeHeader(byte[] body)
    {
        return BitConverter.GetBytes((ushort)body.Length);
    }
}
