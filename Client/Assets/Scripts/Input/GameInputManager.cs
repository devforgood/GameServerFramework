using syncnet;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameInputManager : MonoBehaviour
{
    public Camera mainCamera;
    public LayerMask groundLayer;
    public float moveSpeed = 5f;

    private Transform playerTransform;
    public Vector3? targetPosition = null;

    // 스킬 관리
    private Dictionary<KeyCode, Skill> skillKeyMap = new Dictionary<KeyCode, Skill>();
    private Skill currentSkill = null;
    public bool IsSkillActive { get; set; } = false;

    public Transform PlayerTransform => playerTransform;
    public Vector3 PlayerPosition { get => playerTransform.position; set => playerTransform.position = value; }

    public GameObject player = null;

    // 디버그 드로잉을 위한 변수들
    private LineRenderer arcLineRenderer;
    private const int ARC_POINT_COUNT = 21;

    void Start()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        player = GameObject.FindWithTag("Player");
        if (player != null)
            playerTransform = player.transform;

        // 스킬 등록
        skillKeyMap[KeyCode.F1] = new NormalAttackSkill();
        skillKeyMap[KeyCode.F2] = new JumpSkill();
        skillKeyMap[KeyCode.F3] = new WhirlwindSkill();
        skillKeyMap[KeyCode.F4] = new ExplosionNoPhysics();

        currentSkill = skillKeyMap[KeyCode.F1]; // 기본 스킬 설정

        // LineRenderer 초기화
        InitializeArcLineRenderer();
    }

    private void InitializeArcLineRenderer()
    {
        GameObject lineObj = new GameObject("SkillArcLine");
        lineObj.transform.parent = transform;
        arcLineRenderer = lineObj.AddComponent<LineRenderer>();
        arcLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        arcLineRenderer.startColor = Color.red;
        arcLineRenderer.endColor = Color.red;
        arcLineRenderer.startWidth = 0.1f;
        arcLineRenderer.endWidth = 0.1f;
        arcLineRenderer.positionCount = ARC_POINT_COUNT;
        arcLineRenderer.useWorldSpace = true;
        arcLineRenderer.enabled = false;
    }

    public void DrawArc(Vector3 center, float radius, float startAngle, float endAngle, Color color)
    {
        if (arcLineRenderer == null)
            InitializeArcLineRenderer();

        arcLineRenderer.enabled = true;
        arcLineRenderer.startColor = new Color(color.r, color.g, color.b, 0.5f);
        arcLineRenderer.endColor = new Color(color.r, color.g, color.b, 0.5f);

        float angleStep = (endAngle - startAngle) / (ARC_POINT_COUNT - 1);
        
        for (int i = 0; i < ARC_POINT_COUNT; i++)
        {
            float angle = (startAngle + angleStep * i) * Mathf.Deg2Rad;
            float x = Mathf.Sin(angle) * radius;
            float z = Mathf.Cos(angle) * radius;
            Vector3 pos = center + new Vector3(x, 0.1f, z);
            arcLineRenderer.SetPosition(i, pos);
        }
    }

    public void ClearArcDraw()
    {
        if (arcLineRenderer != null)
            arcLineRenderer.enabled = false;
    }

    void OnDestroy()
    {
        if (arcLineRenderer != null)
            Destroy(arcLineRenderer.gameObject);
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

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (playerTransform == null || currentSkill == null) return;

        Vector3? mousePos = GetMouseGroundPosition();
        if (!mousePos.HasValue) return;

        if (currentSkill is ExplosionNoPhysics explosionSkill)
        {
            float radius = explosionSkill.radius;
            Vector3 center = mousePos.Value;
            
            UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidDisc(center, Vector3.up, radius);
            UnityEditor.Handles.color = new Color(1f, 0.3f, 0f, 1f);
            UnityEditor.Handles.DrawWireDisc(center, Vector3.up, radius);
        }
        else if (currentSkill is NormalAttackSkill normalAttackSkill)
        {
            Vector3 center = playerTransform.position;
            Vector3 direction = (mousePos.Value - center).normalized;
            float targetAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;

            float startAngle = targetAngle - normalAttackSkill.attackAngle / 2;

            // 부채꼴 면 그리기 (반투명)
            UnityEditor.Handles.color = new Color(1f, 0f, 0f, 0.3f);
            UnityEditor.Handles.DrawSolidArc(center, Vector3.up, 
                Quaternion.Euler(0, startAngle, 0) * Vector3.forward, 
                normalAttackSkill.attackAngle, normalAttackSkill.attackRadius);

            // 테두리 그리기 (실선)
            UnityEditor.Handles.color = new Color(1f, 0f, 0f, 1f);
            UnityEditor.Handles.DrawWireArc(center, Vector3.up, 
                Quaternion.Euler(0, startAngle, 0) * Vector3.forward, 
                normalAttackSkill.attackAngle, normalAttackSkill.attackRadius);
        }
        else if (currentSkill is JumpSkill || currentSkill is WhirlwindSkill)
        {
            // 목표 지점만 간단히 표시
            Gizmos.color = currentSkill is JumpSkill ? Color.green : Color.blue;
            Gizmos.DrawSphere(mousePos.Value, 0.1f);
        }
    }
#endif
}
