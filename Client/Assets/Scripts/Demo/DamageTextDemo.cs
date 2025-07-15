using UnityEngine;

public class DamageTextDemo : MonoBehaviour
{
    [Header("Demo Settings")]
    public KeyCode damageKey = KeyCode.Space;
    public KeyCode healKey = KeyCode.H;
    public KeyCode criticalKey = KeyCode.C;
    public KeyCode poisonKey = KeyCode.P;
    public KeyCode fireKey = KeyCode.F;
    
    [Header("Damage Values")]
    public int minDamage = 5;
    public int maxDamage = 50;
    public int healAmount = 25;
    public int criticalDamage = 100;
    public int poisonDamage = 15;
    public int fireDamage = 30;
    
    private Actor actor;
    
    void Start()
    {
        actor = GetComponent<Actor>();
        if (actor == null)
        {
            Debug.LogWarning("Actor component not found on this GameObject");
        }
    }
    
    void Update()
    {
        if (actor == null) return;
        
        // Normal damage
        if (Input.GetKeyDown(damageKey))
        {
            int damage = Random.Range(minDamage, maxDamage + 1);
            actor.TakeDamage(damage);
            Debug.Log($"Dealt {damage} damage");
        }
        
        // Heal
        if (Input.GetKeyDown(healKey))
        {
            // Show heal text near health bar position
            if (actor != null && actor.healthBar != null)
            {
                Vector3 healthBarPosition = actor.healthBar.transform.position;
                Vector3 healPosition = healthBarPosition + Vector3.up * 0.5f;
                AdvancedDamageText healText = AdvancedDamageText.CreateDamageText(healPosition);
                healText.ShowAdvancedDamage(healAmount, false, true, DamageType.Heal);
            }
            Debug.Log($"Healed {healAmount} HP");
        }
        
        // Critical damage
        if (Input.GetKeyDown(criticalKey))
        {
            actor.TakeDamage(criticalDamage);
            Debug.Log($"Dealt critical damage: {criticalDamage}");
        }
        
        // Poison damage
        if (Input.GetKeyDown(poisonKey))
        {
            Vector3 poisonPosition = transform.position + Vector3.up * 2.5f;
            AdvancedDamageText poisonText = AdvancedDamageText.CreateDamageText(poisonPosition);
            poisonText.ShowAdvancedDamage(poisonDamage, false, false, DamageType.Poison);
            Debug.Log($"Dealt poison damage: {poisonDamage}");
        }
        
        // Fire damage
        if (Input.GetKeyDown(fireKey))
        {
            Vector3 firePosition = transform.position + Vector3.up * 2.5f;
            AdvancedDamageText fireText = AdvancedDamageText.CreateDamageText(firePosition);
            fireText.ShowAdvancedDamage(fireDamage, false, false, DamageType.Fire);
            Debug.Log($"Dealt fire damage: {fireDamage}");
        }
        
        // Rapid damage test
        if (Input.GetKey(KeyCode.R))
        {
            int damage = Random.Range(1, 10);
            actor.TakeDamage(damage);
        }
    }
    
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 250));
        GUILayout.Label("Damage Text Demo Controls:");
        GUILayout.Label($"Press {damageKey} for normal damage");
        GUILayout.Label($"Press {healKey} for heal effect");
        GUILayout.Label($"Press {criticalKey} for critical damage");
        GUILayout.Label($"Press {poisonKey} for poison damage");
        GUILayout.Label($"Press {fireKey} for fire damage");
        GUILayout.Label("Hold R for rapid damage test");
        GUILayout.EndArea();
    }
} 