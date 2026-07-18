using FlatBuffers;
using syncnet;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Session : MonoBehaviour
{
	// 게이트(Gate) 등에서 접근할 수 있도록 노출하는 인스턴스.
	// 씬 전환(게이트 이동) 중에도 네트워크 연결을 유지하기 위해 DontDestroyOnLoad 로 유지된다.
	public static Session Instance { get; private set; }

	private bool isChangingMap = false;   // 게이트 이동 요청 진행 중(중복 요청 방지)
	private bool isLoadingScene = false;  // 씬 로드 대기 중 — 이 동안 네트워크 동기화 처리를 멈춘다

	// 게이트 이동으로 막 도착하면 목적지 게이트 위치에 스폰되어 도착 즉시 트리거가 재발동한다.
	// "밖에서 안으로 들어온 경우"만 이동시키기 위해, 도착 직후에는 재이동을 억제하고
	// 플레이어가 게이트 밖으로 나가면(OnTriggerExit) 해제한다. Gate.cs 에서 참조/해제한다.
	public bool SuppressGateWarpUntilExit { get; set; } = false;

	int seq = 1;
	TcpConnection session;
	int message_count = 0;
	float lastSendTime = 0f;

	public Dictionary<int, GameObject> game_objects = new Dictionary<int, GameObject>();

	public Dictionary<int, Action<GameMessage>> responses = new Dictionary<int, Action<GameMessage>>();

	public long unixTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public int player_agnet_id = 0;
	// 최초 로그인 시 서버가 반환한 현재 맵 id 와 스폰 위치(캐릭터 생성/배치에 사용).
	public int loginMapId = 0;
	public Vector3 loginSpawnPos = Vector3.zero;

	// 신규 로그인 후 자동 스폰 대기 플래그. 씬 로드가 끝나면(또는 이미 대상 씬이면 즉시)
	// 서버가 준 스폰 위치(loginSpawnPos: 기본 스폰 마커 또는 이전 로그아웃 위치)로
	// AddAgent(Character) 를 1회 보낸다. 재접속 핸드오버는 서버가 기존 캐릭터를 넘겨주므로 제외.
	private bool pendingLoginSpawn = false;

	// 재접속 토큰(서버가 로그인 응답으로 내려준 플레이어 uuid). 재접속(세션 재연결 또는
	// 앱 재시작) 시 로그인 요청에 되돌려 보내면 서버가 유예 중이던 기존 캐릭터를 넘겨준다.
	// PlayerPrefs 에 영속 저장해 앱/플레이 재시작에도 유지한다.
	private string reconnectToken = "";
	private const string ReconnectTokenKey = "reconnectToken";
	private int last_message_id = 0;
	private bool isConnected = false;  // 연결 상태 추적

    // 자동 재접속 관련 변수
    private bool isReconnecting = false;  // 재접속 중인지 확인
    private float reconnectDelay = 3f;    // 재접속 대기 시간 (초)
    private float lastReconnectTime = 0f; // 마지막 재접속 시도 시간
    private int reconnectAttempts = 0;    // 재접속 시도 횟수
    private int maxReconnectAttempts = 5; // 최대 재접속 시도 횟수
    
    // Ping 기반 연결 상태 확인
    private float pingInterval = 5f;      // ping 전송 간격 (초)
    private float lastPingTime = 0f;      // 마지막 ping 전송 시간
    private float pingTimeout = 10f;      // ping 응답 대기 시간 (초)
    private float lastPongTime = 0f;      // 마지막 pong 수신 시간
    private bool pingEnabled = true;      // ping 활성화 여부

    // todo : 스킬 테이블 생성시 스킬별 지속 시간 설정
    private float skill_duration = 1f; // 스킬 지속 시간
	private float skill_height = 3f; // 스킬 점프 높이

    private Coroutine jumpCoroutine;
    public int nextMesssagetId()
	{
		++last_message_id;
		if (last_message_id <= 0)
		{
			last_message_id = 1;
		}
		return last_message_id;
	}

    void Awake()
	{
		// 씬 전환에도 연결을 유지하기 위한 영속 싱글톤. 새 씬에 중복 Session 이 있으면 자가 파괴한다.
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		// 재접속 토큰(uuid)을 영속 저장소에서 복원한다. 앱/플레이 재시작으로 Session 이 새로
		// 생성돼도, 유예 시간(서버 60초) 내라면 이 토큰으로 기존 캐릭터를 넘겨받을 수 있다.
		reconnectToken = PlayerPrefs.GetString(ReconnectTokenKey, "");
	}

    void Start()
	{
		Application.runInBackground = true;
		// BT 디버그 뷰(Tools/BT Debug Viewer)가 트리 정의를 요청할 때 사용할 송신 경로 등록
		TreeDebugRepository.DefinitionRequestSender = RequestTreeDebug;
		startServer();
	}

	void OnDestroy()
	{
		if (TreeDebugRepository.DefinitionRequestSender == (System.Action<long>)RequestTreeDebug)
			TreeDebugRepository.DefinitionRequestSender = null;
	}

	/// <summary>특정 몬스터의 BT 정의(+현재 상태)를 서버에 요청한다.</summary>
	public void RequestTreeDebug(long monsterId)
	{
		SendMessage(PacketFactory.CreateTreeDebugRequestMessage(monsterId));
	}

	public void startServer()
	{
		Debug.Log($"서버 연결 시작: {GameManager.Instance.server_address}");
		session = new TcpConnection(core.NetworkHelper.CreateIPEndPoint(GameManager.Instance.server_address));
		session.Receiver = this;
		Debug.Log("TcpConnection 생성 완료, 연결 시도 중...");
		session.Connect();
		Debug.Log("Connect() 호출 완료");
	}
	
	// 연결 완료 시 호출되는 메서드
	public void OnConnected()
	{
		Debug.Log("OnConnected() 메서드가 호출되었습니다.");
		isConnected = true;
		Debug.Log("서버에 연결되었습니다.");
		Debug.Log($"TcpConnection.IsConnected: {session?.IsConnected}");
		
		// 재접속 중이었다면 재접속 성공 처리
		if (isReconnecting)
		{
			OnReconnectSuccess();
		}
		
		// 연결 성공 시 ping 타이머 초기화
		lastPongTime = Time.time;
		
		// 연결 완료 시 자동 로그인
		Debug.Log("자동 로그인 시작...");
		Login();
	}
	
	// 연결 해제 시 호출되는 메서드
	public void OnDisconnected()
	{
		bool wasConnected = isConnected;
		isConnected = false;
		
		if (wasConnected)
		{
			Debug.Log("서버와의 연결이 해제되었습니다.");
		}
		else
		{
			Debug.Log("서버 연결에 실패했습니다.");
		}
		
		// 자동 재접속 시작 (이미 재접속 중이면 하지 않음)
		if (!isReconnecting && reconnectAttempts < maxReconnectAttempts)
		{
			StartReconnect();
		}
		else if (reconnectAttempts >= maxReconnectAttempts)
		{
			Debug.LogError($"최대 재접속 시도 횟수({maxReconnectAttempts})에 도달했습니다. 수동으로 재접속해주세요.");
		}
		else if (isReconnecting)
		{
			Debug.Log("이미 재접속 중입니다.");
		}
	}

	// 자동 재접속 시작
	private void StartReconnect()
	{
		if (isReconnecting) return;
		
		isReconnecting = true;
		reconnectAttempts++;
		lastReconnectTime = Time.time;
		
		Debug.Log($"자동 재접속 시도 {reconnectAttempts}/{maxReconnectAttempts} - {reconnectDelay}초 후 시도");
		
		// 기존 연결 정리
		if (session != null)
		{
			session.Dispose();
			session = null;
		}
	}
	
	// 재접속 시도
	private void AttemptReconnect()
	{
		if (!isReconnecting) return;
		
		Debug.Log($"재접속 시도 중... ({reconnectAttempts}/{maxReconnectAttempts})");
		startServer();
		
		// 재접속 시도 후 타이머 리셋 (다음 시도까지 대기)
		lastReconnectTime = Time.time;
	}
	
	// 재접속 성공 시 호출
	private void OnReconnectSuccess()
	{
		isReconnecting = false;
		reconnectAttempts = 0;
		lastPongTime = Time.time; // ping 타이머 리셋
		Debug.Log("재접속 성공!");
	}
	
	// 수동 재접속 (최대 시도 횟수 초과 시 사용)
	public void ManualReconnect()
	{
		reconnectAttempts = 0;
		isReconnecting = false;
		StartReconnect();
	}

	private byte[] MakeHeader(byte[] body)
	{
        // 2byte header + 2byte body length
        return BitConverter.GetBytes((ushort)body.Length);
	}

	public void SendPing(float deltaTime)
	{
		if (!isConnected) return;  // 연결되지 않은 경우 ping 전송하지 않음
		
		lastSendTime += deltaTime;
		if (lastSendTime >= 0.1f)
		{
			
			byte[] body = PacketFactory.CreatePingMessage(seq++);

			session.SendBytes(MakeHeader(body));
			session.SendBytes(body);

			lastSendTime = 0f;
		}
	}

	public void SendMessage(byte[] msg, Action<GameMessage> response = null)
	{
		if (!isConnected)
		{
			Debug.LogWarning("서버에 연결되지 않았습니다. 메시지 전송을 건너뜁니다.");
			return;
		}
		
		// TcpConnection의 실제 연결 상태도 확인
		if (session != null && !session.IsConnected)
		{
			Debug.LogWarning("TcpConnection이 연결되지 않았습니다. 메시지 전송을 건너뜁니다.");
			return;
		}
		
		session.SendBytes(MakeHeader(msg));
		session.SendBytes(msg);
		if (response != null)
		{
			responses.Add(last_message_id, response);
		}
	}

	void OnReceive(byte[] bytes)
	{
		var recv_msg = syncnet.GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
		switch (recv_msg.MsgType)
		{
			case syncnet.GameMessages.UpdateActorNotify:
				HandleUpdateActorNotify(recv_msg);
				break;
			case syncnet.GameMessages.UseSkill:
				HandleUseSkillNotify(recv_msg);
				break;
			case syncnet.GameMessages.Ping:
				// Ping 응답 처리
				HandlePong();
				break;
			case syncnet.GameMessages.TreeDebugSync:
				HandleTreeDebugSync(recv_msg);
				break;
		}

		if(recv_msg.Id > 0 && responses.ContainsKey(recv_msg.Id))
		{
			responses[recv_msg.Id](recv_msg);
			responses.Remove(recv_msg.Id);
		}
	}

	private void HandleUpdateActorNotify(syncnet.GameMessage recv_msg)
	{
		syncnet.UpdateActorNotify updateActorNotify = recv_msg.Msg<syncnet.UpdateActorNotify>().Value;
		//Debug.LogWarning($"HandleUpdateActorNotify: {updateActorNotify.ActorsLength} actors received");
		
		for (int i = 0; i < updateActorNotify.ActorsLength; ++i)	
		{
			var updatedActor = updateActorNotify.Actors(i).Value;
			var agent_id = updatedActor.AgentId;

			//Debug.LogWarning($"Processing Actor {agent_id}: Type={updatedActor.GameObjectType}, State={updatedActor.State?.State}, Health={updatedActor.Health?.Health}");

			// Pos가 null인 경우 처리
			Vector3 pos = new Vector3();
			GameObject game_object = null;
			Actor actor = null;
			if (!game_objects.TryGetValue(agent_id, out game_object))
			{
                if (updatedActor.Pos.HasValue)
                {
                    pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);
                }
                else
                {
                    Debug.LogWarning($"Actor {agent_id} has no position, skipping creation");
                }

					game_object = CreateGameObject(updatedActor.GameObjectType, pos, agent_id);
				if (game_object != null)
				{
					game_objects[agent_id] = game_object;
					actor = game_object.GetComponent<Actor>();
					Debug.LogWarning($"Created new {updatedActor.GameObjectType} object for agent {agent_id}");
				}
			}
			else
			{
				actor = game_object.GetComponent<Actor>();
                if (updatedActor.Pos.HasValue)
                {
                    pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);
                } else
				{
					pos = actor.pos; // Pos가 null인 경우 기존 위치 유지
                }
            }

			if (actor != null)
			{
				UpdateActorState(actor, updatedActor, pos);
			}
			else
			{
				Debug.LogError($"Failed to get Actor component for agent {agent_id}");
			}
		}

		// Debug lines 처리
		for (int i = 0; i < updateActorNotify.DebugsLength; ++i)
		{
			var debugInfo = updateActorNotify.Debugs(i).Value;
			
			// EndPos가 null인 경우 처리
			if (!debugInfo.EndPos.HasValue)
			{
				Debug.LogWarning($"Debug {i}: EndPos is null, skipping debug line");
				continue;
			}
			
			Vector3 pos;
			pos.x = debugInfo.EndPos.Value.X;
			pos.y = debugInfo.EndPos.Value.Y;
			pos.z = debugInfo.EndPos.Value.Z;
			var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);
		}
	}

	private void HandleTreeDebugSync(syncnet.GameMessage recv_msg)
	{
		syncnet.TreeDebugSync treeDebugSync = recv_msg.Msg<syncnet.TreeDebugSync>().Value;
		TreeDebugRepository.Apply(treeDebugSync);
	}

	private GameObject CreateGameObject(GameObjectType type, Vector3 pos, int agentId)
	{
		GameObject game_object = null;
		Actor actor = null;

		switch (type)
		{
			case GameObjectType.Monster:
				game_object = (GameObject)Instantiate(Resources.Load("Monster"), pos, Quaternion.identity);
				actor = game_object.GetComponent<Monster>();
				break;
			case GameObjectType.Character:
				game_object = (GameObject)Instantiate(Resources.Load("Character2"), pos, Quaternion.identity);
				actor = game_object.GetComponent<Character>();
				break;
			default:
				Debug.LogError("error game object type");
				return null;
		}

		if (actor != null)
		{
			actor.agnet_id = agentId;
		}

		return game_object;
	}

	private void UpdateActorState(Actor actor, syncnet.ActorInfo updatedActor, Vector3 pos)
	{
		actor.pos = pos;
		actor.input_locked = updatedActor.InputLocked;

		// 상태 업데이트
		if (updatedActor.State.HasValue)
		{
			var newState = updatedActor.State.Value.State;
			actor.UpdateState(actor.gameObject, newState);
		}
		
		// HP 업데이트
		if (updatedActor.Health.HasValue)
		{
            int oldHealth = actor.health;
            int newHealth = updatedActor.Health.Value.Health;
            if (oldHealth > newHealth)
            {
                Debug.LogWarning($"=== DAMAGE EVENT === Session: Actor {actor.agnet_id} ({actor.gameObject.name}) health: {oldHealth} -> {newHealth}");
            }
            actor.UpdateHealth(updatedActor.Health.Value.Health);
		}

		// 몬스터 특별 처리
		if (updatedActor.GameObjectType == GameObjectType.Monster)
		{
			// 몬스터 상태에 따른 시각적 업데이트
			if (updatedActor.State.HasValue)
			{
				UpdateMonsterVisuals(actor.gameObject, updatedActor.State.Value.State);
			}
		}
	}



	private void UpdateMonsterVisuals(GameObject monster, AIState state)
	{
		var renderer = monster.GetComponent<MeshRenderer>();
		if (renderer != null)
		{
			switch (state)
			{
				case AIState.Detect:
					renderer.material.color = Color.red;
					break;
				case AIState.Patrol:
					renderer.material.color = Color.white;
					break;
				case AIState.Attack:
					renderer.material.color = Color.blue;
					break;
			}
		}
	}

	private void HandleUseSkillNotify(syncnet.GameMessage recv_msg)
	{
		syncnet.UseSkill useSkill = recv_msg.Msg<syncnet.UseSkill>().Value;
		
		// Pos가 null인 경우 처리
		if (!useSkill.Pos.HasValue)
		{
			Debug.LogWarning("UseSkill: Pos is null, skipping skill effect");
			return;
		}
		
		var pos = new Vector3(useSkill.Pos.Value.X, useSkill.Pos.Value.Y, useSkill.Pos.Value.Z);
		var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);

		var target_agent_id = useSkill.Id;
		var remote_player_skill_duration = skill_duration - (unixTimestampMs - useSkill.Timestamp) / 1000f;

		if (game_objects.TryGetValue(target_agent_id, out GameObject game_object))
		{
			jumpCoroutine = StartCoroutine(JumpToPosition(game_object, game_object.transform.position, pos, remote_player_skill_duration, skill_height, useSkill.Timestamp));
		}
	}

	void Update()
	{
		// 연결 상태 디버깅 (연결 상태가 변경될 때만 출력)
		if (session != null && session.IsConnected != isConnected)
		{
			Debug.Log($"연결 상태 변경 - isConnected: {isConnected} -> {session.IsConnected}");
			// OnConnected에서 로그인을 처리하므로 여기서는 하지 않음
		}
		
		// 자동 재접속 처리
		if (isReconnecting && Time.time - lastReconnectTime >= reconnectDelay)
		{
			AttemptReconnect();
		}
		
		// Ping 기반 연결 상태 확인
		if (isConnected && pingEnabled)
		{
			// Ping 전송 (주기적)
			if (Time.time - lastPingTime >= pingInterval)
			{
				SendPing();
			}
			
			// Ping 타임아웃 확인
			CheckPingTimeout();
		}
		
		// 통합 큐 처리 (네트워크 데이터 + 이벤트)
		// 씬 로드 대기 중(isLoadingScene)에는 처리하지 않고 큐에 남겨, 씬 로드 완료 후 새 씬에서 처리한다.
		if (session != null && !isLoadingScene)
		{
			object item;
			while (session.queue.TryDequeue(out item))
			{
				if (item is byte[])
				{
					// 네트워크 데이터 처리
					OnReceive((byte[])item);

					// EnterGate 응답 처리로 씬 로드가 시작되면, 남은 메시지(새 맵 동기화 등)는
					// 씬 로드 완료 후 처리하도록 이번 프레임 처리를 중단한다.
					if (isLoadingScene)
						break;
				}
				else if (item is string)
				{
					// 이벤트 처리
					string eventName = (string)item;
					Debug.Log($"이벤트 처리: {eventName}");
					switch (eventName)
					{
						case "OnConnected":
							OnConnected();
							break;
						case "OnDisconnected":
							OnDisconnected();
							break;
					}
				}
			}
		}
		
		//SendPing(Time.deltaTime);

		foreach (var game_object in game_objects.Values)
		{
			try
			{
				var actor = game_object.GetComponent<Actor>();
				if (actor.input_locked)
				{
                    //Debug.Log($"Actor {actor.agnet_id} input is locked, skipping position update.");
                    continue;
				}

                float lerpSpeed = 15f; // 원하는 값으로 조정 (5~15 정도가 적당함)
    //            float threshold = 1f; // 원하는 값으로 조정 (1 정도가 적당함)
    //            float dist = Vector3.Distance(game_object.transform.position, actor.pos);
				//if (dist > threshold)
				//{
				//	Debug.Log($"Update Actor: {actor.agnet_id}, pos({game_object.transform.position.x}, {game_object.transform.position.y}, {game_object.transform.position.z}) -> ({actor.pos.x}, {actor.pos.y}, {actor.pos.z}) dist: {dist}");
				//	game_object.transform.position = actor.pos;
				//}
				//else
					game_object.transform.position = Vector3.Lerp(game_object.transform.position, actor.pos, Time.deltaTime * lerpSpeed);
			}
			catch
			{

			}
		}
	}

    public void AddAgent(int agent_id, Vector3 pos, GameObjectType type)
    {
        int messageId = nextMesssagetId();
        SendMessage(PacketFactory.CreateAddAgentMessage(messageId, pos, type), response =>
        {
            if (response.MsgType == GameMessages.AddAgent)
            {
                AddAgent addAgent = response.Msg<AddAgent>().Value;
                if (response.Result == StatusCode.Success)
                {
                    Debug.Log("AddAgent Success");
                    if(addAgent.GameObjectType == (int)GameObjectType.Character)
                    {
                        player_agnet_id = addAgent.AgentId;
                        Debug.Log($"Player Agent ID: {player_agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
                    }
                }
                else
                {
                    Debug.Log("AddAgent Fail");
                }
            }
            else
            {
                Debug.Log("AddAgent Error");
            }
        });
    }

    public void RemoveAgent(int agentId)
    {
        SendMessage(PacketFactory.CreateRemoveAgentMessage(agentId));
    }

    public void SetMoveTarget(int agentId, Vector3 pos)
    {
        Debug.Log($"SetMoveTarget agent_id: {agentId}, pos({pos.x}, {pos.y}, {pos.z}) ");
        SendMessage(PacketFactory.CreateSetMoveTargetMessage(agentId, pos));
    }

    public void SetRaycast(Vector3 pos)
    {
        Debug.Log($"SetRaycast pos({pos.x}, {pos.y}, {pos.z}) ");
        SendMessage(PacketFactory.CreateSetRaycastMessage(pos));
    }

    /// <summary>
    /// 게이트 이동을 서버에 요청한다. 성공 응답 시 목적지 맵 씬을 로드한다.
    /// (씬 단위 이동. 프리팹 단위 로드는 추후 최적화로 진행 예정.)
    /// </summary>
    public void EnterGate(int mapId, int gateId, string sceneName)
    {
        if (isChangingMap)
        {
            Debug.Log("EnterGate ignored: already changing map.");
            return;
        }
        isChangingMap = true;

        int messageId = nextMesssagetId();
        Debug.Log($"EnterGate request mapId:{mapId}, gateId:{gateId}, scene:{sceneName}");
        SendMessage(PacketFactory.CreateEnterGateMessage(messageId, mapId, gateId), response =>
        {
            if (response.MsgType == GameMessages.EnterGate && response.Result == StatusCode.Success)
            {
                EnterGate enterGate = response.Msg<EnterGate>().Value;

                // 서버가 목적지 맵에 캐릭터를 재생성하면서 부여한 새 agent id 로 갱신한다.
                player_agnet_id = enterGate.AgentId;

                Vector3 destPos = Vector3.zero;
                if (enterGate.Pos.HasValue)
                    destPos = new Vector3(enterGate.Pos.Value.X, enterGate.Pos.Value.Y, enterGate.Pos.Value.Z);

                Debug.Log($"EnterGate Success. new agentId:{player_agnet_id}, pos({destPos.x},{destPos.y},{destPos.z})");

                // 씬 로드 동안 네트워크 동기화 처리를 멈춘다. 서버는 응답 직후 새 맵 상태(SendStateTo)를
                // 보내는데, 씬 로드 전에 처리하면 새로 만든 액터가 씬 전환으로 파괴되기 때문이다.
                // 큐에 남겨 두었다가 씬 로드 완료(OnGateSceneLoaded) 후 처리한다.
                LoadMapScene(sceneName);
            }
            else
            {
                Debug.Log("EnterGate Fail");
                isChangingMap = false;
            }
        });
    }

    /// <summary>
    /// 맵 씬을 로드한다. 로드가 끝날 때까지 네트워크 동기화 처리를 멈춰(OnGateSceneLoaded 에서 재개),
    /// 로드 전에 도착한 새 맵 상태가 씬 전환으로 파괴되지 않게 한다.
    /// </summary>
    private void LoadMapScene(string sceneName)
    {
        // Build Settings 에 등록되지 않은 씬은 로드할 수 없다. 강제로 LoadScene 하면 예외가 나므로
        // 방어적으로 확인하고, 불가능하면 현재 씬을 유지한 채 네트워크 처리를 계속한다.
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings (File > Build Settings 에 추가 필요). 씬 로드를 건너뜁니다.");
            isLoadingScene = false;
            isChangingMap = false;
            // 로그인 자동 스폰 대기 중이었다면 현재 씬에서라도 스폰한다(서버 위치가 권위).
            TrySpawnLoginCharacter();
            return;
        }

        isLoadingScene = true;
        SceneManager.sceneLoaded += OnGateSceneLoaded;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 게이트 이동으로 인한 씬 로드 완료 콜백. 씬 전환으로 파괴된 이전 액터 참조를 정리하고
    /// 네트워크 동기화 처리를 재개한다. 대기 중이던 새 맵 상태(UpdateActorNotify)가 새 씬에서 액터를 생성한다.
    /// </summary>
    private void OnGateSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnGateSceneLoaded;

        // 씬 로드로 파괴된 이전 맵의 액터 GameObject 참조 정리(딕셔너리에는 파괴된 참조가 남아 있다).
        game_objects.Clear();

        isLoadingScene = false;
        isChangingMap = false;

        // 도착 캐릭터는 목적지 게이트 위치에 스폰되어 도착 즉시 트리거가 재발동한다.
        // 밖으로 걸어 나가기 전까지는 재이동을 억제한다(무한 왕복 방지).
        SuppressGateWarpUntilExit = true;

        Debug.Log($"Gate scene loaded: {scene.name}");

        // 신규 로그인으로 인한 씬 로드였다면 서버가 준 스폰 위치로 캐릭터를 자동 생성한다.
        // (게이트 이동/재접속 핸드오버는 서버가 캐릭터를 만들어 주므로 플래그가 꺼져 있다.)
        TrySpawnLoginCharacter();
    }

    /// <summary>
    /// 신규 로그인 자동 스폰: 서버가 Login 응답으로 준 위치(기본 스폰 마커 또는 이전 로그아웃
    /// 위치)로 AddAgent(Character) 를 1회 보낸다. 대기 플래그가 없으면 아무것도 하지 않는다.
    /// </summary>
    private void TrySpawnLoginCharacter()
    {
        if (!pendingLoginSpawn)
            return;
        pendingLoginSpawn = false;

        Debug.Log($"Auto spawn character at login pos({loginSpawnPos.x},{loginSpawnPos.y},{loginSpawnPos.z})");
        AddAgent(0, loginSpawnPos, GameObjectType.Character);
    }

    public void UseSkill(int skillId, Vector3 pos, int type)
    {
        var timestamp = unixTimestampMs;
        Debug.Log($"UseSkill agent_id: {player_agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) timestamp({timestamp})");

        GameObject game_object = null;
        if (game_objects.TryGetValue(player_agnet_id, out game_object) == false)
        {
            Debug.LogError("Player agent not found in game_objects dictionary.");
            return;
        }
        var actor = game_object.GetComponent<Actor>();
        if (actor == null)
        {
            Debug.LogError("Actor component not found on player agent GameObject.");
            return;
        }
        if(actor.input_locked == true)
        {
            Debug.Log("Player input is locked, cannot use skill.");
            return;
        }
		Gamedata.Skill resSkill = null;
		if(!GameManager.Instance.resource.Skills.TryGetValue(skillId, out resSkill))
		{
			Debug.LogError($"Skill with ID {skillId} not found in resource skills.");
			return;
        }



        SendMessage(PacketFactory.CreateUseSkillMessage(skillId, player_agnet_id, pos, type, timestamp));
		if (resSkill.code_name == "JumpSkill")
		{
			jumpCoroutine = StartCoroutine(JumpToPosition(game_object, game_object.transform.position, pos, skill_duration, skill_height, timestamp));
		}
    }

    public void Login()
    {
        int messageId = nextMesssagetId();
        // 보유한 재접속 토큰(uuid)을 함께 보낸다. 최초 로그인이면 빈 문자열이라 신규 로그인으로
        // 처리되고, 재접속이면 서버가 이 토큰으로 유예 중이던 기존 캐릭터를 넘겨준다.
        SendMessage(PacketFactory.CreateLoginMessage(messageId, reconnectToken), response =>
        {
            if (response.MsgType == GameMessages.Login)
            {
                Login login = response.Msg<Login>().Value;
                if (response.Result == StatusCode.Success)
                {
                    // 서버가 반환한 현재 맵 id 와 스폰 위치를 저장한다.
                    loginMapId = login.MapId;
                    if (login.Pos.HasValue)
                        loginSpawnPos = new Vector3(login.Pos.Value.X, login.Pos.Value.Y, login.Pos.Value.Z);

                    // 재접속 토큰(플레이어 uuid) 저장. 이후 재접속 로그인 요청에 되돌려 보낸다.
                    // PlayerPrefs 에도 저장해 앱/플레이 재시작에도 유지되게 한다.
                    if (!string.IsNullOrEmpty(login.Uuid))
                    {
                        reconnectToken = login.Uuid;
                        PlayerPrefs.SetString(ReconnectTokenKey, reconnectToken);
                        PlayerPrefs.Save();
                    }

                    // AgentId 가 0 이 아니면 재접속: 서버가 유지하던 기존 캐릭터를 넘겨받는다.
                    // AddAgent(신규 스폰)를 하지 않고, 그 agentId 를 채택한 뒤 대상 맵 씬을
                    // (재)로드한다. 로드 완료 후 서버가 보낸 상태 동기화(SendStateTo)가 큐에서
                    // 처리되며 기존 캐릭터가 재생성된다. (게이트 이동과 동일한 씬 로드 파이프라인)
                    if (login.AgentId != 0)
                    {
                        player_agnet_id = login.AgentId;
                        Debug.Log($"Reconnect handover. agentId:{player_agnet_id}, mapId:{loginMapId}, pos({loginSpawnPos.x},{loginSpawnPos.y},{loginSpawnPos.z})");

                        Gamedata.Map rmap;
                        if (GameManager.Instance.resource.Maps.TryGetValue(loginMapId, out rmap)
                            && !string.IsNullOrEmpty(rmap.scene))
                        {
                            // 같은 씬이어도 재로드해 이전(정지된) 액터/참조를 초기화한다.
                            LoadMapScene(rmap.scene);
                        }
                        return;
                    }

                    Debug.Log($"Login Success. mapId:{loginMapId}, pos({loginSpawnPos.x},{loginSpawnPos.y},{loginSpawnPos.z})");

                    // 씬 준비가 끝나면 서버가 준 스폰 위치로 캐릭터를 자동 생성한다.
                    pendingLoginSpawn = true;

                    // 맵 데이터에 연동된 씬 정보로 해당 맵 씬을 로드한다(현재 씬과 다를 때만).
                    Gamedata.Map map;
                    if (GameManager.Instance.resource.Maps.TryGetValue(loginMapId, out map)
                        && !string.IsNullOrEmpty(map.scene)
                        && SceneManager.GetActiveScene().name != map.scene)
                    {
                        LoadMapScene(map.scene); // 스폰은 OnGateSceneLoaded 에서 수행
                    }
                    else
                    {
                        // 이미 대상 씬에 있으면 즉시 스폰한다.
                        TrySpawnLoginCharacter();
                    }
                }
                else
                {
                    Debug.Log("Login Fail");
                }
            }
            else
            {
                Debug.Log("Login Error");
            }
        });
    }

    private IEnumerator JumpToPosition(GameObject game_object, Vector3 start, Vector3 end, float duration, float height, long timestamp)
    {
        float time = 0;
        float dropPoint = 0.7f; // 상승 구간 비율 (0~1)
        float fallDuration = duration * (1f - dropPoint); // 하강 구간 시간
        Vector3 lastPos = start;

        var actor = game_object.GetComponent<Actor>();
        actor.input_locked = true;

        while (time < duration)
        {
            float t = time / duration;
            float yOffset;

            if (t < dropPoint)
            {
                // 천천히 상승 (곡선 조정 가능)
                yOffset = Mathf.Lerp(0, height, t / dropPoint);
            }
            else
            {
                // 완만하게 하강 (선형 하강)
                float fallT = (t - dropPoint) / (1f - dropPoint); // 0~1
                yOffset = Mathf.Lerp(height, 0, fallT);
            }

            Vector3 pos = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            game_object.transform.position = pos;
            lastPos = pos;
            time += Time.deltaTime;
            yield return null;
        }
        // 마지막 위치는 착지점
        game_object.transform.position = end;
        Debug.Log($"JumpToPosition End: {game_object.name}, pos({end.x}, {end.y}, {end.z}), timestamp({timestamp})");
    }

    // Ping 전송
    private void SendPing()
    {
        if (!isConnected || !pingEnabled) return;
        
        lastPingTime = Time.time;
        byte[] body = PacketFactory.CreatePingMessage(seq++);
        session.SendBytes(MakeHeader(body));
        session.SendBytes(body);
        Debug.Log("Ping 전송");
    }
    
    // Pong 수신 처리
    private void HandlePong()
    {
        lastPongTime = Time.time;
        Debug.Log("Pong 수신");
    }
    
    // Ping 타임아웃 확인
    private void CheckPingTimeout()
    {
        if (!isConnected || !pingEnabled || isReconnecting) return;
        
        float timeSinceLastPong = Time.time - lastPongTime;
        if (timeSinceLastPong > pingTimeout)
        {
            Debug.LogWarning($"Ping 타임아웃! 마지막 Pong 수신 후 {timeSinceLastPong:F1}초 경과");
            // 연결 끊김으로 간주하고 재접속 시작
            OnDisconnected();
        }
    }
}
