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


[Serializable]
public class AgentInfo
{
	public int agent;
	public Vector3 pos;
}

public class TestServer : MonoBehaviour
{
	public GameObject[] mob;

	System.Threading.Thread SocketThread;
	volatile bool keepReading = false;
	Socket socket;

	Vector3[] agent_pos = new Vector3[100];
	int agent_count = 0;

	// Start is called before the first frame update
	void Start()
    {
		Application.runInBackground = true;
		startServer();
	}

	private string getIPAddress()
	{
		IPHostEntry host;
		string localIP = "";
		host = Dns.GetHostEntry(Dns.GetHostName());
		foreach (IPAddress ip in host.AddressList)
		{
			if (ip.AddressFamily == AddressFamily.InterNetwork)
			{
				localIP = ip.ToString();
			}

		}
		return localIP;
	}
	void startServer()
	{
		SocketThread = new System.Threading.Thread(networkCode);
		SocketThread.IsBackground = true;
		SocketThread.Start();
	}

	(byte[], byte[]) MakeAddAgent()
    {
		var builder = new FlatBufferBuilder(1024);

		AddAgent.StartAddAgent(builder);
		AddAgent.AddPos(builder, Vec3.CreateVec3(builder, 1.0f, 2.0f, 3.0f));
		var addagent = AddAgent.EndAddAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.AddAgent, addagent.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();
		byte[] header = BitConverter.GetBytes(body.Length);
		return (header, body);
	}
	(byte[], byte[]) MakeRemoveAgent()
	{
		var builder = new FlatBufferBuilder(1024);

		RemoveAgent.StartRemoveAgent(builder);
		RemoveAgent.AddAgentId(builder, 1);
		var removeagnet = RemoveAgent.EndRemoveAgent(builder);

		var msg = GameMessage.CreateGameMessage(builder, GameMessages.RemoveAgent, removeagnet.Value);
		builder.Finish(msg.Value);

		byte[] body = builder.SizedByteArray();
		byte[] header = BitConverter.GetBytes(body.Length);
		return (header, body);
	}

	void networkCode()
	{
		string data;

		// Data buffer for incoming data.
		byte[] bytes = new Byte[1024];

		// host running the application.
		Debug.Log("Ip " + getIPAddress().ToString());
		IPAddress[] ipArray = Dns.GetHostAddresses(getIPAddress());
		IPEndPoint endPoint = core.NetworkHelper.CreateIPEndPoint("127.0.0.1:60001");

		// Create a TCP/IP socket.
		socket = new Socket(ipArray[0].AddressFamily,
			SocketType.Stream, ProtocolType.Tcp);

		// Bind the socket to the local endpoint and 
		// listen for incoming connections.



		byte[] header;
		byte[] body;
		try
		{
			socket.Connect(endPoint);
			Debug.Log("Client Connected");     //It doesn't work



			int cnt = 0;
			// Start listening for connections.
			while (true)
            {
                keepReading = true;

				// Program is suspended while waiting for an incoming connection.
				Debug.Log("Waiting for Connection");     //It works

				Debug.Log("Client Connected");     //It doesn't work
				data = null;

				// An incoming connection needs to be processed.
				while (keepReading)
				{
					if(cnt++ % 2 == 0)
                    {
						(header, body) = MakeAddAgent();
                    }
					else
                    {
						(header, body) = MakeRemoveAgent();
					}


					if (socket.Send(header) <= 0)
                    {
						keepReading = false;
						socket.Disconnect(true);
						break;
					}

					if(socket.Send(body) <= 0)
                    {
						keepReading = false;
						socket.Disconnect(true);
						break;
					}




					//int bytesRec = socket.Receive(bytes, 4, SocketFlags.None);
					//int length = BitConverter.ToInt32(bytes, 0);
					//Debug.Log("Received from Server");
					//if (bytesRec <= 0)
					//{
					//	keepReading = false;
					//	socket.Disconnect(true);
					//	break;
					//}

					//bytesRec = socket.Receive(bytes, length, SocketFlags.None);
					//if (bytesRec <= 0)
					//{
					//	keepReading = false;
					//	socket.Disconnect(true);
					//	break;
					//}

					//var str = Encoding.Default.GetString(bytes);
					//Debug.Log($"recv : {str}");
					//var agent_info = JsonUtility.FromJson<AgentInfo>(str);
					//agent_pos[agent_info.agent] = agent_info.pos;
					//if (agent_count < (agent_info.agent+1))
					//	agent_count = (agent_info.agent+1);


					//data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

					//if (data.IndexOf("<EOF>") > -1)
					//{
					//	break;
					//}
					System.Threading.Thread.Sleep(1);
				}

				System.Threading.Thread.Sleep(1);
			}
		}
		catch (Exception e)
		{
			Debug.Log(e.ToString());
		}
	}

	void stopServer()
	{
		keepReading = false;

		//stop thread
		if (SocketThread != null)
		{
			SocketThread.Abort();
		}

		if (socket != null && socket.Connected)
		{
			socket.Disconnect(false);
			Debug.Log("Disconnected!");
		}
	}

	void OnDisable()
	{
		stopServer();
	}

	// Update is called once per frame
	void Update()
    {
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
	}
}
