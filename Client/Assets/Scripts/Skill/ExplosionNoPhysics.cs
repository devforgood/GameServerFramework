using System.Collections;
using UnityEngine;

public class ExplosionNoPhysics : ISkill
{
    public string Name => "ExplosionNoPhysics";

    public float radius = 5f;
    public float explosionForce = 15f;
    public AnimationCurve forceOverDistance; // 거리별 힘 조절용 커브 (선택)
    public float explosionDelay = 0.1f;      // 폭발 연출용 딜레이(선택)
    public float upwardPowerRatio = 0.7f;    // 위로 솟구치는 힘 비율

    private Coroutine explosionCoroutine;

    public void OnSelect(GameInputManager context) { }
    public void OnDeselect(GameInputManager context)
    {
        if (explosionCoroutine != null)
        {
            context.StopCoroutine(explosionCoroutine);
            explosionCoroutine = null;
        }
    }

    public void OnSkillButtonDown(GameInputManager context)
    {
        Vector3? pos = context.GetMouseGroundPosition();
        if (pos.HasValue)
        {
            // 기존 코루틴이 있다면 중지
            if (explosionCoroutine != null)
                context.StopCoroutine(explosionCoroutine);

            explosionCoroutine = context.StartCoroutine(ExplosionRoutine(pos.Value, context));
        }
    }

    public void OnSkillButtonUp(GameInputManager context) { }
    public void Update(GameInputManager context) { }

    private IEnumerator ExplosionRoutine(Vector3 position, GameInputManager context)
    {
        if (explosionDelay > 0f)
            yield return new WaitForSeconds(explosionDelay);

        Collider[] colliders = Physics.OverlapSphere(position, radius);
        foreach (var col in colliders)
        {
            var unit = col.GetComponent<FakePhysicsUnit>();
            if (unit != null)
            {
                Vector3 dir = (col.transform.position - position).normalized;
                float distance = Vector3.Distance(position, col.transform.position);
                float distanceRatio = Mathf.Clamp01(distance / radius);

                float power = explosionForce * (1 - distanceRatio);
                if (forceOverDistance != null && forceOverDistance.keys.Length > 0)
                    power *= forceOverDistance.Evaluate(1 - distanceRatio);

                // 위로 솟구치는 힘을 명확히 추가
                Vector3 force = dir * power;
                force.y = Mathf.Abs(force.y) + power * upwardPowerRatio;

                context.StartCoroutine(JumpUp(unit, force, 1f, 2f));
            }
        }

        explosionCoroutine = null;
    }

    // 유닛을 솟아오르게 하는 코루틴
    private IEnumerator JumpUp(FakePhysicsUnit unit, Vector3 velocity, float duration, float height)
    {
        Vector3 start = unit.transform.position;
        Vector3 end = start + new Vector3(velocity.x, 0, velocity.z);
        float upHeight = velocity.y * 0.1f + height;
        float randomHeight = upHeight * Random.Range(0.9f, 1.1f);
        float randomDuration = duration * Random.Range(0.9f, 1.1f);

        float time = 0;
        while (time < randomDuration)
        {
            float t = time / randomDuration;
            // 포물선 궤적
            float yOffset = 4f * randomHeight * t * (1 - t);
            unit.transform.position = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            // 회전 효과
            unit.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
            time += Time.deltaTime;
            yield return null;
        }
        unit.transform.position = end;
    }
}
