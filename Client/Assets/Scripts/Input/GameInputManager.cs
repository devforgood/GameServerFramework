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
    // 데이터(gamedata) 기반 스킬 프리뷰: KeyCode→skillId. gamedata 는 리소스 로드 이후에야
    // 유효하므로 첫 선택 시 지연 생성해 캐싱한다(순수 연출 프리뷰, 네트워크 전송 없음).
    private Dictionary<KeyCode, int> fxSkillKeyMap = new Dictionary<KeyCode, int>();
    private Dictionary<int, Skill> fxSkillCache = new Dictionary<int, Skill>();
    private Skill currentSkill = null;
    public bool IsSkillActive { get; set; } = false;

    // 현재 선택된 스킬(HUD/외부 UI 용).
    public Skill CurrentSkill => currentSkill;

    // HUD 핫바 표시 순서(Dictionary 는 순서를 보장하지 않으므로 명시한다).
    private static readonly KeyCode[] HotbarOrder =
    {
        KeyCode.F1, KeyCode.F2, KeyCode.F3, KeyCode.F4,
        KeyCode.F5, KeyCode.F6, KeyCode.F7, KeyCode.F8, KeyCode.F9
    };
    private GUIStyle hudTitleStyle;
    private GUIStyle hudRowStyle;
    private Texture2D hudBoxTex;

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

        // 데이터 기반 D2 스킬 프리뷰(우클릭으로 발동). 실제 시전/데미지는 서버 경로(Session.UseSkill)가 담당.
        fxSkillKeyMap[KeyCode.F5] = 101; // Fireball(projectile)
        fxSkillKeyMap[KeyCode.F6] = 102; // Meteor(meteor)
        fxSkillKeyMap[KeyCode.F7] = 104; // Nova(nova)
        fxSkillKeyMap[KeyCode.F8] = 106; // Teleport(teleport)
        fxSkillKeyMap[KeyCode.F9] = 200; // Holy Fire(passive aura, 지면 링)

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

        // 데이터 기반 스킬 선택(지연 생성)
        foreach (var kv in fxSkillKeyMap)
        {
            if (Input.GetKeyDown(kv.Key))
            {
                var skill = GetOrCreateFxSkill(kv.Value);
                if (skill != null)
                {
                    if (currentSkill != null)
                        currentSkill.OnDeselect(this);
                    currentSkill = skill;
                    currentSkill.OnSelect(this);
                }
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

    // skillId 로 스킬 인스턴스를 지연 생성한다(gamedata 채움). 리소스 미로드면 null.
    // code_name 이 있으면 SkillFactory(AuraSkill 등), 없으면 fx 기반 ActiveFxSkill 로 감싼다.
    private Skill GetOrCreateFxSkill(int skillId)
    {
        if (fxSkillCache.TryGetValue(skillId, out var cached))
            return cached;

        Gamedata.Skill data = null;
        if (GameManager.Instance == null || GameManager.Instance.resource == null
            || !GameManager.Instance.resource.Skills.TryGetValue(skillId, out data))
            return null;

        Skill skill;
        if (!string.IsNullOrEmpty(data.code_name))
            skill = SkillFactory.Create(skillId); // AuraSkill 등 클라 클래스
        else
        {
            skill = new ActiveFxSkill();
            skill.gamedata = data;
        }

        fxSkillCache[skillId] = skill;
        return skill;
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

    // ── 현재 선택된 스킬 HUD (IMGUI — 씬/캔버스 세팅 없이 플레이하면 바로 표시) ──
    // 좌상단에 선택된 스킬을 크게, 아래에 F1~F9 핫바를 그리고 선택 항목을 강조한다.
    // 실제 네트워크 시전은 Shift+좌클릭(항상 skillId 1)이라는 힌트도 함께 표시한다.
    void OnGUI()
    {
        EnsureHudStyles();

        const float w = 240f;
        const float rowH = 20f;
        float x = 10f, y = 10f;
        float h = 30f + HotbarOrder.Length * rowH + 26f;

        GUI.DrawTexture(new Rect(x, y, w, h), hudBoxTex, ScaleMode.StretchToFill);

        GUI.Label(new Rect(x + 10, y + 6, w - 20, 22),
            $"선택된 스킬: {PrettyName(currentSkill)}", hudTitleStyle);

        float ly = y + 32f;
        foreach (var key in HotbarOrder)
        {
            string label;
            bool selected;
            if (skillKeyMap.TryGetValue(key, out var s))
            {
                label = PrettyName(s);
                selected = (currentSkill == s);
            }
            else if (fxSkillKeyMap.TryGetValue(key, out var id))
            {
                label = PrettyName(id);
                selected = fxSkillCache.TryGetValue(id, out var cached) && cached == currentSkill;
            }
            else
            {
                continue;
            }

            hudRowStyle.normal.textColor = selected ? new Color(1f, 0.9f, 0.3f) : Color.white;
            string mark = selected ? "▶ " : "   ";
            GUI.Label(new Rect(x + 10, ly, w - 20, rowH), $"{mark}[{key}] {label}", hudRowStyle);
            ly += rowH;
        }

        hudRowStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);
        GUI.Label(new Rect(x + 10, ly + 4, w - 20, rowH), "시전: 선택 후 우클릭 / 서버: Shift+좌클릭", hudRowStyle);
    }

    private void EnsureHudStyles()
    {
        if (hudBoxTex == null)
        {
            hudBoxTex = new Texture2D(1, 1);
            hudBoxTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.6f));
            hudBoxTex.Apply();
        }
        if (hudTitleStyle == null)
            hudTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 15, fontStyle = FontStyle.Bold };
        if (hudRowStyle == null)
            hudRowStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };
    }

    // 스킬 표시 이름. gamedata 가 있으면 name_id("fireball_name"→"fireball"), 없으면 클래스명.
    private string PrettyName(Skill s)
    {
        if (s == null) return "없음";
        if (s.gamedata != null) return PrettyName(s.gamedata.id);
        return s.GetType().Name.Replace("Skill", "");
    }

    private string PrettyName(int skillId)
    {
        if (GameManager.Instance != null && GameManager.Instance.resource != null
            && GameManager.Instance.resource.Skills.TryGetValue(skillId, out var d)
            && !string.IsNullOrEmpty(d.name_id))
        {
            return d.name_id.Replace("_name", "");
        }
        return $"skill {skillId}";
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
