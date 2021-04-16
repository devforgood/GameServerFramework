//
// Attach this script to your camera in order to use depth nodes in forward rendering
//

#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Camera))]
public class EnableCameraDepthInForwardDA : MonoBehaviour {
#if UNITY_EDITOR
	void OnDrawGizmos(){
		Set();
	}
#endif
	void Start () {
		Set();
	}
	void Set(){
		if(GetComponent<Camera>().depthTextureMode == DepthTextureMode.None)
			GetComponent<Camera>().depthTextureMode = DepthTextureMode.Depth;
	}
}
#endif