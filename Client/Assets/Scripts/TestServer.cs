using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;


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
	Socket listener;
	Socket handler;

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
	void networkCode()
	{
		string data;

		// Data buffer for incoming data.
		byte[] bytes = new Byte[1024];

		// host running the application.
		Debug.Log("Ip " + getIPAddress().ToString());
		IPAddress[] ipArray = Dns.GetHostAddresses(getIPAddress());
		IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Any, 60001);

		// Create a TCP/IP socket.
		listener = new Socket(ipArray[0].AddressFamily,
			SocketType.Stream, ProtocolType.Tcp);

		// Bind the socket to the local endpoint and 
		// listen for incoming connections.

		try
		{
			listener.Bind(localEndPoint);
			listener.Listen(10);

			// Start listening for connections.
			while (true)
			{
				keepReading = true;

				// Program is suspended while waiting for an incoming connection.
				Debug.Log("Waiting for Connection");     //It works

				handler = listener.Accept();
				Debug.Log("Client Connected");     //It doesn't work
				data = null;

				// An incoming connection needs to be processed.
				while (keepReading)
				{
					bytes = new byte[1024];
					int bytesRec = handler.Receive(bytes, 4, SocketFlags.None);
					int length = BitConverter.ToInt32(bytes, 0);
					Debug.Log("Received from Server");
					if (bytesRec <= 0)
					{
						keepReading = false;
						handler.Disconnect(true);
						break;
					}

					bytesRec = handler.Receive(bytes, length, SocketFlags.None);
					if (bytesRec <= 0)
					{
						keepReading = false;
						handler.Disconnect(true);
						break;
					}

					var str = Encoding.Default.GetString(bytes);
					Debug.Log($"recv : {str}");
					var agent_info = JsonUtility.FromJson<AgentInfo>(str);
					agent_pos[agent_info.agent] = agent_info.pos;
					if (agent_count < (agent_info.agent+1))
						agent_count = (agent_info.agent+1);


					data += Encoding.ASCII.GetString(bytes, 0, bytesRec);



					if (data.IndexOf("<EOF>") > -1)
					{
						break;
					}
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

		if (handler != null && handler.Connected)
		{
			handler.Disconnect(false);
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
