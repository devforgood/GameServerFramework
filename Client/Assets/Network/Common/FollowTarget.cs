/*  This file is part of the "Tanks Multiplayer" project by Rebound Games.
 *  You are only allowed to use these resources if you've bought them from the Unity Asset Store.
 * 	You shall not license, sublicense, sell, resell, transfer, assign, distribute or
 * 	otherwise make available to any third party the Service or the Content. */
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IOS || UNITY_ANDROID
using UnityEngine;
//using DG.Tweening;

    /// <summary>
    /// Camera script for following the player or a different target transform.
    /// Extended with ability to hide certain layers (e.g. UI) while in "follow mode".
    /// </summary>
public class FollowTarget : MonoBehaviour
{
    public bool isActive = true;
    /// <summary>
    /// The camera target to follow.
    /// Automatically picked up in LateUpdate().
    /// </summary>
    public Transform target;
        
    /// <summary>
    /// Layers to hide after calling HideMask().
    /// </summary>
    public LayerMask respawnMask;

    /// <summary>
    /// The clamped distance in the x-z plane to the target.
    /// </summary>
    public float distance = 10.0f;
        
    /// <summary>
    /// The clamped height the camera should be above the target.
    /// </summary>
    public float height = 5.0f;
    public float minHeight = 10.0f;

    [Header("-카메라 이동 가능한 범위")]
    public bool isCamMoveRect;
    public Rect CamMoveRect;
    /// <summary>
    /// Reference to the Camera component.
    /// </summary>
    [HideInInspector]
    public Camera cam;
        
    /// <summary>
    /// Reference to the camera Transform.
    /// </summary>
    [HideInInspector]
    public Transform camTransform;

    [HideInInspector]
    public static FollowTarget Instance;
    //카메라 흔드는 연출.
    bool isShake = false;
    float shakeStrength;
    float shakeSpeed;

    private void Awake()
    {
        Instance = this;
    }

    //initialize variables
    void Start()
    {
        cam = GetComponent<Camera>();
        camTransform = transform;

        //the AudioListener for this scene is not attached directly to this camera,
        //but to a separate gameobject parented to the camera. This is because the
        //camera is usually positioned above the player, however the AudioListener
        //should consider audio clips from the position of the player in 3D space.
        //so here we position the AudioListener child object at the target position.
        //Remark: parenting the AudioListener to the player doesn't work, because
        //it gets disabled on death and therefore stops playing sounds completely
        Transform listener = GetComponentInChildren<AudioListener>().transform;
        listener.position = transform.position + transform.forward * distance;
    }

    public void ShakeCam(float duration = 0.3f, float strength = 0.1f, float speed = 10.0f)
    {
        //isShake = true;
        //shakeStrength = strength;
        //shakeSpeed = speed;
        //cam.transform.DOScale(1, duration).OnComplete(() => {
        //    isShake = false;
        //});
    }
    //position the camera in every frame
    void LateUpdate()
    {
        if (!isActive)
            return;
        //cancel if we don't have a target
        if (!target)
            return;

        //convert the camera's transform angle into a rotation
        Quaternion currentRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

        //set the position of the camera on the x-z plane to:
        //distance units behind the target, height units above the target
        Vector3 pos = target.position;
        pos -= currentRotation * Vector3.forward * Mathf.Abs(distance);
        pos.y = target.position.y + Mathf.Abs(height);
        transform.position = pos;
        //look at the target
        transform.LookAt(target);

        //clamp distance
        transform.position = target.position - (transform.forward * Mathf.Abs(distance));

        if (isCamMoveRect)
        {
            if (transform.position.x < CamMoveRect.xMin)
                transform.position = new Vector3(CamMoveRect.xMin, transform.position.y, transform.position.z);
            if (transform.position.x > CamMoveRect.xMax)
                transform.position = new Vector3(CamMoveRect.xMax, transform.position.y, transform.position.z);
            if (transform.position.z < CamMoveRect.yMin)
                transform.position = new Vector3(transform.position.x, transform.position.y, CamMoveRect.yMin);
            if (transform.position.z > CamMoveRect.yMax)
                transform.position = new Vector3(transform.position.x, transform.position.y, CamMoveRect.yMax);

            if (transform.position.y < minHeight)
                transform.position = new Vector3(transform.position.x, minHeight, transform.position.z);
        }
        if (isShake)
            transform.position += new Vector3(Mathf.Sin(Time.time * shakeSpeed) * Random.insideUnitCircle.x * shakeStrength, 0, Mathf.Sin(Time.time * shakeSpeed) * Random.insideUnitCircle.y * shakeStrength);
    }

    public void EndCamAni()
    {
        GetComponent<Animator>().enabled = false;
        //트윈으로 카메라 위치 이동.
        //transform.DOMove(target.position - (transform.forward * Mathf.Abs(distance)), 0.8f).OnComplete(() => { isActive = true; });
    }

    /// <summary>
    /// Culls the specified layers of 'respawnMask' by the camera.
    /// </summary>
    public void HideMask(bool shouldHide)
    {
        if(shouldHide) cam.cullingMask &= ~respawnMask;
        else cam.cullingMask |= respawnMask;
    }
}
#endif
