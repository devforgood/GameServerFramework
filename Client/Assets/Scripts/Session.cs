using FlatBuffers;
using syncnet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class Session : MonoSingleton<Session>
{
	int seq = 1;
	TcpConnection session;
	int message_count = 0;
	float lastSendTime = 0f;

	public Dictionary<int, Vector3> agents = new Dictionary<int, Vector3>();
	public Dictionary<int, GameObject> monsters = new Dictionary<int, GameObject>(); 

	public void startServer()
	{
		session = new TcpConnection(core.NetworkHelper.CreateIPEndPoint("127.0.0.1:60001"));
		session.Receiver = this;
		session.Connect();
	}


	byte[] MakeHeader(byte[] body)
	{
		return BitConverter.GetBytes(body.Length);
	}

	public byte[] MakeAddAgent(Vector3 pos)
	{
		var builder = new FlatBufferBuilder(1024);

		AddAgent.StartAddAgent(builder);
		//AddAgent.AddPos(builder, Vec3.CreateVec3(builder, -1.4f, 0.69f, 2.68f));
		AddAgent.AddPos(builder, Vec3.CreateVec3(builder, pos.x, pos.y, pos.z));
		var offset = AddAgent.EndAddAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.AddAgent, offset.Value);
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

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.Ping, offset.Value);
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

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.SetRaycast, offset.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	public void SendPing(float deltaTime)
	{
		lastSendTime += deltaTime;
		if (lastSendTime >= 0.01f)
		{
			byte[] body = MakePing();

			session.SendBytes(MakeHeader(body));
			session.SendBytes(body);

			lastSendTime = 0f;
		}
	}

	public void SendMessage(byte[] msg)
	{
		session.SendBytes(MakeHeader(msg));
		session.SendBytes(msg);
	}


	void OnReceive(byte[] bytes)
	{
		var recv_msg = GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
		switch (recv_msg.MsgType)
		{
			case GameMessages.GetAgents:
				{
					agents.Clear();
					GetAgents getAgents = recv_msg.Msg<GetAgents>().Value;
					for (int i = 0; i < getAgents.AgentsLength; ++i)
					{
						var agent_id = getAgents.Agents(i).Value.AgentId;
						var pos = new Vector3(getAgents.Agents(i).Value.Pos.Value.X, getAgents.Agents(i).Value.Pos.Value.Y, getAgents.Agents(i).Value.Pos.Value.Z);
						agents[agent_id] = pos;

						GameObject mob = null;
						if(monsters.TryGetValue(agent_id, out mob)==false)
                        {
							mob = (GameObject)Instantiate(Resources.Load("Monster"), pos, Quaternion.identity);
							mob.GetComponent<Monster>().agnet_id = agent_id;
							monsters[agent_id] = mob; 
						}
					}

					List<int> removals = new List<int>();
					foreach(var monster in monsters)
                    {
						if (agents.ContainsKey(monster.Key) == false)
							removals.Add(monster.Key);
                    }

					foreach(var id in removals)
                    {
						Destroy(monsters[id]);
						monsters.Remove(id);
					}

					for (int i = 0; i < getAgents.DebugsLength; ++i)
					{
						Vector3 pos;
						pos.x = getAgents.Debugs(i).Value.EndPos.Value.X;
						pos.y = getAgents.Debugs(i).Value.EndPos.Value.Y;
						pos.z = getAgents.Debugs(i).Value.EndPos.Value.Z;
						var obj = (GameObject)Instantiate(Resources.Load("DebugTarget"), pos, Quaternion.identity);
					}
				}
				break;
		}
	}

	void Update()
	{
		SendPing(Time.deltaTime);
		byte[] result;
		while (session.queue.TryDequeue(out result))
		{
			OnReceive(result);
		}

		foreach (var agent in Session.Instance.agents)
		{
			try
			{
				//Debug.DrawLine(ExportNavMeshToObj.ToUnityVector(lastPosition[i]), ExportNavMeshToObj.ToUnityVector(crowd.GetAgent(i).Position), Color.green, 1);
				monsters[agent.Key].transform.position = Vector3.Lerp(monsters[agent.Key].transform.position, agent.Value, UnityEngine.Time.deltaTime * 10f);
			}
			catch
			{

			}
		}
	}
}
