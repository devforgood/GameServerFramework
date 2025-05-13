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
        }
    }

    public void OnSkillButtonUp(GameInputManager context) { }

    public void Update(GameInputManager context) { }

    private IEnumerator JumpToPosition(GameInputManager context, Vector3 start, Vector3 end, float duration, float height)
    {
        float time = 0;
        while (time < duration)
        {
            float t = time / duration;
            float yOffset = Mathf.Sin(Mathf.PI * t) * height;
            context.PlayerPosition = Vector3.Lerp(start, end, t) + Vector3.up * yOffset;
            time += Time.deltaTime;
            yield return null;
        }
        context.PlayerPosition = end;
        context.IsSkillActive = false;
    }
}
