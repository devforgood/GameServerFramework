using System.Collections;
using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask groundLayer;
    public float moveSpeed = 5f;
    public float jumpDuration = 1f;
    public float jumpHeight = 3f;
    public float whirlwindSpeed = 5f;
    public float whirlwindRotateSpeed = 5220f;

    private Transform playerTransform;
    private Vector3? targetPosition = null;
    private bool isJumping = false;
    private bool isWhirlwind = false;

    private enum SkillType { None, Jump, Whirlwind }
    private SkillType selectedSkill = SkillType.None;

    // 휠윈드용 목적지
    private Vector3 whirlwindTarget;
    private Coroutine whirlwindCoroutine;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        // F1: 점프 스킬 선택
        if (Input.GetKeyDown(KeyCode.F1))
        {
            selectedSkill = SkillType.Jump;
            isWhirlwind = false;
        }

        // F2: 휠윈드 스킬 선택
        if (Input.GetKeyDown(KeyCode.F2))
        {
            selectedSkill = SkillType.Whirlwind;
        }

        // 마우스 좌클릭: 일반 이동
        if (!isJumping && !isWhirlwind && Input.GetMouseButtonDown(0))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                Vector3 dest = hit.point;
                dest.y = playerTransform.position.y;
                targetPosition = dest;
            }
        }

        // 일반 이동 처리
        if (!isJumping && !isWhirlwind && targetPosition.HasValue)
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition.Value,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(playerTransform.position, targetPosition.Value) < 0.05f)
                targetPosition = null;
        }

        // 휠윈드: 우클릭을 누르고 있는 동안 발동
        if (!isJumping && selectedSkill == SkillType.Whirlwind)
        {
            if (Input.GetMouseButtonDown(1) && !isWhirlwind)
            {
                targetPosition = null;
                // 휠윈드 시작
                UpdateWhirlwindTarget();
                whirlwindCoroutine = StartCoroutine(WhirlwindMove(whirlwindSpeed, whirlwindRotateSpeed));
            }
            else if (Input.GetMouseButton(1) && isWhirlwind)
            {
                // 휠윈드 중 목적지 계속 갱신
                UpdateWhirlwindTarget();
            }
            else if (Input.GetMouseButtonUp(1) && isWhirlwind)
            {
                // 휠윈드 종료
                StopCoroutine(whirlwindCoroutine);
                isWhirlwind = false;
                whirlwindCoroutine = null;
            }
        }

        // 점프 스킬: 선택 후 우클릭으로 발동(기존과 동일)
        if (!isJumping && !isWhirlwind && selectedSkill == SkillType.Jump && Input.GetMouseButtonDown(1))
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                Vector3 dest = hit.point;
                dest.y = playerTransform.position.y;
                targetPosition = null;
                StartCoroutine(JumpToPosition(playerTransform.position, dest, jumpDuration, jumpHeight));
            }
        }
    }

    void UpdateWhirlwindTarget()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 dest = hit.point;
            dest.y = playerTransform.position.y;
            whirlwindTarget = dest;
        }
    }

    IEnumerator JumpToPosition(Vector3 start, Vector3 end, float duration, float height)
    {
        isJumping = true;
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            float yOffset = Mathf.Sin(Mathf.PI * t) * height;
            playerTransform.position = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            time += Time.deltaTime;
            yield return null;
        }
        playerTransform.position = end;
        isJumping = false;
    }

    IEnumerator WhirlwindMove(float speed, float rotateSpeed)
    {
        isWhirlwind = true;
        while (isWhirlwind)
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                whirlwindTarget,
                speed * Time.deltaTime
            );
            playerTransform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.Self);

            // 목적지에 거의 도달하면 멈추지 않고, 마우스 위치가 바뀌면 계속 이동
            yield return null;
        }
    }
}
