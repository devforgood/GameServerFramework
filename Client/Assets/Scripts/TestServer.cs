using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using FlatBuffers;
using syncnet;


//[Serializable]
//public class AgentInfo
//{
//	public int agent;
//	public Vector3 pos;
//}

public class TestServer : MonoBehaviour
{
	public GameObject[] mob;
	public Camera camera;

	Vector3[] agent_pos = new Vector3[100];
	int agent_count = 0;

	TcpConnection session;
	int message_count = 0;
	float lastSendTime = 0f;
	int seq = 1;


	RaycastHit hit_;
	bool clickOn = false;
	bool clickCtrlOn = false;
	bool clickAltOn = false;
	bool clickLeftOn = false;

	// Start is called before the first frame update
	void Start()
    {
		Application.runInBackground = true;
		startServer();
	}

	void startServer()
	{
		session = new TcpConnection(core.NetworkHelper.CreateIPEndPoint("127.0.0.1:60001"));
		session.Receiver = this;
		session.Connect();

	}

	byte[] MakeHeader(byte[] body)
    {
		return BitConverter.GetBytes(body.Length);
	}

	byte[] MakeAddAgent(Vector3 pos)
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
	byte[] MakeRemoveAgent(int agentId)
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

	byte[] MakeSetMoveTarget(int agentId, Vector3 pos)
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

	byte[] MakePing()
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

	byte[] MakeSetRaycast(Vector3 pos)
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

	void SendPing(float deltaTime)
    {
		lastSendTime += deltaTime;
		if(lastSendTime >= 0.01f)
        {
			byte[] body = MakePing();

            session.SendBytes(MakeHeader(body));
			session.SendBytes(body);

			lastSendTime = 0f;
		}
	}

	void SendMessage(byte[] msg)
    {
		session.SendBytes(MakeHeader(msg));
		session.SendBytes(msg);
	}


	void OnReceive(byte[] bytes)
    {
		var recv_msg = GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
		switch (recv_msg.MsgType)
		{
			case GameMessages.AgentInfo:
				{
					AgentInfo agnetInfo = recv_msg.Msg<AgentInfo>().Value;
					Vec3 pos = agnetInfo.Pos.Value;
					Debug.Log($"recv id : {agnetInfo.AgentId}, pos({pos.X}, {pos.Y}, {pos.Z} )");
					agent_pos[0].x = pos.X;
					agent_pos[0].y = pos.Y;
					agent_pos[0].z = pos.Z;
					agent_count = 1;
				}
				break;
			case GameMessages.GetAgents:
                {
					GetAgents getAgents = recv_msg.Msg<GetAgents>().Value;
					for(int i=0;i<getAgents.AgentsLength;++i)
                    {
						Vec3 pos = getAgents.Agents(i).Value.Pos.Value;
						//Debug.Log($"recv id : {getAgents.Agents(i).Value.AgentId}, pos({pos.X}, {pos.Y}, {pos.Z} )");
						agent_pos[getAgents.Agents(i).Value.AgentId].x = pos.X;
						agent_pos[getAgents.Agents(i).Value.AgentId].y = pos.Y;
						agent_pos[getAgents.Agents(i).Value.AgentId].z = pos.Z;
					}
					agent_count = getAgents.AgentsLength;


					for(int i=0;i<getAgents.DebugsLength;++i)
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

	void OnDisable()
	{
	}

	bool GetHitPoint()
	{
		Vector3 mos = Input.mousePosition;
		mos.z = camera.farClipPlane; // 카메라가 보는 방향과, 시야를 가져온다.

		Vector3 dir = camera.ScreenToWorldPoint(mos);
		// 월드의 좌표를 클릭했을 때 화면에 자신이 보고있는 화면에 맞춰 좌표를 바꿔준다.

		if (Physics.Raycast(camera.transform.position, dir, out hit_, mos.z))
		{
			//target.position = hit.point; // 타겟을 레이캐스트가 충돌된 곳으로 옮긴다.
			if (hit_.transform.gameObject.tag == "floor")
			{
				return true;
			}
		}
		return false;
	}

	// Update is called once per frame
	void Update()
    {
		SendPing(Time.deltaTime);
		byte[] result;
		while(session.queue.TryDequeue(out result))
        {
			OnReceive(result);
        }

		for (int i = 0; i < agent_count ; ++i)
		{
			try
			{
				//Debug.DrawLine(ExportNavMeshToObj.ToUnityVector(lastPosition[i]), ExportNavMeshToObj.ToUnityVector(crowd.GetAgent(i).Position), Color.green, 1);
				mob[i].transform.position = Vector3.Lerp(mob[i].transform.position, agent_pos[i], UnityEngine.Time.deltaTime * 10f);
			}
			catch
			{

			}
		}

		if(Input.GetMouseButtonUp(0))
        {
			clickOn = false;
			clickCtrlOn = false;
			clickAltOn = false;
        }

		if (Input.GetMouseButtonUp(1))
		{
			clickLeftOn = false;
		}



		if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))  
		{
			if (clickCtrlOn == false)
			{
				if (GetHitPoint())
				{
					SendMessage(MakeAddAgent(hit_.point)); 
                }

				clickCtrlOn = true;
            }
        }
		else if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
		{
			if (clickAltOn == false)
			{
				if (GetHitPoint())
				{

				}
				clickAltOn = true;
			}
		}
		else if (Input.GetMouseButton(0))
		{
			if (clickOn == false)
			{
				if (GetHitPoint())
				{
					SendMessage(MakeSetMoveTarget(-1, hit_.point));
				}

				clickOn = true;
			}
		}

		if (Input.GetMouseButton(1))  
		{
			if (clickLeftOn == false)
			{
				if (GetHitPoint())
				{
					SendMessage(MakeSetRaycast(hit_.point));
				}

				clickLeftOn = true;
			}
		}


	}
}
