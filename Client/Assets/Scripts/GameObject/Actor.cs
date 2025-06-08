using UnityEngine;

public class Actor : MonoBehaviour
{
    public int agnet_id;
    public Vector3 pos;
    public syncnet.AIState state;
    public int health = 100;
    public bool input_locked;
    
    private HealthBar healthBar;

    void Awake()
    {
        CreateHealthBar();
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

    public void UpdateHealthUI(int currentHealth)
    {
        if (healthBar != null)
        {
            float healthPercent = Mathf.Clamp01(currentHealth / 100f);
            healthBar.UpdateHealth(healthPercent);
        }
    }

    public void TakeDamage(int damage)
    {
        health = Mathf.Clamp(health - damage, 0, 100);
        UpdateHealthUI(health);
    }
}
