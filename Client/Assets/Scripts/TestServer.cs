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
	public Camera camera;


	RaycastHit hit_;
	bool clickOn = false;
	bool clickCtrlOn = false;
	bool clickAltOn = false;

	bool clickLeftCtrlOn = false;
	bool clickLeftAltOn = false;
	bool clickLeftOn = false;


	// Start is called before the first frame update
	void Start()
    {
		Application.runInBackground = true;
		Session.Instance.startServer();
	}




	void OnDisable()
	{
	}

	bool GetHitPoint(string tag)
	{
		Vector3 mos = Input.mousePosition;
		mos.z = camera.farClipPlane; // ī�޶� ���� �����, �þ߸� �����´�.

		Vector3 dir = camera.ScreenToWorldPoint(mos);
		// ������ ��ǥ�� Ŭ������ �� ȭ�鿡 �ڽ��� �����ִ� ȭ�鿡 ���� ��ǥ�� �ٲ��ش�.

		if (Physics.Raycast(camera.transform.position, dir, out hit_, mos.z))
		{
			//target.position = hit.point; // Ÿ���� ����ĳ��Ʈ�� �浹�� ������ �ű��.
			if (tag != string.Empty)
			{
				if (hit_.transform.gameObject.tag == tag)
				{
					return true;
				}
			}
		}
		return false;
	}

	// Update is called once per frame
	void Update()
    {


		if(Input.GetMouseButtonUp(0))
        {
			clickOn = false;
			clickCtrlOn = false;
			clickAltOn = false;
        }

		if (Input.GetMouseButtonUp(1))
		{
			clickLeftOn = false;
			clickLeftCtrlOn = false;
			clickLeftAltOn = false;
		}



		if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftControl))  
		{
			if (clickCtrlOn == false)
			{
				if (GetHitPoint("floor"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeAddAgent(hit_.point)); 
                }

				clickCtrlOn = true;
            }
        }
		else if (Input.GetMouseButton(0) && Input.GetKey(KeyCode.LeftAlt))
		{
			if (clickAltOn == false)
			{
				if (GetHitPoint("monster"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeRemoveAgent(hit_.transform.gameObject.GetComponent<Monster>().agnet_id));
				}
				clickAltOn = true;
			}
		}
		else if (Input.GetMouseButton(0))
		{
			if (clickOn == false)
			{
				if (GetHitPoint("floor"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeSetMoveTarget(-1, hit_.point));
				}

				clickOn = true;
			}
		}

		if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftControl))
		{
			if (clickLeftCtrlOn == false)
			{
				if (GetHitPoint("floor"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeAddAgent(hit_.point, GameObjectType.Character));
				}

				clickLeftCtrlOn = true;
			}
		}
		else if (Input.GetMouseButton(1) && Input.GetKey(KeyCode.LeftAlt))
		{
			if (clickLeftAltOn == false)
			{
				if (GetHitPoint("floor"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeSetRaycast(hit_.point));
				}

				clickLeftAltOn = true;
			}
		}
		else if (Input.GetMouseButton(1))  
		{
			if (clickLeftOn == false)
			{
				if (GetHitPoint("floor"))
				{
					Session.Instance.SendMessage(Session.Instance.MakeSetMoveTarget(Session.Instance.player_agnet_id, hit_.point));
				}

				clickLeftOn = true;
			}
		}


	}
}
