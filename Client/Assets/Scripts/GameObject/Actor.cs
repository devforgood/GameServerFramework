using UnityEngine;

public class Actor : MonoBehaviour
{
    public int actor_id;
    public Vector3 pos;   // 서버가 마지막으로 알려준 위치(보간 목표)
    public syncnet.AIState state;
    public int health = 100;
    public bool input_locked;

    // ── 위치 보간(entity interpolation) ──
    // 서버 시뮬레이션은 10Hz(0.1초)라, 그 사이를 '직전 서버 위치 → 최신 서버 위치' 등속 보간으로 메운다.
    // 예전처럼 지수 Lerp 로 목표를 쫓으면 패킷이 올 때마다 속도가 튀었다가 잦아들어(빠르게 붙고 느려짐)
    // 돌진(차지)처럼 빠른 이동에서 출렁이고 끊겨 보인다. 등속 보간은 한 틱만큼 뒤에서 매끄럽게 따라간다.
    [HideInInspector] public Vector3 prevPos;        // 보간 시작점(직전 서버 위치)
    [HideInInspector] public float posReceivedTime;  // pos 를 받은 시각
    [HideInInspector] public float posInterval = 0.1f; // 보간 구간 길이(직전 수신과의 간격)


    public HealthBar healthBar;
    private Session session;
    private DamageTextManager damageTextManager;

    // Damage text cooldown to prevent spam
    private float lastDamageTime = 0f;
    private const float DAMAGE_TEXT_COOLDOWN = 0.1f; // 100ms cooldown

    // ── 로코모션(걷기) ──
    // 서버는 속도도 바라보는 방향도 보내지 않는다. 그래서 '실제로 그려진 위치가
    // 프레임 사이에 얼마나 움직였는가'를 재서 애니메이션을 고른다.
    // 이렇게 하면 누가 transform 을 움직였든(서버 동기화, 점프 연출) 똑같이 동작한다.
    protected Animator locomotionAnimator;
    private Vector3 lastFramePos;
    private float smoothedSpeed;
    private static readonly int SpeedParam = Animator.StringToHash("Speed");

    /// <summary>이 속도 아래는 멈춘 것으로 본다. 보간 지터로 발이 떠는 것을 막는다.</summary>
    private const float MoveEpsilon = 0.05f;
    /// <summary>속도 평활 계수. 클수록 빨리 반응한다.</summary>
    private const float SpeedDamping = 12f;
    /// <summary>바라보는 방향이 도는 속도(도/초).</summary>
    private const float TurnSpeed = 720f;

    // ── 발 미끄러짐 보정 ──
    // 애니메이션 클립은 저마다 '이 속도로 걷는다'는 보폭을 갖고 있다. 실제 이동이 그보다 빠르면
    // 딱 그 차이만큼 발이 땅에서 미끄러진다. 그래서 넘어가는 만큼 재생 속도를 올려 보폭을 맞춘다.
    //
    // 값은 CharacterResourceTool 이 컨트롤러의 블렌드 문턱값에서 읽어 프리팹에 새겨 준다.
    // 직접 고치지 말 것 — 컨트롤러를 다시 만들면 덮어쓴다.
    [HideInInspector] public float locomotionTopSpeed = 1.69f;

    /// <summary>재생 속도 배율 상한. 이보다 올리면 다리가 우스울 만큼 빨라진다.</summary>
    private const float MaxPlaybackScale = 1.8f;

    void Awake()
    {
        CreateHealthBar();
        session = FindObjectOfType<Session>();
        damageTextManager = DamageTextManager.Instance;

        // 모델은 자식으로 붙어 있고 Animator 도 거기 있다. 캡슐 프리팹이면 null 이라 그냥 건너뛴다.
        locomotionAnimator = GetComponentInChildren<Animator>();
        lastFramePos = transform.position;
    }

    /// <summary>
    /// 위치는 Session.Update 안의 ActorSync.Tick 이 쓴다. 그 뒤에 도는 LateUpdate 에서
    /// 이번 프레임 이동량을 재야 한 프레임 밀리지 않는다.
    /// </summary>
    void LateUpdate()
    {
        // 사망 연출로 애니메이터를 꺼 둔 경우 — 멈춘 포즈를 유지한다.
        if (locomotionAnimator == null || !locomotionAnimator.enabled) return;

        Vector3 delta = transform.position - lastFramePos;
        delta.y = 0f; // 오르막을 걷는다고 더 빨리 걷는 것처럼 보이면 안 된다
        lastFramePos = transform.position;

        float instant = Time.deltaTime > 0f ? delta.magnitude / Time.deltaTime : 0f;
        if (instant < MoveEpsilon) instant = 0f;

        // 서버 갱신은 10Hz 라 프레임별 이동량이 고르지 않다. 그대로 넣으면 걷기가 깜빡인다.
        smoothedSpeed = Mathf.Lerp(smoothedSpeed, instant, 1f - Mathf.Exp(-SpeedDamping * Time.deltaTime));
        locomotionAnimator.SetFloat(SpeedParam, smoothedSpeed);

        // 문턱값 안쪽은 블렌드 트리가 보폭을 맞춰 주므로 등속으로 재생한다.
        // 그 위로는 클립이 따라오지 못하니 재생 속도로 메운다(상한까지).
        float scale = 1f;
        if (locomotionTopSpeed > 0.01f && smoothedSpeed > locomotionTopSpeed)
            scale = Mathf.Min(smoothedSpeed / locomotionTopSpeed, MaxPlaybackScale);
        locomotionAnimator.speed = scale;

        // 서버가 방향을 안 보내므로 이동 방향으로 직접 돌린다.
        // 이게 없으면 걷는 자세 그대로 옆으로 미끄러진다.
        if (instant > 0f)
        {
            var look = Quaternion.LookRotation(delta.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, look, TurnSpeed * Time.deltaTime);
        }
    }

