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

    // todo : 스킬 테이블 생성시 스킬별 지속 시간 설정
    private float skill_duration = 1f; // 스킬 지속 시간
	private float skill_height = 3f; // 스킬 점프 높이

    private Coroutine jumpCoroutine;
	private bool isCasting = false;
    public int nextMesssagetId()
	{
		++last_message_id;
		if (last_message_id <= 0)
		{
			last_message_id = 1;
		}
		return last_message_id;
	}

    [Header("Add Agent Event")]
	[SerializeField] private SessionChannelSO _OnAddAgent = default;

	[Header("Remove Agent Event Event")]
	[SerializeField] private SessionChannelSO _OnRemoveAgent = default;

	[Header("Set Move Target Event")]
	[SerializeField] private SessionChannelSO _OnSetMoveTarget = default;

	[Header("Set Raycast Event")]
	[SerializeField] private SessionChannelSO _OnSetRaycast = default;

	[Header("Set Move Character Event")]
	[SerializeField] private SessionChannelSO _OnSetMoveCharacter = default;

    [Header("Set Login Event")]
    [SerializeField] private SessionChannelSO _OnLogin = default;

    [Header("Set Use Skill Event")]
    [SerializeField] private SessionChannelSO _OnUseSkill = default;

    private void OnEnable()
	{
		if (_OnAddAgent != null)
		{
			_OnAddAgent.OnEventRaised += OnAddAgent;
		}

		if (_OnRemoveAgent != null)
		{
			_OnRemoveAgent.OnEventRaised += OnRemoveAgent;
		}

		if (_OnSetMoveTarget != null)
		{
			_OnSetMoveTarget.OnEventRaised += OnSetMoveTarget;
		}

		if (_OnSetRaycast != null)
		{
			_OnSetRaycast.OnEventRaised += OnSetRaycast;
		}

		if (_OnSetMoveCharacter != null)
		{
			_OnSetMoveCharacter.OnEventRaised += OnSetMoveCharacter;
		}
        if (_OnLogin != null)
        {
            _OnLogin.OnEventRaised += OnLogin;
        }
        if (_OnUseSkill != null)
        {
            _OnUseSkill.OnEventRaised += OnUseSkill;
        }
    }

	private void OnDisable()
	{
		if (_OnAddAgent != null)
		{
			_OnAddAgent.OnEventRaised -= OnAddAgent;
		}
		if (_OnRemoveAgent != null)
		{
			_OnRemoveAgent.OnEventRaised -= OnRemoveAgent;
		}
		if (_OnSetMoveTarget != null)
		{
			_OnSetMoveTarget.OnEventRaised -= OnSetMoveTarget;
		}
		if (_OnSetRaycast != null)
		{
			_OnSetRaycast.OnEventRaised -= OnSetRaycast;
		}
		if (_OnSetMoveCharacter != null)
		{
			_OnSetMoveCharacter.OnEventRaised -= OnSetMoveCharacter;
		}
        if (_OnLogin != null)
        {
            _OnLogin.OnEventRaised -= OnLogin;
        }
        if (_OnUseSkill != null)
        {
            _OnUseSkill.OnEventRaised -= OnUseSkill;
        }
    }

	private void OnAddAgent(int agent_id, Vector3 pos, int type)
	{
        int messageId = nextMesssagetId();
        SendMessage(MakeAddAgent(messageId, pos, (GameObjectType)type), response =>
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

	private void OnRemoveAgent(int agent_id, Vector3 pos, int type)
	{
		SendMessage(MakeRemoveAgent(agent_id));
	}

	private void OnSetMoveTarget(int agent_id, Vector3 pos, int type)
	{
		Debug.Log($"SetMoveTarget agent_id: {player_agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
        SendMessage(MakeSetMoveTarget(player_agnet_id, pos));
	}

	private void OnSetRaycast(int agent_id, Vector3 pos, int type)
	{
		Debug.Log($"SetRaycast agent_id: {agent_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
        SendMessage(MakeSetRaycast(pos));
	}

	private void OnSetMoveCharacter(int agent_id, Vector3 pos, int type)
	{
        Debug.Log($"SetMoveCharacter agent_id: {player_agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
        SendMessage(MakeSetMoveTarget(player_agnet_id, pos));
	}
    private void OnLogin(int agent_id, Vector3 pos, int type)
    {
		int messageId = nextMesssagetId();
        SendMessage(MakeLogin(messageId), response => 
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
	private void OnUseSkill(int agent_id, Vector3 pos, int type)
	{
		var timestamp = unixTimestampMs;
        Debug.Log($"UseSkill agent_id: {agent_id}, pos({pos.x}, {pos.y}, {pos.z}) timestamp({timestamp})");

		if(isCasting == true)
		{
			Debug.Log("isCasting");
			return;
        }
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

        SendMessage(MakeUseSkill(2, player_agnet_id, pos, type, timestamp));
        jumpCoroutine = StartCoroutine(JumpToPosition(game_object, game_object.transform.position, pos, skill_duration, skill_height, timestamp));
	}

    private IEnumerator JumpToPosition(GameObject game_object, Vector3 start, Vector3 end, float duration, float height, long timestamp)
    {
        float time = 0;
        float dropPoint = 0.7f; // 상승 구간 비율 (0~1)
        float fallDuration = duration * (1f - dropPoint); // 하강 구간 시간
        Vector3 lastPos = start;

        isCasting = true;
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
		isCasting = false;
		Debug.Log($"JumpToPosition End: {game_object.name}, pos({end.x}, {end.y}, {end.z}), timestamp({timestamp})");
    }

    void Start()
	{
		Application.runInBackground = true;
		startServer();
	}

	public void startServer()
	{
		session = new TcpConnection(core.NetworkHelper.CreateIPEndPoint(GameManager.Instance.server_address));
		session.Receiver = this;
		session.Connect();
	}


	byte[] MakeHeader(byte[] body)
	{
        // 2byte header + 2byte body length
        return BitConverter.GetBytes((ushort)body.Length);
	}

	public byte[] MakeAddAgent(int messageId, Vector3 pos, GameObjectType gameObjectType = GameObjectType.Monster)
	{
		var builder = new FlatBufferBuilder(1024);

		AddAgent.StartAddAgent(builder);
		//AddAgent.AddPos(builder, Vec3.CreateVec3(builder, -1.4f, 0.69f, 2.68f));
		AddAgent.AddPos(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
		AddAgent.AddGameObjectType(builder, gameObjectType);
		var offset = AddAgent.EndAddAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.AddAgent, offset.Value, messageId);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}
	public byte[] MakeRemoveAgent(int agentId)
	{
		var builder = new FlatBufferBuilder(1024);

		RemoveAgent.StartRemoveAgent(builder);
		RemoveAgent.AddAgentId(builder, agentId);
		var offset = RemoveAgent.EndRemoveAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.RemoveAgent, offset.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	public byte[] MakeSetMoveTarget(int agentId, Vector3 pos)
	{
		var builder = new FlatBufferBuilder(1024);

		SetMoveTarget.StartSetMoveTarget(builder);
		//SetMoveTarget.AddAgentId(builder, 1);
		//SetMoveTarget.AddPos(builder, Vec3.CreateVec3(builder, 0.73f, 0.69f, 11.5f));
		SetMoveTarget.AddAgentId(builder, agentId);
		SetMoveTarget.AddPos(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
		var offset = SetMoveTarget.EndSetMoveTarget(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.SetMoveTarget, offset.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	public byte[] MakePing()
	{
		var builder = new FlatBufferBuilder(1024);

		syncnet.Ping.StartPing(builder);
		syncnet.Ping.AddSeq(builder, seq++);
		var offset = syncnet.Ping.EndPing(builder);

		var msg = GameMessage.CreateGameMessage(builder,GameMessages.Ping, offset.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	public byte[] MakeSetRaycast(Vector3 pos)
	{
		var builder = new FlatBufferBuilder(1024);

		SetRaycast.StartSetRaycast(builder);
		SetRaycast.AddPos(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
		var offset = SetRaycast.EndSetRaycast(builder);

		var msg = GameMessage.CreateGameMessage(builder,GameMessages.SetRaycast, offset.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	public byte[] MakeLogin(int messageId)
    {
        var builder = new FlatBufferBuilder(1024);
		var nameOffSet = builder.CreateString("test");
		var passwordOffSet = builder.CreateString("1234");
        Login.StartLogin(builder);
        Login.AddUserId(builder, nameOffSet);
        Login.AddPassword(builder,passwordOffSet);
        var offset = Login.EndLogin(builder);
        var msg = GameMessage.CreateGameMessage(builder, GameMessages.Login, offset.Value, messageId);
        builder.Finish(msg.Value);
        byte[] body = builder.SizedByteArray();
        return body;
    }

    public byte[] MakeUseSkill(int skillId, int agentId, Vector3 pos, int type, long timestamp)
    {
        var builder = new FlatBufferBuilder(1024);
        UseSkill.StartUseSkill(builder);
		UseSkill.AddSkillId(builder, skillId);
        UseSkill.AddId(builder, agentId);
        UseSkill.AddPos(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
		UseSkill.AddTargetId(builder, type);
		UseSkill.AddDuration(builder, 1);
		UseSkill.AddTimestamp(builder, timestamp);
        var offset = UseSkill.EndUseSkill(builder);
        var msg = GameMessage.CreateGameMessage(builder, GameMessages.UseSkill, offset.Value);
        builder.Finish(msg.Value);
        byte[] body = builder.SizedByteArray();
        return body;
    }

    public void SendPing(float deltaTime)
	{
		lastSendTime += deltaTime;
		if (lastSendTime >= 0.1f)
		{
			byte[] body = MakePing();

			session.SendBytes(MakeHeader(body));
			session.SendBytes(body);

			lastSendTime = 0f;
		}
	}

	public void SendMessage(byte[] msg, Action<GameMessage> response = null)
	{
		session.SendBytes(MakeHeader(msg));
		session.SendBytes(msg);
		if (response != null)
		{
			responses.Add(last_message_id, response);
		}
    }


	void OnReceive(byte[] bytes)
	{
		var recv_msg = GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
		switch (recv_msg.MsgType)
		{
			case GameMessages.UpdateActorNotify:
				{
                    UpdateActorNotify updateActorNotify = recv_msg.Msg<UpdateActorNotify>().Value;
					for (int i = 0; i < updateActorNotify.ActorsLength; ++i)
					{
						var updatedActor = updateActorNotify.Actors(i).Value;
						var agent_id = updatedActor.AgentId;
						var pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);
						//Debug.Log($"UpdateActorNotify agent_id: {agent_id}, pos({pos.x}, {pos.y}, {pos.z}) ");

                        GameObject game_object = null;
						Actor actor = null;
                        if (game_objects.TryGetValue(agent_id, out game_object) == false)
						{
							switch (updatedActor.GameObjectType)
							{
								case GameObjectType.Monster:
									game_object = (GameObject)Instantiate(Resources.Load("Monster"), pos, Quaternion.identity);
									actor = game_object.GetComponent<Monster>();
                                    actor.agnet_id = agent_id;
									break;
								case GameObjectType.Character:
									game_object = (GameObject)Instantiate(Resources.Load("Character2"), pos, Quaternion.identity);
                                    actor = game_object.GetComponent<Character>();
                                    actor.agnet_id = agent_id;
									break;
								default:
									Debug.LogError("error game object type");
									break;
							}

							game_objects[agent_id] = game_object;
						} 
						else
						{
							actor = game_object.GetComponent<Actor>();
                        }


						actor.pos = pos;
						actor.input_locked = updatedActor.InputLocked;

                        if (updatedActor.State.HasValue)
                        {
                            actor.state = updatedActor.State.Value.State;

                        }
                        if (updatedActor.Health.HasValue)
                        {
                            actor.health = updatedActor.Health.Value.Health;
                        }

                        if (updatedActor.GameObjectType == GameObjectType.Monster)
						{
							switch (updatedActor.State.Value.State)
							{
								case AIState.Detect:
									game_objects[agent_id].GetComponent<MeshRenderer>().material.color = Color.red;
									break;
								case AIState.Patrol:
									game_objects[agent_id].GetComponent<MeshRenderer>().material.color = Color.white;
									break;
								case AIState.Attack:
									game_objects[agent_id].GetComponent<MeshRenderer>().material.color = Color.blue;
									break;
							}
						}
						else if (updatedActor.GameObjectType == GameObjectType.Character)
						{
                            //Debug.Log($"Player Agent ID: {agent_id}, pos({pos.x}, {pos.y}, {pos.z}) ");

                        }
					}
					//List<int> removals = new List<int>();
					//foreach(var game_object in game_objects)
     //               {
					//	if (agents.ContainsKey(game_object.Key) == false)
					//		removals.Add(game_object.Key);
     //               }

					//foreach(var id in removals)
     //               {
					//	Destroy(game_objects[id]);
					//	game_objects.Remove(id);
					//}

					for (int i = 0; i < updateActorNotify.DebugsLength; ++i)
					{
						Vector3 pos;
						pos.x = updateActorNotify.Debugs(i).Value.EndPos.Value.X;
						pos.y = updateActorNotify.Debugs(i).Value.EndPos.Value.Y;
						pos.z = updateActorNotify.Debugs(i).Value.EndPos.Value.Z;
						var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);
					}
				}
				break;
			case GameMessages.UseSkill:
                {
                    UseSkill useSkill = recv_msg.Msg<UseSkill>().Value;
                    var pos = new Vector3(useSkill.Pos.Value.X, useSkill.Pos.Value.Y, useSkill.Pos.Value.Z);
                    var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);

					var target_agent_id = useSkill.Id;
					var remote_player_skill_duration = skill_duration - (unixTimestampMs- useSkill.Timestamp) / 1000f;

                    GameObject game_object = null;
                    if (game_objects.TryGetValue(target_agent_id, out game_object) == true)
                    {
                        jumpCoroutine = StartCoroutine(JumpToPosition(game_object, game_object.transform.position, pos, remote_player_skill_duration, skill_height, useSkill.Timestamp));

                    }
                }
                break;
        }

		if(recv_msg.Id > 0)
		{
			if (responses.ContainsKey(recv_msg.Id))
			{
				responses[recv_msg.Id](recv_msg);
				responses.Remove(recv_msg.Id);
			}
		}
    }

	void Update()
	{
		//SendPing(Time.deltaTime);
		byte[] result;
		while (session.queue.TryDequeue(out result))
		{
			OnReceive(result);
		}

		foreach (var game_object in game_objects.Values)
		{
			try
			{
				var actor = game_object.GetComponent<Actor>();
                float lerpSpeed = 15f; // 원하는 값으로 조정 (5~15 정도가 적당함)
                game_object.transform.position =
					Vector3.Lerp(game_object.transform.position, actor.pos, Time.deltaTime * lerpSpeed);
			}
			catch
			{

			}
		}
	}
}
