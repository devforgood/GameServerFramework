using UnityEngine;

public class HealthBar : MonoBehaviour
{
    public Actor actor;
    public GameObject healthBar;
    public int health;

    void Update()
    {
        // 항상 카메라를 향하도록 회전 (Billboard 효과)
        if (Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }
    }

    public void UpdateHealthUI(int currentHealth)
    {
        health = currentHealth;
        // 체력바 크기 업데이트 (x축 스케일만 변경)
        Vector3 scale = healthBar.transform.localScale;
        scale.x = Mathf.Clamp01(currentHealth / 100f);
        healthBar.transform.localScale = scale;

        // 체력에 따라 색상 변경
        SpriteRenderer spriteRenderer = healthBar.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            if (currentHealth > 70)
                spriteRenderer.color = Color.green;
            else if (currentHealth > 30)
                spriteRenderer.color = Color.yellow;
            else
                spriteRenderer.color = Color.red;
        }
    }
} 