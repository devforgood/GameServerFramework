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
//
// 이펙트 본체는 파티클(Vfx.Play, 키는 VfxLibraryTool.Catalog)이다.
// 라이브러리에 키가 없을 때만 예전 절차적 도형(SkillFx)으로 대신한다 — 평소에는 그리지 않는다.
//
// 여기를 타는 것은 플레이어 스킬뿐이다. 몬스터의 시전은 서버가 브로드캐스트하지 않으므로(Monster::Attack →
// SkillSet::TryCast) 몬스터 공격 연출은 Monster 가 AIState.Attack 을 보고 직접 재생한다.
public static class SkillFxDispatcher
{
    // 반환값은 이 연출의 본체 코루틴이다. 캐스터가 낙관적으로 재생한 연출을 서버가 거부했을 때
    // Session 이 StopCoroutine 으로 취소하는 데 쓴다(취소할 게 없는 즉발 연출은 null).
    public static Coroutine Play(Gamedata.Skill data, GameObject caster, Vector3 targetPos, MonoBehaviour host)
    {
        if (data == null || caster == null || host == null)
        {
            Debug.LogWarning($"[SkillFx] Play skipped: data={(data == null ? "null" : data.id.ToString())} caster={(caster == null ? "null" : caster.name)} host={(host == null ? "null" : host.name)}");
            return null;
        }

        string fx = data.fx ?? "";
        Debug.Log($"[SkillFx] Play skill={data.id} fx='{fx}' element='{data.element}' caster={caster.name} target={targetPos}");
        switch (fx)
        {
            case "projectile": return host.StartCoroutine(Projectile(data, caster, targetPos, host));
            case "meteor":     return host.StartCoroutine(Meteor(data, targetPos, host));
            case "nova":       Nova(data, caster, host); return null;
            case "teleport":   Teleport(caster, targetPos, host); return null;
            case "impact":     Explode(data, targetPos, Radius(data, 3f), new Color(1f, 0.9f, 0.3f), host); return null;
            case "heal":       Heal(caster, host); return null;
            case "slash":      Slash(data, caster, targetPos, host); return null;
            case "charge":     return host.StartCoroutine(Charge(data, caster, targetPos, host));
            case "chain":      return host.StartCoroutine(Chain(data, caster, targetPos, host));
            case "holy":       return host.StartCoroutine(Holy(data, targetPos, host));
            case "tornado":    return host.StartCoroutine(Tornado(data, targetPos, host));
            case "cone":       Cone(data, caster, targetPos, host); return null;
            default:
                Debug.Log($"[SkillFx] skill {data.id} 에 fx 가 없어 연출을 생략합니다(fx='{fx}').");
                return null;
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

    // 근접 베기: 캐스터 정면으로 초승달이 지나간다(일반공격/광란/휠윈드 등).
    private static void Slash(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        float radius = data.range > 0 ? data.range : 2f;
        float angle = data.angle > 0 ? data.angle : 90f;
        Vector3 center = caster.transform.position;
        Vector3 dir = target - center; dir.y = 0f;
        float baseDeg = (dir.sqrMagnitude > 0.001f) ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;

        var color = Tint(data, new Color(1f, 1f, 0.85f));

        // 전방위 베기는 회전 베기라 방향 없이 캐스터 중심 파동으로 보여 준다.
        Vector3 facing = Quaternion.Euler(0f, baseDeg, 0f) * Vector3.forward;
        if (angle >= 300f)
        {
            string nova = Key("nova", data);
            if (Vfx.Play(nova, center + Vector3.up * 0.3f, radius, KeyTint(nova, "nova", data, color)) == null)
                host.StartCoroutine(SkillFx.Burst(center, radius * 0.5f, 0.3f, color));
        }
        // 초승달의 중심이 피벗이라 캐스터 위치에 놓고 사거리를 곱하면 호가 판정 범위를 따라간다.
        else if (Vfx.PlayFacing("slash", center + Vector3.up * 0.9f, facing, radius, KeyTint("slash", "slash", data, color)) == null)
            host.StartCoroutine(SkillFx.Burst(center + facing * radius * 0.5f, radius * 0.4f, 0.25f, color));
    }

    // radius(우선) → range → 폴백. 클라 연출 크기용.
    private static float Radius(Gamedata.Skill data, float fallback)
    {
        if (data.radius > 0) return data.radius;
        if (data.range > 0) return data.range;
        return fallback;
    }

    // ── 에셋 이펙트 헬퍼 ──

    // "용도.속성" 키가 있으면 그것, 없으면 "용도". 둘 다 없으면 null.
    private static string Key(string use, Gamedata.Skill data)
    {
        if (!string.IsNullOrEmpty(data.element) && Vfx.Has(use + "." + data.element))
            return use + "." + data.element;
        return Vfx.Has(use) ? use : null;
    }

    // 속성 키가 따로 있으면 그 이펙트의 원색을 쓰고, 공용 키로 떨어졌을 때만 속성 색을 입힌다.
    private static Color? KeyTint(string key, string use, Gamedata.Skill data, Color fallback)
    {
        if (key == null || key != use || string.IsNullOrEmpty(data.element) || data.element == "physical")
            return null;
        return Tint(data, fallback);
    }

    // 착탄: 화염(또는 속성 없음)은 반경만 한 폭발. 그 외 속성은 반경만 한 파동(속성 색) + 가운데 속성 타격.
    // 타격 이펙트를 반경만큼 키우면 불꽃 튐까지 커져 어색해서 파동으로 범위를 보여 준다.
    private static void Explode(Gamedata.Skill data, Vector3 pos, float radius, Color fallback, MonoBehaviour host)
    {
        bool fire = data.element == "fire" || string.IsNullOrEmpty(data.element);
        if (fire && Vfx.Has("explosion"))
        {
            Vfx.Play("explosion", pos, radius);
            return;
        }
        if (!fire && Vfx.Has("nova"))
        {
            Vfx.Play("nova", pos, radius, Tint(data, fallback));
            Vfx.Play(Key("hit", data), pos + Vector3.up * 0.8f, 1.3f);
            return;
        }
        host.StartCoroutine(SkillFx.Burst(pos, radius, 0.4f, Tint(data, fallback)));
    }

    private static void Heal(GameObject caster, MonoBehaviour host)
    {
        var green = new Color(0.4f, 1f, 0.5f);
        if (Vfx.Play("heal", caster.transform.position + Vector3.up * 0.2f, Quaternion.identity, 1f, null, caster.transform) == null)
            host.StartCoroutine(SkillFx.Burst(caster.transform.position, 1.6f, 0.5f, green));
    }

    private static Vector3 Flat(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude > 0.0001f ? v : Vector3.forward;
    }

    // 투사체: 캐스터에서 목표까지 발광 구체가 날아가 착탄 지점에서 폭발(파이어볼/파이어볼트/라이트닝 퓨리).
    private static IEnumerator Projectile(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = Tint(data, new Color(1f, 0.5f, 0.1f));
        Vector3 start = caster.transform.position + Vector3.up * 1f;
        target.y = start.y;

        // 몸체 이펙트를 빈 오브젝트에 붙여 날린다. 없으면 발광 구체.
        GameObject orb;
        string body = Key("projectile", data);
        if (body != null)
        {
            orb = new GameObject("Projectile");
            orb.transform.SetPositionAndRotation(start, Quaternion.LookRotation(Flat(target - start)));
            Vfx.Play(body, start, orb.transform.rotation, 1f, KeyTint(body, "projectile", data, color), orb.transform, looping: true);
            // 서버 거부로 이 코루틴이 중간에 멈추면 아래 Destroy 에 닿지 못한다. 반복 이펙트가 영영 남지 않게 한다.
            Object.Destroy(orb, 5f);
        }
        else
            orb = SkillFx.Orb(start, color, 0.4f);

        const float speed = 20f;
        while (orb != null && Vector3.Distance(orb.transform.position, target) > 0.3f)
        {
            orb.transform.position = Vector3.MoveTowards(orb.transform.position, target, speed * Time.deltaTime);
            yield return null;
        }
        if (orb != null) Object.Destroy(orb);
        Explode(data, target, Radius(data, 4f), new Color(1f, 0.5f, 0.1f), host);
    }

    // 낙하 광역: 목표 지점 텔레그래프 링 → 하늘에서 코어 낙하 → 착탄 폭발 + 파문(메테오/블리자드/프로즌 오브).
    private static IEnumerator Meteor(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = Tint(data, new Color(1f, 0.35f, 0.05f));
        Vfx.Play("circle", target, radius, color);

        yield return new WaitForSeconds(0.5f); // 낙하 예고

        GameObject core;
        string body = Key("projectile", data);
        if (body != null)
        {
            core = new GameObject("MeteorCore");
            core.transform.SetPositionAndRotation(target + Vector3.up * 20f, Quaternion.LookRotation(Vector3.down));
            Vfx.Play(body, core.transform.position, core.transform.rotation, 2.5f, KeyTint(body, "projectile", data, color), core.transform, looping: true);
            Object.Destroy(core, 5f);   // 코루틴이 취소돼도 남지 않게(투사체와 같은 이유)
        }
        else
            core = SkillFx.Orb(target + Vector3.up * 20f, color, 1.2f);
        float t = 0f; const float fall = 0.4f;
        Vector3 s = core.transform.position;
        while (t < fall)
        {
            t += Time.deltaTime;
            if (core != null) core.transform.position = Vector3.Lerp(s, target, t / fall);
            yield return null;
        }
        if (core != null) Object.Destroy(core);

        Explode(data, target, radius, new Color(1f, 0.35f, 0.05f), host);
    }

    // 캐스터 중심 확장 파문(노바/스태틱 필드/포이즌 노바/전투 함성).
    private static void Nova(Gamedata.Skill data, GameObject caster, MonoBehaviour host)
    {
        float radius = Radius(data, 6f);
        var color = Tint(data, new Color(0.4f, 0.8f, 1f));
        Vector3 center = caster.transform.position;
        string key = Key("nova", data);
        if (key != null)
            Vfx.Play(key, center + Vector3.up * 0.3f, radius, KeyTint(key, "nova", data, color));
        else
            host.StartCoroutine(SkillFx.Burst(center, radius * 0.5f, 0.3f, color));
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
                // 흙먼지는 자주 뿌리면 겹쳐 뭉개져서 링보다 간격을 넓힌다.
                if (Vfx.Has("dash"))
                {
                    nextTrail += 0.12f;
                    Vfx.PlayFacing("dash", caster.transform.position, -dir);
                }
                else
                {
                    nextTrail += 0.06f;
                    host.StartCoroutine(SkillFx.ExpandRing(caster.transform.position, 1.2f, 0.3f, color));
                }
            }
            yield return null;
        }

        Explode(data, dest, Radius(data, 3f), new Color(1f, 0.85f, 0.5f), host);
    }

