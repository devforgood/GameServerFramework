using UnityEngine;

public class Actor : MonoBehaviour
{
    public int agnet_id;
    public Vector3 pos;
    public syncnet.AIState state;
    public int health = 100;
    public bool input_locked;
    
    protected HealthBar healthBar;
    private Session session;

    void Awake()
    {
        CreateHealthBar();
        session = FindObjectOfType<Session>();
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
            Debug.Log($"HealthBar updated for {gameObject.name}: {currentHealth} ({healthPercent * 100}%)");
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
        health = Mathf.Clamp(health - damage, 0, 100);
        UpdateHealthUI(health);
    }

    public virtual void UpdateState(GameObject game_object, syncnet.AIState newState)
    {
        var oldState = state;
        state = newState;
        
        Debug.Log($"Actor {agnet_id} state changed: {oldState} -> {newState}");
        
        // 상태별 처리
        switch (state)
        {
            case syncnet.AIState.Dead:
                Debug.Log($"Actor {agnet_id} is dead, showing death effect...");
                // 사망 시 시각적 효과 (예: 색상 변경, 애니메이션 등)
                ShowDeathEffect();
                break;
                
            case syncnet.AIState.Destroyed:
                Debug.Log($"Actor {agnet_id} is destroyed, removing from scene...");
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
            Debug.Log($"Removed actor {agnet_id} from game_objects");
        }
        
        // 오브젝트 파괴
        Destroy(gameObject);
    }
}
