using UnityEngine;

// 팔라딘 오라 등 패시브 스킬의 클라이언트 표현.
// 패시브는 유저 액션 없이 "보유만으로 지속 적용"되므로, 서버는 UseSkill 브로드캐스트를 하지 않는다.
// 따라서 지면 오라 링은 UseSkill 연출(SkillFxDispatcher)이 아니라 이 클래스가 직접 붙였다 뗀다.
//
// 지금은 데모로 "스킬 선택 시 켜고 해제 시 끈다"(GameInputManager 선택 기반)로 토글한다.
// TODO: 서버가 캐릭터의 활성 패시브 목록을 동기화하면, 선택이 아니라 보유 여부로 상시 표시하도록 바꾼다.
public class AuraSkill : Skill
{
    private GameObject ringObj;

    public override void OnSelect(GameInputManager context)
    {
        if (context != null && context.player != null)
            StartAura(context.player);
    }

    public override void OnDeselect(GameInputManager context)
    {
        StopAura();
    }

    // 대상 캐릭터에 오라 지면 링을 붙인다(중복 방지). heal 오라면 초록, damage 오라면 주황.
    public void StartAura(GameObject owner)
    {
        if (ringObj != null || owner == null)
            return;

        ringObj = new GameObject("AuraRing");
        ringObj.transform.SetParent(owner.transform, false);

        var ring = ringObj.AddComponent<AuraRing>();
        ring.radius = (gamedata != null && gamedata.radius > 0) ? gamedata.radius : 5f;
        ring.color = (gamedata != null && gamedata.heal > 0)
            ? new Color(0.3f, 1f, 0.4f, 0.8f)   // 기도(회복) 오라
            : new Color(1f, 0.55f, 0.1f, 0.8f); // 성화(화염) 오라
        ring.Rebuild();
    }

    public void StopAura()
    {
        if (ringObj != null)
        {
            Object.Destroy(ringObj);
            ringObj = null;
        }
    }
}
