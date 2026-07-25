using UnityEngine;

public class Actor : MonoBehaviour
{
    public int agnet_id;
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

    void Awake()
    {
        CreateHealthBar();
        session = FindObjectOfType<Session>();
        damageTextManager = DamageTextManager.Instance;
    }

    void CreateHealthBar()
    {
        GameObject healthBarObj = new GameObject("HealthBar");
        healthBarObj.transform.SetParent(transform);
        healthBarObj.transform.localPosition = new Vector3(0, 2f, 0);
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
            transform.position = next;
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
            damageTextManager.ShowDamage(agnet_id, damageTextPosition, damage, isCritical);
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
                Debug.LogWarning($"Actor {agnet_id} is dead, showing death effect...");
                // 사망 시 시각적 효과 (예: 색상 변경, 애니메이션 등)
                ShowDeathEffect();
                break;
                
            case syncnet.AIState.Destroyed:
                Debug.LogWarning($"Actor {agnet_id} is destroyed, removing from scene...");
                // 파괴 시 오브젝트 제거
                RemoveFromScene();
                break;
        }
    }

    protected virtual void ShowDeathEffect()
    {
        // 사망 시 시각적 효과
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // 회색으로 변경하여 사망 상태 표시
            renderer.material.color = Color.gray;
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
        if (session != null && session.game_objects.ContainsKey(agnet_id))
        {
            session.game_objects.Remove(agnet_id);
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