    // 번개 줄기: 캐스터에서 목표까지 지그재그 줄기를 매 프레임 흔들어 번쩍이게 한다(체인 라이트닝/라이트닝).
    private static IEnumerator Chain(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = Tint(data, new Color(0.75f, 0.9f, 1f));
        Vector3 from = caster.transform.position + Vector3.up * 1f;
        Vector3 to = new Vector3(target.x, from.y, target.z);

        var lr = SkillFx.Bolt(from, to, color);
        Vfx.Play(Key("hit", data), from, 0.6f);
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
        if (Vfx.Play("strike", new Vector3(to.x, to.y - 1f, to.z)) == null)
            host.StartCoroutine(SkillFx.Burst(to, 1.5f, 0.25f, color));
        Vfx.Play(Key("hit", data), to);
    }

    // 심판의 빛기둥: 목표 지점 예고 링 → 하늘에서 내리꽂히는 빛기둥 → 폭발 + 파문(천상의 주먹).
    private static IEnumerator Holy(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 4f);
        var color = Tint(data, new Color(1f, 0.93f, 0.55f));
        Vfx.Play("circle", target, radius, color);

        yield return new WaitForSeconds(0.25f); // 강림 예고

        if (Vfx.Play("strike.holy", target, radius * 0.4f) != null)
            yield return new WaitForSeconds(0.2f);
        else
        {
            var column = SkillFx.Column(target, radius * 0.35f, 14f, color);
            yield return host.StartCoroutine(SkillFx.FadeOut(column, 0.45f, color));
        }
        Explode(data, target, radius, new Color(1f, 0.93f, 0.55f), host);
    }

    // 회오리: 목표 지점 반경 안을 돌며 흙먼지가 위로 감겨 올라간다(토네이도).
    private static IEnumerator Tornado(Gamedata.Skill data, Vector3 target, MonoBehaviour host)
    {
        float radius = Radius(data, 4f);
        var color = Tint(data, new Color(0.85f, 0.9f, 0.95f));

        if (!Vfx.Has("tornado"))
        {
            host.StartCoroutine(SkillFx.Burst(target, radius, 0.3f, color));
            yield break;
        }

        const float dur = 0.9f;
        float t = 0f, nextDust = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            if (t >= nextDust)
            {
                nextDust += 0.15f;
                float a = (t / dur) * Mathf.PI * 4f;
                Vfx.Play("tornado", target + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius * 0.4f,
                         Quaternion.Euler(0f, a * Mathf.Rad2Deg, 0f), radius * 0.5f);
            }
            yield return null;
        }
    }

    // 원뿔 분사: 캐스터 정면으로 불꽃을 뿜는다(인페르노).
    private static void Cone(Gamedata.Skill data, GameObject caster, Vector3 target, MonoBehaviour host)
    {
        float range = data.range > 0 ? data.range : 4f;
        var color = Tint(data, new Color(1f, 0.5f, 0.1f));

        Vector3 center = caster.transform.position;
        Vector3 dir = target - center; dir.y = 0f;
        float baseDeg = (dir.sqrMagnitude > 0.001f) ? Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg : 0f;
        Vector3 facing = Quaternion.Euler(0f, baseDeg, 0f) * Vector3.forward;

        string flame = Key("cone", data);
        // 길이 2 m 로 만든 분사라 사거리의 절반을 곱한다.
        if (flame == null || Vfx.PlayFacing(flame, center + Vector3.up * 0.8f, facing, range * 0.5f, KeyTint(flame, "cone", data, color)) == null)
            host.StartCoroutine(SkillFx.Burst(center + facing * range * 0.7f + Vector3.up * 0.5f, 1.2f, 0.25f, color));
    }

    // 순간이동: 출발/도착 지점 섬광만 재생한다.
    // 위치 이동은 절대 클라가 낙관적으로 하지 않는다(서버 권위) — 서버가 쿨다운 등으로 거부하면
    // 에이전트가 안 움직이는데 클라만 옮겨두면, 위치 동기화(actor.pos)가 원위치로 스냅백시킨다.
    // 실제 이동은 서버 텔레포트 후 UpdateActorNotify → Session 의 위치 보간/스냅으로 반영된다.
    private static void Teleport(GameObject caster, Vector3 target, MonoBehaviour host)
    {
        var color = new Color(0.7f, 0.4f, 1f);
        Vector3 from = caster.transform.position;
        Vector3 to = new Vector3(target.x, from.y, target.z);
        if (Vfx.Play("teleport.out", from) == null)
            host.StartCoroutine(SkillFx.Burst(from, 1.5f, 0.25f, color)); // 출발 지점 섬광
        if (Vfx.Play("teleport.in", to) == null)
            host.StartCoroutine(SkillFx.Burst(to, 1.5f, 0.25f, color));   // 도착 지점 섬광
    }
}
