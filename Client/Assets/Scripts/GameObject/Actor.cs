using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Actor : MonoBehaviour
{
    public int agnet_id;
    public Vector3 pos;
    public syncnet.AIState state;
    public int health = 100;
    public bool input_locked;
    public GameObject healthBar;

    void Awake() {
        // 코드로 HP을 생성
        healthBar = new GameObject("HealthBar");
        healthBar.transform.SetParent(transform);
        healthBar.transform.localPosition = new Vector3(0, 1.5f, 0);
        healthBar.transform.localScale = new Vector3(1, 1, 1);

        // 체력 바 이미지 생성
        GameObject healthBarImage = new GameObject("HealthBarImage");
        healthBarImage.transform.SetParent(healthBar.transform);
        healthBarImage.transform.localPosition = Vector3.zero;
        healthBarImage.transform.localScale = new Vector3(1, 1, 1);

        // 체력 바 이미지 설정
        SpriteRenderer spriteRenderer = healthBarImage.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = Resources.Load<Sprite>("HealthBar");
        spriteRenderer.color = Color.red;

        // 체력 바 스크립트 추가
        HealthBar healthBarScript = healthBar.AddComponent<HealthBar>();
        healthBarScript.actor = this;
        healthBarScript.healthBar = healthBar;
        healthBarScript.health = health;
        healthBarScript.UpdateHealthUI(health);

    }

    public void UpdateHealthUI(int health) {
        healthBar.transform.localScale = new Vector3(health / 100f, 1, 1);
    }


}
