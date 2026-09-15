using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 디아블로(3·4) 식 쿼터뷰 카메라.
///
///   - 방향은 고정한다. 캐릭터가 돌아도 카메라는 돌지 않아 화면 위쪽이 늘 같은 방위다.
///   - 내 캐릭터를 화면 중앙에 두고 따라간다.
///   - 휠로 줌한다. 가까워질수록 각도가 낮아져 캐릭터가 더 잘 보인다(디아블로 4 방식).
///
/// 씬마다 Main Camera 가 따로 있고 Session 은 씬을 넘어 유지되므로, 씬이 로드될 때마다
/// Main Camera 에 스스로 붙는다. 씬 파일은 고치지 않는다.
/// 내 캐릭터가 없는 씬(테스트 씬 등)에서는 카메라를 건드리지 않는다.
/// </summary>
[RequireComponent(typeof(Camera))]
public class DiabloCamera : MonoBehaviour
{
    [Header("시점")]
    [Tooltip("수평 방위(도). 0 이면 월드 +Z 가 화면 위쪽이다.")]
    public float yaw = 0f;
    [Tooltip("시야각. 좁을수록 원근 왜곡이 줄어 쿼터뷰 느낌이 강해진다.")]
    public float fieldOfView = 40f;
    [Tooltip("카메라가 겨누는 지점의 높이(발 기준). 몸통 가운데쯤.")]
    public float focusHeight = 1.0f;

    [Header("줌 (가까움 ↔ 멂)")]
    public float minDistance = 10f;
    public float maxDistance = 24f;
    [Tooltip("가장 가까울 때 내려다보는 각도")]
    public float minPitch = 45f;
    [Tooltip("가장 멀 때 내려다보는 각도")]
    public float maxPitch = 60f;
    [Range(0f, 1f), Tooltip("시작 줌 위치. 0 = 가장 가까움, 1 = 가장 멂")]
    public float zoom = 0.55f;
    public float zoomStep = 0.1f;
    public float zoomSmoothTime = 0.12f;

    [Header("추적")]
    [Tooltip("따라가는 지연. 0 에 가까울수록 딱 붙는다.")]
    public float followSmoothTime = 0.06f;
    [Tooltip("이보다 크게 튀면(텔레포트·게이트) 보간하지 않고 즉시 옮긴다.")]
    public float snapDistance = 8f;

    private Camera cam;
    private Transform target;
    private Vector3 focus;
    private Vector3 focusVelocity;
    private float currentZoom;
    private float zoomVelocity;
    private bool hasFocus;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AttachToMainCamera();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) => AttachToMainCamera();

    private static void AttachToMainCamera()
    {
        var main = Camera.main;
        if (main != null && main.GetComponent<DiabloCamera>() == null)
            main.gameObject.AddComponent<DiabloCamera>();
    }

    void Awake()
    {
        cam = GetComponent<Camera>();
        currentZoom = zoom;
    }

    void LateUpdate()
    {
        if (!ResolveTarget())
            return;

        // 처음 붙었을 때 FOV 를 바꾼다. 캐릭터가 없는 씬에서는 원래 카메라를 그대로 둔다.
        if (!hasFocus)
            cam.fieldOfView = fieldOfView;

        HandleZoom();

        Vector3 desired = target.position + Vector3.up * focusHeight;
        if (!hasFocus || Vector3.Distance(focus, desired) > snapDistance)
        {
            focus = desired;
            focusVelocity = Vector3.zero;
            hasFocus = true;
        }
        else
        {
            focus = Vector3.SmoothDamp(focus, desired, ref focusVelocity, followSmoothTime);
        }

        float pitch = Mathf.Lerp(minPitch, maxPitch, currentZoom);
        float distance = Mathf.Lerp(minDistance, maxDistance, currentZoom);
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
    }

    /// <summary>
    /// 내 캐릭터 오브젝트를 찾는다. 게이트 이동·재접속 때 actor id 와 오브젝트가 바뀌므로 매 프레임 확인한다.
    /// </summary>
    private bool ResolveTarget()
    {
        var session = Session.Instance;
        if (session == null)
            return false;

        GameObject player;
        if (!session.game_objects.TryGetValue(session.player_actor_id, out player) || player == null)
            return target != null; // 잠깐 사라진 경우(재생성 중)에는 마지막 위치에 머문다

        if (target != player.transform)
        {
            target = player.transform;
            hasFocus = false; // 새 캐릭터로 바뀌면 보간 없이 바로 옮긴다
        }
        return true;
    }

    private void HandleZoom()
    {
        float wheel = Input.mouseScrollDelta.y;
        if (wheel != 0f)
            zoom = Mathf.Clamp01(zoom - wheel * zoomStep); // 휠을 위로 굴리면 가까워진다

        currentZoom = Mathf.SmoothDamp(currentZoom, zoom, ref zoomVelocity, zoomSmoothTime);
    }
}
