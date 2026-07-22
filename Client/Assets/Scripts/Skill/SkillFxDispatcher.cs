using System.Collections;
using UnityEngine;

// 스킬 프레젠테이션(이펙트) 디스패처 — 클라이언트 전용.
//
// 서버는 시전 성공 시 UseSkill 을 브로드캐스트하고(캐스터 제외), 클라는 그걸 근거로 연출만 재생한다.
// 어떤 연출을 재생할지는 skill 데이터의 `fx` 문자열로 결정한다(예: "projectile", "meteor").
// fx 는 클라 전용 필드라 서버 로직/클래스에 전혀 영향이 없다(code_name 과 달리 서버 파생 클래스가 생기지 않는다).
//
// 호출 지점:
//   - 캐스터 로컬 재생: Session.UseSkill (브로드캐스트에서 캐스터가 제외되므로 직접 재생)
//   - 원격 관전자 재생: Session.HandleUseSkillNotify
// 두 경로가 같은 함수를 써서 자신과 남에게 동일한 연출이 보인다.
public static class SkillFxDispatcher
{
    public static void Play(Gamedata.Skill data, GameObject caster, Vector3 targetPos, MonoBehaviour host)
    {
        if (data == null || caster == null || host == null)
            return;

        string fx = data.fx ?? "";
        switch (fx)
        {
            case "projectile": host.StartCoroutine(Projectile(data, caster, targetPos, host)); break;
            case "meteor":     host.StartCoroutine(Meteor(data, targetPos, host)); break;
            case "nova":       Nova(data, caster, host); break;
            case "teleport":   Teleport(caster, targetPos, host); break;
            case "impact":     host.StartCoroutine(SkillFx.Burst(targetPos, Radius(data, 3f), 0.4f, new Color(1f, 0.9f, 0.3f))); break;
            default: break; // fx 미지정 스킬(근접 등)은 연출 없음
        }
    }

    // radius(우선) → range → 폴백. 클라 연출 크기용.
    private static float Radius(Gamedata.Skill data, float fallback)
    {
        if (data.radius > 0) return data.radius;
        if (data.range > 0) return data.range;
        return fallback;
    }

    // 투사체: 캐스터에서 목표까지 발광 구체가 날아가 착탄 지점에서 폭발(파이어볼/파이어볼트/라이트닝 퓨리).
    private static IEnumerator Projectile(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = new Color(1f, 0.5f, 0.1f);
        Vector3 start = caster.transform.position + Vector3.up * 1f;
        target.y = start.y;
        var orb = SkillFx.Orb(start, color, 0.4f);

        const float speed = 20f;
        while (orb != null && Vector3.Distance(orb.transform.position, target) > 0.3f)
        {
            orb.transform.position = Vector3.MoveTowards(orb.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        if (orb != null) Object.Destroy(orb);
        host.StartCoroutine(SkillFx.Burst(target, Radius(data, 4f), 0.35f, new Color(1f, 0.4f, 0.05f)));
    }

    // 낙하 광역: 목표 지점 텔레그래프 링 → 하늘에서 코어 낙하 → 착탄 폭발 + 파문(메테오/블리자드/프로즌 오브).
    private static IEnumerator Meteor(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = new Color(1f, 0.35f, 0.05f);
        var ring = SkillFx.Ring(target, radius, new Color(1f, 0.3f, 0f));

        yield return new WaitForSeconds(0.5f); // 낙하 예고

        var core = SkillFx.Orb(target + Vector3.up * 20f, color, 1.2f);
        float t = 0f; const float fall = 0.4f;
        Vector3 s = core.transform.position;
        while (t < fall)
        {
            t += Time.deltaTime;
            if (core != null) core.transform.position = Vector3.Lerp(s, target, t / fall);
            yield return null;
        }
        if (core != null) Object.Destroy(core);
        if (ring != null) Object.Destroy(ring.gameObject);

        host.StartCoroutine(SkillFx.Burst(target, radius, 0.4f, color));
        host.StartCoroutine(SkillFx.ExpandRing(target, radius, 0.5f, new Color(1f, 0.6f, 0.1f)));
    }

    // 캐스터 중심 확장 파문(노바/스태틱 필드/포이즌 노바).
    private static void Nova(Gamedata.Skill data, GameObject caster, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = new Color(0.4f, 0.8f, 1f);
        host.StartCoroutine(SkillFx.ExpandRing(caster.transform.position, radius, 0.4f, color));
        host.StartCoroutine(SkillFx.Burst(caster.transform.position, radius * 0.5f, 0.3f, new Color(0.5f, 0.85f, 1f)));
    }

    // 순간이동: 출발/도착 지점 섬광 + 즉시 위치 이동(서버도 텔레포트하므로 위치는 곧 동기화된다).
    private static void Teleport(GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = new Color(0.7f, 0.4f, 1f);
        Vector3 from = caster.transform.position;
        host.StartCoroutine(SkillFx.Burst(from, 1.5f, 0.25f, color));
        caster.transform.position = new Vector3(target.x, from.y, target.z);
        host.StartCoroutine(SkillFx.Burst(caster.transform.position, 1.5f, 0.25f, color));
    }
}
