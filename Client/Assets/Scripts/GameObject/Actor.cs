using UnityEngine;

public class Actor : MonoBehaviour
{
    public int agnet_id;
    public Vector3 pos;
    public syncnet.AIState state;
    public int health = 100;
    public bool input_locked;
    
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
