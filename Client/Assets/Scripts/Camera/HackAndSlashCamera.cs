using UnityEngine;

public class HackAndSlashCamera : MonoBehaviour
{
    [Header("필수 설정")]
    public Transform target; // 따라갈 대상(플레이어)

    [Header("카메라 세팅")]
    public float distance = 15.0f;      // 캐릭터와의 거리
    public float height = 6.0f;        // 캐릭터 위로의 높이
    public float angle = 45.0f;        // 쿼터뷰 각도(디아블로2는 약 45도)
    public float followSpeed = 8.0f;   // 따라가는 속도

    void Start()
    {
        if (target == null && GameObject.FindWithTag("Player") != null)
            target = GameObject.FindWithTag("Player").transform;
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // 쿼터뷰 각도에서의 오프셋 계산
        Quaternion rot = Quaternion.Euler(angle, 0, 0); // Y축 45도 회전(대각선), X축 쿼터뷰 각도
        Vector3 offset = rot * new Vector3(0, 0, -distance) + Vector3.up * height;

        // 목표 위치 계산
        Vector3 desiredPosition = target.position + offset;

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // 캐릭터를 바라봄
        transform.LookAt(target.position + Vector3.up * 1.5f);
    }
}