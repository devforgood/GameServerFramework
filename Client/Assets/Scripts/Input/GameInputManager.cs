using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask groundLayer;
    public float moveSpeed = 5f;

    private Transform playerTransform;
    private Vector3? targetPosition = null;

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

        if (Input.GetMouseButtonDown(0)) // 좌클릭
        {
            Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f, groundLayer))
            {
                // y좌표는 현재 플레이어의 y값을 사용
                Vector3 dest = hit.point;
                dest.y = playerTransform.position.y;
                targetPosition = dest;
            }
        }

        // 목표 위치로 이동
        if (targetPosition.HasValue)
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition.Value,
                moveSpeed * Time.deltaTime
            );

            // 도착하면 targetPosition 해제
            if (Vector3.Distance(playerTransform.position, targetPosition.Value) < 0.05f)
                targetPosition = null;
        }
    }
}
