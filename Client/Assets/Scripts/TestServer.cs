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
					Session.Instance.SendMessage(Session.Instance.MakeAddAgent(hit_.point)); 
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
					Session.Instance.SendMessage(Session.Instance.MakeSetMoveTarget(-1, hit_.point));
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
					Session.Instance.SendMessage(Session.Instance.MakeSetRaycast(hit_.point));
				}

				clickLeftOn = true;
			}
		}


	}
}
