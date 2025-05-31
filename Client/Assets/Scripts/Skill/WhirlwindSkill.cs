
using System.Collections;
using UnityEngine;

public class WhirlwindSkill : BaseSkill
{

    public float whirlwindSpeed = 5f;
    public float whirlwindRotateSpeed = 720f;
    public string Name => "Whirlwind";
    private Coroutine whirlwindCoroutine;
    private Vector3 target;

    public void OnSelect(GameInputManager context) { }
    public void OnDeselect(GameInputManager context)
    {
        if (whirlwindCoroutine != null)
        {
            context.StopCoroutine(whirlwindCoroutine);
            context.IsSkillActive = false;
            whirlwindCoroutine = null;
        }
    }

    public void OnSkillButtonDown(GameInputManager context)
    {
        Vector3? dest = context.GetMouseGroundPosition();
        if (dest.HasValue && !context.IsSkillActive)
        {
            target = dest.Value;
            whirlwindCoroutine = context.StartCoroutine(WhirlwindMove(context));
            context.IsSkillActive = true;
            context.targetPosition = null;
        }
    }

    public void OnSkillButtonUp(GameInputManager context)
    {
        if (whirlwindCoroutine != null)
        {
            context.StopCoroutine(whirlwindCoroutine);
            context.IsSkillActive = false;
            whirlwindCoroutine = null;
        }
    }

    public void Update(GameInputManager context)
    {
        if (context.IsSkillActive && Input.GetMouseButton(1))
        {
            Vector3? dest = context.GetMouseGroundPosition();
            if (dest.HasValue)
                target = dest.Value;
        }
    }

    private IEnumerator WhirlwindMove(GameInputManager context)
    {
        while (Input.GetMouseButton(1))
        {
            Vector3 pos = context.PlayerPosition;
            target.y = pos.y;
            context.PlayerPosition = Vector3.MoveTowards(pos, target, whirlwindSpeed * Time.deltaTime);
            context.PlayerTransform.Rotate(Vector3.up, whirlwindRotateSpeed * Time.deltaTime, Space.Self);
            yield return null;
        }
        context.IsSkillActive = false;
    }
}