    void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        // 모델마다 키가 달라(캐릭터 2.16m, 스켈레톤 2.56m) 콜라이더 높이 위에 띄운다.
        // 콜라이더는 CharacterResourceTool 이 모델 경계에 맞춰 둔다.
        var capsule = GetComponent<CapsuleCollider>();
        float barHeight = capsule != null ? capsule.center.y + capsule.height * 0.5f + 0.2f : 2f;
        healthBarObj.transform.localPosition = new Vector3(0, barHeight, 0);
        healthBarObj.transform.localRotation = Quaternion.identity;
        
        healthBar = healthBarObj.AddComponent<HealthBar>();
        healthBar.Initialize();
        UpdateHealthUI(health);
    }

    public virtual void UpdateHealthUI(int currentHealth)
    {
        if (healthBar != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / 100f);
            healthBar.UpdateHealth(healthPercent);
        }
        else
        {
            Debug.LogWarning($"HealthBar not found for {gameObject.name}, recreating...");
            CreateHealthBar();
            if (healthBar != null)
            {
                float healthPercent = Mathf.Clamp01(currentHealth / 100f);
                healthBar.UpdateHealth(healthPercent);
            }
        }
    }

    /// <summary>
    /// 서버가 보낸 새 위치를 보간 목표로 받는다. 스폰/텔레포트/게이트처럼 한 번에 크게 뛴
    /// 경우(snapDistance 초과)는 보간하지 않고 즉시 스냅한다 — 그 사이를 걸어가면 벽을 통과해 보인다.
    /// </summary>
    public void SetServerPosition(Vector3 next, float snapDistance)
    {
        bool first = posReceivedTime <= 0f;
        bool jumped = Vector3.Distance(pos, next) > snapDistance;

        if (first || jumped)
        {
            prevPos = next;
            transform.position = GroundedPosition(next);
        }
        else
        {
            prevPos = pos; // 직전 목표 → 새 목표 구간을 등속으로 채운다
        }

        posInterval = first ? 0.1f : Mathf.Clamp(Time.time - posReceivedTime, 0.02f, 0.5f);
        posReceivedTime = Time.time;
        pos = next;
    }

    /// <summary>이번 프레임에 그릴 위치. 서버 갱신 간격을 등속으로 나눠 채운다(넘으면 최신 위치에 멈춘다).</summary>
    public Vector3 InterpolatedPosition()
    {
        if (posReceivedTime <= 0f)
            return transform.position;

        float t = posInterval > 0f ? (Time.time - posReceivedTime) / posInterval : 1f;
        return Vector3.Lerp(prevPos, pos, Mathf.Clamp01(t));
    }

    // ── 바닥 붙이기 ──
    // 서버 좌표의 y 는 navmesh 표면 높이다. Recast 는 바닥을 복셀로 쌓으며 높이를 최소 한 칸
    // (cellHeight, 지금 0.2 m) 올림하므로 평지에서도 navmesh 가 실제 바닥보다 떠 있다(서버 로그의 y 가
    // 전부 0.2 인 이유). 경사로에서는 detail mesh 오차까지 더해진다. 그 y 를 그대로 쓰면 캐릭터가 뜬다.
    //
    // 그래서 그릴 때만 서버 위치 근처의 실제 바닥 콜라이더 높이에 발을 붙인다. 이동 판정은 여전히
    // 서버 navmesh 가 하므로 x/z 는 건드리지 않는다. 바닥 콜라이더가 없는 씬에서는 서버 y 를 그대로 쓴다.
    private const float GroundProbeUp = 1f;
    private const float GroundProbeDown = 1f;

    // 바닥으로 인정하는 navmesh 와의 차이. navmesh 는 바닥보다 최대 한 칸(0.2) 남짓 높다.
    // 그보다 크게 벌어지면 서 있는 면이 아니라 그 밑의 다른 면을 맞힌 것이다 — Starting Village 의
    // 경사로는 콜라이더가 없어서 레이가 밑의 평지에 닿는데, 거기 붙이면 캐릭터가 경사로에 파묻힌다
    // (실측: 평지 차이 0.2, 경사로에서 밑바닥까지 0.3~1.0). 그럴 때는 서버 y 를 그대로 쓴다.
    private const float MaxNavMeshAboveGround = 0.35f;
    private const float MaxNavMeshBelowGround = 0.25f;
    private static readonly RaycastHit[] groundHits = new RaycastHit[8];

    // 멈춰 있을 때까지 매 프레임 레이를 쏘지 않도록 마지막으로 잰 자리와 결과를 기억한다.
    private Vector3 lastGroundProbe = new Vector3(float.NaN, 0f, 0f);
    private float lastGroundY;

    public Vector3 GroundedPosition(Vector3 serverPos)
    {
        if (!(Mathf.Abs(serverPos.x - lastGroundProbe.x) < 0.01f &&
              Mathf.Abs(serverPos.z - lastGroundProbe.z) < 0.01f &&
              Mathf.Abs(serverPos.y - lastGroundProbe.y) < 0.01f))
        {
            lastGroundProbe = serverPos;
            lastGroundY = SnapToGround(serverPos).y;
        }
        return new Vector3(serverPos.x, lastGroundY, serverPos.z);
    }

    /// <summary>서버 위치 바로 아래(또는 조금 위)의 바닥 높이로 y 를 맞춘다. 못 찾으면 그대로 돌려준다.</summary>
    public static Vector3 SnapToGround(Vector3 serverPos)
    {
        int count = Physics.RaycastNonAlloc(serverPos + Vector3.up * GroundProbeUp, Vector3.down, groundHits,
            GroundProbeUp + GroundProbeDown, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        bool found = false;
        float groundY = serverPos.y;
        float bestGap = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = groundHits[i];
            if (hit.normal.y < 0.5f)
                continue; // 벽·장애물 옆면
            if (hit.collider.GetComponentInParent<Actor>() != null)
                continue; // 겹쳐 선 다른 캐릭터·몬스터 머리 위에 올라서면 안 된다

            float above = serverPos.y - hit.point.y;
            if (above > MaxNavMeshAboveGround || above < -MaxNavMeshBelowGround)
                continue;

            // 범위 안에 면이 둘이면(겹친 바닥) 서버 높이에 가까운 쪽이 서 있는 곳이다.
            float gap = Mathf.Abs(above);
            if (gap < bestGap)
            {
                bestGap = gap;
                groundY = hit.point.y;
                found = true;
            }
        }

        return found ? new Vector3(serverPos.x, groundY, serverPos.z) : serverPos;
    }

    public void TakeDamage(int damage)
    {
        // Show damage text UI with cooldown check
        ShowDamageText(damage);
        
        // Optional: Add screen shake or other effects
        // AddScreenShake(damage);
    }
    
    private void ShowDamageText(int damage)
    {
        // Check cooldown to prevent spam
        if (Time.time - lastDamageTime < DAMAGE_TEXT_COOLDOWN)
        {
            return;
        }
        
        lastDamageTime = Time.time;
        
        // Determine if it's a critical hit (you can implement your own logic)
        bool isCritical = damage > 20; // Example: damage over 20 is critical
        
        // Use current actor position instead of potentially outdated healthBar position
        Vector3 damageTextPosition = transform.position + Vector3.up * 2.5f; // Show above the actor
        
        // Use cached DamageTextManager instance
        if (damageTextManager != null)
        {
            damageTextManager.ShowDamage(actor_id, damageTextPosition, damage, isCritical);
        }
        else
        {
            Debug.LogError("DamageTextManager is null, cannot show damage text");
        }
    }

    public virtual void UpdateState(GameObject game_object, syncnet.AIState newState)
    {
        var oldState = state;
        state = newState;
        
        // 상태별 처리
        switch (state)
        {
            case syncnet.AIState.Dead:
                Debug.LogWarning($"Actor {actor_id} is dead, showing death effect...");
                // 사망 시 시각적 효과 (예: 색상 변경, 애니메이션 등)
                ShowDeathEffect();
                break;
                
            case syncnet.AIState.Destroyed:
                Debug.LogWarning($"Actor {actor_id} is destroyed, removing from scene...");
                // 파괴 시 오브젝트 제거
                RemoveFromScene();
                break;
        }
    }

    protected virtual void ShowDeathEffect()
    {
        // 사망 시 시각적 효과
        // 캡슐 시절에는 루트에 MeshRenderer 가 있었지만, 실제 캐릭터 모델은
        // 자식 SkinnedMeshRenderer 로 그려진다. 둘 다 잡으려고 자식까지 훑는다.
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var renderer in renderers)
        {
            if (renderer == null) continue;
            // 회색으로 변경하여 사망 상태 표시
            foreach (var mat in renderer.materials)
            {
                if (mat != null && mat.HasProperty("_Color"))
                    mat.color = Color.gray;
            }
        }
        
        // HealthBar 숨기기
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
    }

    protected virtual void RemoveFromScene()
    {
        // Session의 game_objects에서 제거
        if (session != null && session.game_objects.ContainsKey(actor_id))
        {
            session.game_objects.Remove(actor_id);
        }
        
        // 오브젝트 파괴
        Destroy(gameObject);
    }

    public void UpdateHealth(int health) 
    {
        var oldHealth = this.health;
        if(oldHealth > health)
        {
            int damage = oldHealth - health;
            TakeDamage(damage);
        }

        this.health = health;
        UpdateHealthUI(health);
    }

}
