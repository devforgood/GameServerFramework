using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monster : Actor
{
    // MonsterAnimationTool 이 만드는 컨트롤러의 파라미터. 캡슐 프리팹이면 애니메이터가 없어 무시된다.
    private static readonly int AttackParam = Animator.StringToHash("Attack");

    public override void UpdateHealthUI(int currentHealth)
    {
        base.UpdateHealthUI(currentHealth);
        Debug.Log($"Monster {actor_id} health updated to: {currentHealth}");
    }

    public override void UpdateState(GameObject game_object, syncnet.AIState newState)
    {
        base.UpdateState(game_object, newState);
        Debug.Log($"Monster {actor_id} state changed to: {newState}");

        // 서버는 사거리 안에서 교전하는 동안 내내 Attack 상태로 둔다. 그동안 공격 동작을 반복한다.
        if (locomotionAnimator != null && locomotionAnimator.enabled)
            locomotionAnimator.SetBool(AttackParam, newState == syncnet.AIState.Attack);
    }

    protected override void ShowDeathEffect()
    {
        // 몬스터 전용 사망 효과
        // 모델은 자식 SkinnedMeshRenderer 로 그려지므로 자식까지 훑는다.
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                // 빨간색으로 변경하여 몬스터 사망 표시
                if (mat != null && mat.HasProperty("_Color"))
                    mat.color = Color.red;
            }
        }

        // 사망 모션이 없어 그 자리에서 포즈를 멈춘다. 그대로 두면 소멸 전까지 대기 동작을 한다.
        if (locomotionAnimator != null)
            locomotionAnimator.enabled = false;

        // HealthBar 숨기기
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
        }

        Debug.Log($"Monster {actor_id} death effect applied");
    }
}
