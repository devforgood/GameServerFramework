using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Actor
{
    public override void UpdateHealthUI(int currentHealth)
    {
        base.UpdateHealthUI(currentHealth);
        Debug.Log($"Monster {actor_id} health updated to: {currentHealth}");
    }

    public override void UpdateState(GameObject game_object, syncnet.AIState newState)
    {
        base.UpdateState(game_object, newState);
        Debug.Log($"Monster {actor_id} state changed to: {newState}");
    }
    
    protected override void ShowDeathEffect()
    {
        // 몬스터 전용 사망 효과
        var renderer = GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // 빨간색으로 변경하여 몬스터 사망 표시
            renderer.material.color = Color.red;
        }
        
        // HealthBar 숨기기
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }
        
        Debug.Log($"Monster {actor_id} death effect applied");
    }
}
