using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(menuName = "Events/Session Event Channel")]
public class SessionChannelSO : ScriptableObject
{
	public UnityAction<int, Vector3, int> OnEventRaised;
	public void RaiseEvent(int agent_id, Vector3 pos, int type)
	{
		if (OnEventRaised != null)
			OnEventRaised.Invoke(agent_id, pos, type);
		else
        {
			Debug.LogError("event error");
        }
	}
}
