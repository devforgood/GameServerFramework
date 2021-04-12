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

	byte[] MakeAddAgent()
    {
		var builder = new FlatBufferBuilder(1024);

		AddAgent.StartAddAgent(builder);
		AddAgent.AddPos(builder, Vec3.CreateVec3(builder, -1.4f, 0.69f, 2.68f));
		var addagent = AddAgent.EndAddAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.AddAgent, addagent.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}
	byte[] MakeRemoveAgent()
	{
		var builder = new FlatBufferBuilder(1024);

		RemoveAgent.StartRemoveAgent(builder);
		RemoveAgent.AddAgentId(builder, 9);
		var removeagnet = RemoveAgent.EndRemoveAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.RemoveAgent, removeagnet.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	byte[] MakeSetMoveTarget()
	{
		var builder = new FlatBufferBuilder(1024);

		SetMoveTarget.StartSetMoveTarget(builder);
		SetMoveTarget.AddAgentId(builder, 1);
		SetMoveTarget.AddPos(builder, Vec3.CreateVec3(builder, 0.73f, 0.69f, 11.5f));
		var removeagnet = SetMoveTarget.EndSetMoveTarget(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.SetMoveTarget, removeagnet.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();

		return body;
	}

	void SendMessage(float deltaTime)
    {
		lastSendTime += deltaTime;
		if(lastSendTime >= 0.01f)
        {
			byte[] body;

			if (message_count == 0)
			{
				body = MakeAddAgent();
			}
			else if (message_count == 1)
			{
				body = MakeSetMoveTarget();
			}
			else
			{
				body = MakeRemoveAgent();
			}
			++message_count;

			session.SendBytes(MakeHeader(body));
			session.SendBytes(body);

			lastSendTime = 0f;
		}

	}

	void OnReceive(byte[] bytes)
    {
		var recv_msg = GameMessage.GetRootAsGameMessage(new ByteBuffer(bytes));
		if (recv_msg.MsgType == GameMessages.AgentInfo)
		{
			AgentInfo agnetInfo = recv_msg.Msg<AgentInfo>().Value;
			Vec3 pos = agnetInfo.Pos.Value;
			Debug.Log($"recv id : {agnetInfo.AgentId}, pos({pos.X}, {pos.Y}, {pos.Z} )");
			agent_pos[0].x = pos.X;
			agent_pos[0].y = pos.Y;
			agent_pos[0].z = pos.Z;
			agent_count = 1;
		}
	}

	void OnDisable()
	{
	}

	// Update is called once per frame
	void Update()
    {
		SendMessage(Time.deltaTime);
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

		if (Input.GetMouseButton(0))  // 마우스가 클릭 되면
		{
			Vector3 mos = Input.mousePosition;
			mos.z = camera.farClipPlane; // 카메라가 보는 방향과, 시야를 가져온다.

			Vector3 dir = camera.ScreenToWorldPoint(mos);
			// 월드의 좌표를 클릭했을 때 화면에 자신이 보고있는 화면에 맞춰 좌표를 바꿔준다.

			RaycastHit hit;
			if (Physics.Raycast(camera.transform.position, dir, out hit, mos.z))
			{
				//target.position = hit.point; // 타겟을 레이캐스트가 충돌된 곳으로 옮긴다.
				if(hit.transform.gameObject.tag == "floor")
                {

                }
			}
		}
	}
}
