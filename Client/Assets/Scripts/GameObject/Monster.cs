using System.Collections.Generic;
using UnityEngine;

public class Monster : Actor
{
    // MonsterAnimationTool 이 만드는 컨트롤러의 파라미터. 캡슐 프리팹이면 애니메이터가 없어 무시된다.
    private static readonly int AttackParam = Animator.StringToHash("Attack");

    // 사망 연출로 칠하기 전의 색. 되살릴 때 돌려놓는다(아래 UpdateState 설명 참고).
    private readonly Dictionary<Material, Color> originalColors = new Dictionary<Material, Color>();

    public override void UpdateHealthUI(int currentHealth)
    {
        base.UpdateHealthUI(currentHealth);
        Debug.Log($"Monster {actor_id} health updated to: {currentHealth}");
    }

    public override void UpdateState(GameObject game_object, syncnet.AIState newState)
    {
        bool alive = newState != syncnet.AIState.Dead && newState != syncnet.AIState.Destroyed;

        // 서버 actor id 는 액터가 사라지면 재사용된다. 죽은 몬스터의 오브젝트를 그대로 물려받으면
        // 사망 연출(애니메이터 끔 + 빨강)이 남아 새 몬스터가 대기 포즈로 미끄러지고 공격도 하지 않는다.
        // 살아 있는 상태가 오면 연출을 되돌린다.
        if (alive)
            Revive();

        base.UpdateState(game_object, newState);
        Debug.Log($"Monster {actor_id} state changed to: {newState}");

        // 서버는 사거리 안에서 교전하는 동안 내내 Attack 상태로 둔다. 그동안 공격 동작을 반복한다.
        if (locomotionAnimator != null && locomotionAnimator.enabled)
            locomotionAnimator.SetBool(AttackParam, newState == syncnet.AIState.Attack);
        else if (alive)
            Debug.LogWarning($"Monster {actor_id}: 애니메이터가 없거나 꺼져 있어 상태 {newState} 를 애니메이션에 반영하지 못했습니다.");
    }

    /// <summary>
    /// 공격 동작에는 걷기 보정 배속을 걸지 않는다. animator.speed 는 컨트롤러 전체에 걸리므로,
    /// 추격 중 올라간 배속(최대 2배)이 그대로 남으면 공격이 그만큼 빨라진다 —
    /// 공격 클립 1.5초는 서버 공격 쿨타임 1.5초에 맞춰져 있어서 등속으로 재생해야 한다.
    /// </summary>
    protected override bool ScalePlaybackWithSpeed
    {
        get { return state != syncnet.AIState.Attack; }
    }

    protected override void ShowDeathEffect()
    {
        // 몬스터 전용 사망 효과
        // 모델은 자식 SkinnedMeshRenderer 로 그려지므로 자식까지 훑는다.
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            foreach (var mat in renderer.materials)
            {
                if (mat == null || !mat.HasProperty("_Color"))
                    continue;

                if (!originalColors.ContainsKey(mat))
                    originalColors[mat] = mat.color;
                // 빨간색으로 변경하여 몬스터 사망 표시
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

    /// <summary>사망 연출을 되돌린다(같은 오브젝트가 새 몬스터에 다시 쓰일 때).</summary>
    private void Revive()
    {
        if (originalColors.Count > 0)
        {
            foreach (var pair in originalColors)
                if (pair.Key != null)
                    pair.Key.color = pair.Value;
            originalColors.Clear();
        }

        if (locomotionAnimator != null && !locomotionAnimator.enabled)
            locomotionAnimator.enabled = true;

        if (healthBar != null && !healthBar.gameObject.activeSelf)
            healthBar.gameObject.SetActive(true);
    }
}
