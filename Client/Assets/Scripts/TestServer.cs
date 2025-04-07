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

	[SerializeField] private SessionChannelSO _addAgentChannel = default;
	[SerializeField] private SessionChannelSO _removeAgentChannel = default;
	[SerializeField] private SessionChannelSO _setMoveTargetChannel = default;
	[SerializeField] private SessionChannelSO _setRaycastChannel = default;
	[SerializeField] private SessionChannelSO _setMoveCharacterChannel = default;
    [SerializeField] private SessionChannelSO _setLoginChannel = default;



    void OnDisable()
	{
	}

	bool GetHitPoint(string tag)
	{
		Vector3 mos = Input.mousePosition;
		mos.z = camera.farClipPlane; // 카메라가 보는 방향과, 시야를 가져온다.

		Vector3 dir = camera.ScreenToWorldPoint(mos);
		// 월드의 좌표를 클릭했을 때 화면에 자신이 보고있는 화면에 맞춰 좌표를 바꿔준다.

		if (Physics.Raycast(camera.transform.position, dir, out hit_, mos.z))
		{
			//target.position = hit.point; // 타겟을 레이캐스트가 충돌된 곳으로 옮긴다.
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
					_addAgentChannel.RaiseEvent(0, hit_.point, (int)GameObjectType.Monster);

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
					_removeAgentChannel.RaiseEvent(hit_.transform.gameObject.GetComponent<Monster>().agnet_id, Vector3.zero, 0);
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
					_setMoveTargetChannel.RaiseEvent(-1, hit_.point, 0);
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
					_addAgentChannel.RaiseEvent(0, hit_.point, (int)GameObjectType.Character);
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
					_setRaycastChannel.RaiseEvent(0, hit_.point, 0);
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
					_setMoveCharacterChannel.RaiseEvent(0, hit_.point, 0);
				}

				clickLeftOn = true;
			}
		}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _setLoginChannel.RaiseEvent(0, hit_.point, 0);
        }


    }
}
