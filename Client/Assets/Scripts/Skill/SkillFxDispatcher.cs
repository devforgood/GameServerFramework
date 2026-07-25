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
        {
            Debug.LogWarning($"[SkillFx] Play skipped: data={(data == null ? "null" : data.id.ToString())} caster={(caster == null ? "null" : caster.name)} host={(host == null ? "null" : host.name)}");
            return;
        }

        string fx = data.fx ?? "";
        Debug.Log($"[SkillFx] Play skill={data.id} fx='{fx}' element='{data.element}' caster={caster.name} target={targetPos}");
        switch (fx)
        {
            case "projectile": host.StartCoroutine(Projectile(data, caster, targetPos, host)); break;
            case "meteor":     host.StartCoroutine(Meteor(data, targetPos, host)); break;
            case "nova":       Nova(data, caster, host); break;
            case "teleport":   Teleport(caster, targetPos, host); break;
            case "impact":     host.StartCoroutine(SkillFx.Burst(targetPos, Radius(data, 3f), 0.4f, Tint(data, new Color(1f, 0.9f, 0.3f)))); break;
            case "heal":       host.StartCoroutine(SkillFx.Burst(caster.transform.position, 1.6f, 0.5f, new Color(0.3f, 1f, 0.4f))); break;
            case "slash":      host.StartCoroutine(Slash(data, caster, targetPos, host)); break;
            case "charge":     host.StartCoroutine(Charge(data, caster, targetPos, host)); break;
            case "chain":      host.StartCoroutine(Chain(data, caster, targetPos, host)); break;
            case "holy":       host.StartCoroutine(Holy(data, targetPos, host)); break;
            case "tornado":    host.StartCoroutine(Tornado(data, targetPos, host)); break;
            case "cone":       host.StartCoroutine(Cone(data, caster, targetPos, host)); break;
            default:
                Debug.Log($"[SkillFx] skill {data.id} 에 fx 가 없어 연출을 생략합니다(fx='{fx}').");
                break;
        }
    }

    // 속성(element) 색. 같은 fx 라도 화염/냉기/번개/독/신성이 다른 색으로 보이게 한다.
    // element 는 fx 와 마찬가지로 클라 전용 프레젠테이션 데이터라 서버는 무시한다.
    // 값이 없거나 모르는 속성이면 그 연출의 기본색(fallback)을 그대로 쓴다.
    private static Color Tint(Gamedata.Skill data, Color fallback)
    {
        switch (data.element ?? "")
        {
            case "fire":      return new Color(1f, 0.45f, 0.08f);
            case "cold":      return new Color(0.45f, 0.8f, 1f);
            case "lightning": return new Color(0.75f, 0.9f, 1f);
            case "poison":    return new Color(0.45f, 0.95f, 0.35f);
            case "holy":      return new Color(1f, 0.93f, 0.55f);
            case "physical":  return new Color(1f, 1f, 0.9f);
            default:          return fallback;
        }
    }

    // 근접 베기: 캐스터가 목표를 향한 부채꼴(range/angle) 호를 순간 그렸다 페이드(일반공격/광란/휠윈드 등).
    private static IEnumerator Slash(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        float radius = data.range > 0 ? data.range : 2f;
        float angle = data.angle > 0 ? data.angle : 90f;
        Vector3 center = caster.transform.position;
        Vector3 dir = target - center; dir.y = 0f;
        float baseDeg = (dir.sqrMagnitude > 0.001f) ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;

        var color = Tint(data, new Color(1f, 1f, 0.85f));
        var lr = SkillFx.Arc(center, radius, baseDeg - angle / 2f, baseDeg + angle / 2f, color);

        float t = 0f; const float dur = 0.25f;
        while (t < dur)
        {
            t += Time.deltaTime;
            var c = color; c.a = Mathf.Clamp01(1f - t / dur);
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (lr != null) Object.Destroy(lr.gameObject);
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
        var color = Tint(data, new Color(1f, 0.5f, 0.1f));
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
        host.StartCoroutine(SkillFx.Burst(target, Radius(data, 4f), 0.35f, color));
    }

    // 낙하 광역: 목표 지점 텔레그래프 링 → 하늘에서 코어 낙하 → 착탄 폭발 + 파문(메테오/블리자드/프로즌 오브).
    private static IEnumerator Meteor(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = Tint(data, new Color(1f, 0.35f, 0.05f));
        var ring = SkillFx.Ring(target, radius, color);

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
        host.StartCoroutine(SkillFx.ExpandRing(target, radius, 0.5f, color));
    }

    // 캐스터 중심 확장 파문(노바/스태틱 필드/포이즌 노바/전투 함성).
    private static void Nova(Gamedata.Skill data, GameObject caster, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = Tint(data, new Color(0.4f, 0.8f, 1f));
        host.StartCoroutine(SkillFx.ExpandRing(caster.transform.position, radius, 0.4f, color));
        host.StartCoroutine(SkillFx.Burst(caster.transform.position, radius * 0.5f, 0.3f, color));
    }

    // 돌진(차지/볼트): 캐스터가 지나온 자리에 잔상 링을 남기고 도착 지점에서 충격파를 터뜨린다.
    // 이동 자체는 서버 dash 효과가 매 틱 진행하며 위치 동기화로 반영되므로, 연출은 캐스터의
    // 실제(서버) 위치를 따라다니기만 한다 — 클라가 낙관적으로 옮기지 않는다(teleport 와 같은 이유).
    private static IEnumerator Charge(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = Tint(data, new Color(1f, 0.85f, 0.5f));
        Vector3 start = caster.transform.position;
        Vector3 dest = new Vector3(target.x, start.y, target.z);

        // 서버 dash 와 같은 규칙으로 도착 지점을 range 안으로 자른다(연출 위치를 서버와 맞춘다).
        Vector3 dir = dest - start;
        float maxDistance = data.range > 0 ? data.range : 10f;
        if (dir.magnitude > maxDistance)
            dest = start + dir.normalized * maxDistance;

        float duration = data.duration > 0 ? data.duration : 1f;
        float t = 0f, nextTrail = 0f;
        while (t < duration && caster != null)
        {
            t += Time.deltaTime;
            if (t >= nextTrail)
            {
                nextTrail += 0.06f;
                host.StartCoroutine(SkillFx.ExpandRing(caster.transform.position, 1.2f, 0.3f, color));
            }
            yield return null;
        }

        float radius = Radius(data, 3f);
        host.StartCoroutine(SkillFx.Burst(dest, radius, 0.4f, color));
        host.StartCoroutine(SkillFx.ExpandRing(dest, radius, 0.45f, color));
    }

    // 번개 줄기: 캐스터에서 목표까지 지그재그 줄기를 매 프레임 흔들어 번쩍이게 한다(체인 라이트닝/라이트닝).
    private static IEnumerator Chain(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = Tint(data, new Color(0.75f, 0.9f, 1f));
        Vector3 from = caster.transform.position + Vector3.up * 1f;
        Vector3 to = new Vector3(target.x, from.y, target.z);

        var lr = SkillFx.Bolt(from, to, color);
        float t = 0f; const float dur = 0.3f;
        while (t < dur)
        {
            t += Time.deltaTime;
            SkillFx.SetBolt(lr, from, to);
            var c = color; c.a = Mathf.Clamp01(1f - t / dur);
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (lr != null) Object.Destroy(lr.gameObject);
        host.StartCoroutine(SkillFx.Burst(to, 1.5f, 0.25f, color));
    }

    // 심판의 빛기둥: 목표 지점 예고 링 → 하늘에서 내리꽂히는 빛기둥 → 폭발 + 파문(천상의 주먹).
    private static IEnumerator Holy(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 4f);
        var color = Tint(data, new Color(1f, 0.93f, 0.55f));
        var ring = SkillFx.Ring(target, radius, color);

        yield return new WaitForSeconds(0.25f); // 강림 예고

        var column = SkillFx.Column(target, radius * 0.35f, 14f, color);
        yield return host.StartCoroutine(SkillFx.FadeOut(column, 0.45f, color));
        if (ring != null) Object.Destroy(ring.gameObject);

        host.StartCoroutine(SkillFx.Burst(target, radius, 0.35f, color));
        host.StartCoroutine(SkillFx.ExpandRing(target, radius, 0.4f, color));
    }

    // 회오리: 목표 지점에서 반경이 맥동하며 위로 감아 올라가는 링 다발(토네이도).
    private static IEnumerator Tornado(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 4f);
        var color = Tint(data, new Color(0.85f, 0.9f, 0.95f));

        const int ringCount = 4;
        const float dur = 0.9f;
        var rings = new LineRenderer[ringCount];
        for (int i = 0; i < ringCount; i++)
            rings[i] = SkillFx.Ring(target, radius * 0.3f, color);

        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            for (int i = 0; i < ringCount; i++)
            {
                float height = ((k * 2f + i * 0.25f) % 1f) * 5f;                       // 위로 흘러 올라간다
                float r = radius * (0.25f + 0.18f * i) * (0.7f + 0.3f * Mathf.Sin((k * 6f + i) * Mathf.PI));
                SkillFx.SetRing(rings[i], target + Vector3.up * height, r);
                var c = color; c.a = Mathf.Clamp01(1f - k);
                rings[i].startColor = rings[i].endColor = c;
            }
            yield return null;
        }

        foreach (var lr in rings)
            if (lr != null) Object.Destroy(lr.gameObject);

        host.StartCoroutine(SkillFx.Burst(target, radius, 0.3f, color));
    }

    // 원뿔 분사: 캐스터 정면 부채꼴(range/angle)이 뻗어 나가는 화염 방사(인페르노).
    private static IEnumerator Cone(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        float range = data.range > 0 ? data.range : 4f;
        float angle = data.angle > 0 ? data.angle : 45f;
        var color = Tint(data, new Color(1f, 0.5f, 0.1f));

        Vector3 center = caster.transform.position;
        Vector3 dir = target - center; dir.y = 0f;
        float baseDeg = (dir.sqrMagnitude > 0.001f) ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;

        var lr = SkillFx.Arc(center, 0.2f, baseDeg - angle / 2f, baseDeg + angle / 2f, color);
        float t = 0f; const float dur = 0.3f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = t / dur;
            SkillFx.SetArc(lr, center, Mathf.Lerp(0.2f, range, k), baseDeg - angle / 2f, baseDeg + angle / 2f);
            var c = color; c.a = Mathf.Clamp01(1f - k);
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (lr != null) Object.Destroy(lr.gameObject);

        // 분사 끝단의 불꽃 덩어리.
        float rad = baseDeg * Mathf.Deg2Rad;
        host.StartCoroutine(SkillFx.Burst(center + new Vector3(Mathf.Sin(rad), 0.5f, Mathf.Cos(rad)) * range * 0.7f, 1.2f, 0.25f, color));
    }

    // 순간이동: 출발/도착 지점 섬광만 재생한다.
    // 위치 이동은 절대 클라가 낙관적으로 하지 않는다(서버 권위) — 서버가 쿨다운 등으로 거부하면
    // 에이전트가 안 움직이는데 클라만 옮겨두면, 위치 동기화(actor.pos)가 원위치로 스냅백시킨다.
    // 실제 이동은 서버 텔레포트 후 UpdateActorNotify → Session 의 위치 보간/스냅으로 반영된다.
    private static void Teleport(GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = new Color(0.7f, 0.4f, 1f);
        Vector3 from = caster.transform.position;
        host.StartCoroutine(SkillFx.Burst(from, 1.5f, 0.25f, color));                                  // 출발 지점 섬광
        host.StartCoroutine(SkillFx.Burst(new Vector3(target.x, from.y, target.z), 1.5f, 0.25f, color)); // 도착 지점 섬광
    }
}
