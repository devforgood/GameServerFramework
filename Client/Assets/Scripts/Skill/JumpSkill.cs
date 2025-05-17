using System.Collections;
using UnityEngine;

public class JumpSkill : ISkill
{
    public float jumpDuration = 1f;
    public float jumpHeight = 3f;
    public string Name => "Jump";
    private Coroutine jumpCoroutine;

    public void OnSelect(GameInputManager context) { }
    public void OnDeselect(GameInputManager context) { }

    public void OnSkillButtonDown(GameInputManager context)
    {
        // 점프는 버튼 Down에서만 발동
        Vector3? dest = context.GetMouseGroundPosition();
        if (dest.HasValue && !context.IsSkillActive)
        {
            jumpCoroutine = context.StartCoroutine(JumpToPosition(context, context.PlayerPosition, dest.Value, jumpDuration, jumpHeight));
            context.IsSkillActive = true;
            context.targetPosition = null;
        }
    }

    public void OnSkillButtonUp(GameInputManager context) { }

    public void Update(GameInputManager context) { }

    private IEnumerator JumpToPosition(GameInputManager context, Vector3 start, Vector3 end, float duration, float height)
    {
        float time = 0;
        float dropPoint = 0.7f; // 상승 구간 비율 (0~1)
        float fallDuration = duration * (1f - dropPoint); // 하강 구간 시간
        Vector3 lastPos = start;

        while (time < duration)
        {
            float t = time / duration;
            float yOffset;

            if (t < dropPoint)
            {
                // 천천히 상승 (곡선 조정 가능)
                yOffset = Mathf.Lerp(0, height, t / dropPoint);
            }
            else
            {
                // 완만하게 하강 (선형 하강)
                float fallT = (t - dropPoint) / (1f - dropPoint); // 0~1
                yOffset = Mathf.Lerp(height, 0, fallT);
            }

            Vector3 pos = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            context.PlayerPosition = pos;
            lastPos = pos;
            time += Time.deltaTime;
            yield return null;
        }
        // 마지막 위치는 착지점
        context.PlayerPosition = end;
        context.IsSkillActive = false;
    }
}
