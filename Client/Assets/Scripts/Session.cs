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
		session = new TcpConnection(core.NetworkHelper.CreateIPEndPoint(GameManager.Instance.server_address));
		session.Receiver = this;
		session.Connect();
	}

	private byte[] MakeHeader(byte[] body)
	{
        // 2byte header + 2byte body length
        return BitConverter.GetBytes((ushort)body.Length);
	}

	public void SendPing(float deltaTime)
	{
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
		for (int i = 0; i < updateActorNotify.ActorsLength; ++i)
		{
			var updatedActor = updateActorNotify.Actors(i).Value;
			var agent_id = updatedActor.AgentId;
			var pos = new Vector3(updatedActor.Pos.Value.X, updatedActor.Pos.Value.Y, updatedActor.Pos.Value.Z);

			GameObject game_object = null;
			Actor actor = null;
			if (!game_objects.TryGetValue(agent_id, out game_object))
			{
				game_object = CreateGameObject(updatedActor.GameObjectType, pos, agent_id);
				if (game_object != null)
				{
					game_objects[agent_id] = game_object;
					actor = game_object.GetComponent<Actor>();
				}
			}
			else
			{
				actor = game_object.GetComponent<Actor>();
			}

			if (actor != null)
			{
				UpdateActorState(game_object, actor, updatedActor, pos);
			}
		}

		// Debug lines 처리
		for (int i = 0; i < updateActorNotify.DebugsLength; ++i)
		{
			Vector3 pos;
			pos.x = updateActorNotify.Debugs(i).Value.EndPos.Value.X;
			pos.y = updateActorNotify.Debugs(i).Value.EndPos.Value.Y;
			pos.z = updateActorNotify.Debugs(i).Value.EndPos.Value.Z;
			var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);
		}
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

	private void UpdateActorState(GameObject game_object, Actor actor, syncnet.ActorInfo updatedActor, Vector3 pos)
	{
		actor.pos = pos;
		actor.input_locked = updatedActor.InputLocked;

        if (updatedActor.Health.HasValue)
		{
			actor.health = updatedActor.Health.Value.Health;
			actor.UpdateHealthUI(actor.health);
		}

		if (updatedActor.GameObjectType == GameObjectType.Monster)
		{
			if (updatedActor.State.HasValue)
			{
				UpdateMonsterVisuals(actor.gameObject, updatedActor.State.Value.State);
			}
		}
		else if (updatedActor.GameObjectType == GameObjectType.Character)
		{
			//Debug.Log($"Player Agent ID: {actor.agnet_id}, pos({pos.x}, {pos.y}, {pos.z}) ");
		}

        if (updatedActor.State.HasValue)
        {
            Debug.Log($"Actor state {updatedActor.State.Value.State}");
            actor.UpdateState(game_object, updatedActor.State.Value.State);
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
		if (resSkill.CodeName == "JumpSkill")
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
}
