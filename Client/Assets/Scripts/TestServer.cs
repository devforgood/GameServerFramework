using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using FlatBuffers;
using MyGame.Sample;


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



		// ------------------------------------------------
		var builder = new FlatBufferBuilder(1024);
		var weaponOneName = builder.CreateString("Sword");
		var weaponOneDamage = 3;
		var weaponTwoName = builder.CreateString("Axe");
		var weaponTwoDamage = 5;
		// Use the `CreateWeapon()` helper function to create the weapons, since we set every field.
		var sword = Weapon.CreateWeapon(builder, weaponOneName, (short)weaponOneDamage);
		var axe = Weapon.CreateWeapon(builder, weaponTwoName, (short)weaponTwoDamage);
		// Serialize a name for our monster, called "Orc".
		var name = builder.CreateString("Orc");
		// Create a `vector` representing the inventory of the Orc. Each number
		// could correspond to an item that can be claimed after he is slain.
		// Note: Since we prepend the bytes, this loop iterates in reverse order.
		Monster.StartInventoryVector(builder, 10);
		for (int i = 9; i >= 0; i--)
		{
			builder.AddByte((byte)i);
		}
		var inv = builder.EndVector();
		var weaps = new Offset<Weapon>[2];
		weaps[0] = sword;
		weaps[1] = axe;
		// Pass the `weaps` array into the `CreateWeaponsVector()` method to create a FlatBuffer vector.
		var weapons = Monster.CreateWeaponsVector(builder, weaps);
		//Monster.StartPathVector(fbb, 2);
		//Vec3.CreateVec3(builder, 1.0f, 2.0f, 3.0f);
		//Vec3.CreateVec3(builder, 4.0f, 5.0f, 6.0f);
		//var path = fbb.EndVector();
		// Create our monster using `StartMonster()` and `EndMonster()`.
		Monster.StartMonster(builder);
		Monster.AddPos(builder, Vec3.CreateVec3(builder, 1.0f, 2.0f, 3.0f));
		Monster.AddHp(builder, (short)300);
		Monster.AddName(builder, name);
		Monster.AddInventory(builder, inv);
		Monster.AddColor(builder, MyGame.Sample.Color.Red);
		Monster.AddWeapons(builder, weapons);
		Monster.AddEquippedType(builder, Equipment.Weapon);
		Monster.AddEquipped(builder, axe.Value); // Axe
		//Monster.AddPath(builder, path);
		var orc = Monster.EndMonster(builder);
		//Monster.AddEquippedType(builder, Equipment.Weapon); // Union type
		//Monster.AddEquipped(builder, axe.Value); // Union data
												 // Call `Finish()` to instruct the builder that this monster is complete.
		builder.Finish(orc.Value); // You could also call `Monster.FinishMonsterBuffer(builder, orc);`.
								   // This must be called after `Finish()`.
		//var buf = builder.DataBuffer; // Of type `FlatBuffers.ByteBuffer`.
									  // The data in this ByteBuffer does NOT start at 0, but at buf.Position.
									  // The end of the data is marked by buf.Length, so the size is
									  // buf.Length - buf.Position.
									  // Alternatively this copies the above data out of the ByteBuffer for you:
		byte[] body = builder.SizedByteArray();


		//--------------------------------------------------------------------------------

		byte[] header = BitConverter.GetBytes(body.Length);

		try
		{
			socket.Connect(endPoint);
			Debug.Log("Client Connected");     //It doesn't work



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
					bytes = new byte[1024];
					

					if(socket.Send(header) <= 0)
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
