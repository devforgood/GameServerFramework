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

public class Session : MonoBehaviour
{
	int seq = 1;
	TcpConnection session;
	int message_count = 0;
	float lastSendTime = 0f;

	public Dictionary<int, GameObject> game_objects = new Dictionary<int, GameObject>();

	public Dictionary<int, Action<GameMessage>> responses = new Dictionary<int, Action<GameMessage>>();

	public long unixTimestampMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public int player_agnet_id = 0;
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

    void Start()
	{
		Application.runInBackground = true;
		startServer();
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
		TreeDebugView.Instance.Apply(treeDebugSync);
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
		if (session != null)
		{
			object item;
			while (session.queue.TryDequeue(out item))
			{
				if (item is byte[])
				{
					// 네트워크 데이터 처리
					OnReceive((byte[])item);
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
        SendMessage(PacketFactory.CreateLoginMessage(messageId), response => 
        { 
            if (response.MsgType == GameMessages.Login)
            {
                Login login = response.Msg<Login>().Value;
                if (response.Result == StatusCode.Success)
                {
                    Debug.Log("Login Success");
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
