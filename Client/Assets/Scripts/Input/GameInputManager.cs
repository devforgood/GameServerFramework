using System.Collections;
using System.Collections.Generic;
using UnityEngine;




public class GameInputManager : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask groundLayer;
    public float moveSpeed = 5f;


    private Transform playerTransform;
    private Vector3? targetPosition = null;

    // 스킬 관리
    private Dictionary<KeyCode, ISkill> skillKeyMap = new Dictionary<KeyCode, ISkill>();
    private ISkill currentSkill = null;
    public bool IsSkillActive { get; set; } = false;

    public Transform PlayerTransform => playerTransform;
    public Vector3 PlayerPosition { get => playerTransform.position; set => playerTransform.position = value; }

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // 스킬 등록
        skillKeyMap[KeyCode.F1] = new JumpSkill();
        skillKeyMap[KeyCode.F2] = new WhirlwindSkill();
        skillKeyMap[KeyCode.F3] = new ExplosionNoPhysics();

        currentSkill = skillKeyMap[KeyCode.F1]; // 기본 스킬 설정
    }

    void Update()
    {
        if (playerTransform == null)
            return;

        // 스킬 선택
        foreach (var kv in skillKeyMap)
        {
            if (Input.GetKeyDown(kv.Key))
            {
                if (currentSkill != null)
                    currentSkill.OnDeselect(this);
                currentSkill = kv.Value;
                currentSkill.OnSelect(this);
            }
        }

        // 스킬 발동(우클릭)
        if (currentSkill != null)
        {
            if (Input.GetMouseButtonDown(1))
                currentSkill.OnSkillButtonDown(this);
            if (Input.GetMouseButtonUp(1))
                currentSkill.OnSkillButtonUp(this);
            currentSkill.Update(this);
        }

        // 일반 이동(좌클릭)
        if (!IsSkillActive && Input.GetMouseButtonDown(0))
        {
            Vector3? dest = GetMouseGroundPosition();
            if (dest.HasValue)
            {
                Vector3 d = dest.Value;
                d.y = playerTransform.position.y;
                targetPosition = d;
            }
        }

        if (!IsSkillActive && targetPosition.HasValue)
        {
            playerTransform.position = Vector3.MoveTowards(
                playerTransform.position,
                targetPosition.Value,
                moveSpeed * Time.deltaTime
            );
            if (Vector3.Distance(playerTransform.position, targetPosition.Value) < 0.05f)
                targetPosition = null;
        }
    }

    // 마우스 위치의 지면 좌표 반환
    public Vector3? GetMouseGroundPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, 100f, groundLayer))
        {
            Vector3 dest = hit.point;
            dest.y = playerTransform.position.y;
            return dest;
        }
        return null;
    }
}
