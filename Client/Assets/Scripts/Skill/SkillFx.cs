using System.Collections;
using UnityEngine;

// 스킬 연출용 절차적 VFX 헬퍼.
// 아트 에셋 없이 프리미티브/LineRenderer 로 즉석 생성한다.
//
// 지금은 두 가지 용도로만 쓴다.
//   - 번개 줄기(Bolt): 두 점을 잇는 연출이라 파티클보다 LineRenderer 가 맞다
//   - 폴백: 이펙트 라이브러리에 키가 없을 때 대신 그린다(에셋이 빠져도 연출이 사라지지 않게)
// 평소 연출은 전부 파티클이다(Vfx.Play) — 여기 도형을 파티클 위에 겹쳐 그리지 말 것.
//
// 모든 오브젝트는 수명이 끝나면 스스로 Destroy 되므로 호출 측이 정리할 필요가 없다.
public static class SkillFx
{
    // 발광(가산) 머티리얼. 번개처럼 빛나야 하는 선에 쓴다. 셰이더는 이펙트 머티리얼과 같은 것이라 빌드에 포함된다.
    public static Material AdditiveColor(Color c)
    {
        var shader = Shader.Find("Legacy Shaders/Particles/Additive");
        if (shader == null)
            return UnlitColor(c);
        var mat = new Material(shader);
        mat.SetColor("_TintColor", new Color(c.r * 0.5f, c.g * 0.5f, c.b * 0.5f, 0.5f));
        return mat;
    }

    // 반투명 언릿 컬러 머티리얼. LineRenderer/프리미티브 공용.
    public static Material UnlitColor(Color c)
    {
        var mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = c;
        return mat;
    }

    // 콜라이더 없는 발광 구체(투사체/코어/폭발 코어).
    public static GameObject Orb(Vector3 pos, Color color, float size)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * size;
        go.GetComponent<Renderer>().material = UnlitColor(color);
        return go;
    }

    // 지그재그 번개 줄기(체인 라이트닝/라이트닝). SetBolt 로 매 프레임 다시 흔들면 번쩍이는 느낌이 난다.
    public static LineRenderer Bolt(Vector3 from, Vector3 to, Color color, int segments = 12, float width = 0.12f)
    {
        var go = new GameObject("SkillBolt");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = AdditiveColor(color);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.loop = false;
        lr.positionCount = Mathf.Max(2, segments);
        SetBolt(lr, from, to);
        return lr;
    }

    // 양 끝점은 고정하고 중간 정점만 진행 방향의 수직으로 흔든다.
    public static void SetBolt(LineRenderer lr, Vector3 from, Vector3 to, float jitter = 0.5f)
    {
        int n = lr.positionCount;
        Vector3 dir = (to - from).normalized;
        Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;
        for (int i = 0; i < n; i++)
        {
            float k = (float)i / (n - 1);
            Vector3 p = Vector3.Lerp(from, to, k);
            if (i != 0 && i != n - 1)
                p += side * Random.Range(-jitter, jitter) + Vector3.up * Random.Range(-jitter, jitter) * 0.5f;
            lr.SetPosition(i, p);
        }
    }

    // 지면에서 솟는 빛기둥(천상의 주먹 등). 콜라이더 없는 원기둥.
    public static GameObject Column(Vector3 pos, float radius, float height, Color color)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var col = go.GetComponent<Collider>();
        if (col != null) Object.Destroy(col);
        go.transform.position = pos + Vector3.up * (height * 0.5f);
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f); // 실린더 기본 높이 2
        go.GetComponent<Renderer>().material = UnlitColor(color);
        return go;
    }

    // 렌더러 색을 duration 동안 페이드아웃시키고 오브젝트를 정리한다.
    public static IEnumerator FadeOut(GameObject go, float duration, Color color)
    {
        var rend = go != null ? go.GetComponent<Renderer>() : null;
        float t = 0f;
        while (t < duration && go != null)
        {
            t += Time.deltaTime;
            var c = color; c.a = Mathf.Clamp01(1f - t / duration);
            if (rend != null) rend.material.color = c;
            yield return null;
        }
        if (go != null) Object.Destroy(go);
    }

    // 지면 원(반경 ring). 지금은 ExpandRing 전용이다.
    private static LineRenderer Ring(Vector3 center, float radius, Color color, float width = 0.15f, int segments = 48)
    {
        var go = new GameObject("SkillRing");
        var lr = go.AddComponent<LineRenderer>();
        lr.material = UnlitColor(color);
        lr.startColor = lr.endColor = color;
        lr.startWidth = lr.endWidth = width;
        lr.useWorldSpace = true;
        lr.loop = true;
        lr.positionCount = segments;
        SetRing(lr, center, radius);
        return lr;
    }

    // LineRenderer 정점을 center 중심 반경 radius 의 원으로 배치.
    private static void SetRing(LineRenderer lr, Vector3 center, float radius)
    {
        int n = lr.positionCount;
        for (int i = 0; i < n; i++)
        {
            float a = (float)i / n * Mathf.PI * 2f;
            lr.SetPosition(i, center + new Vector3(Mathf.Cos(a) * radius, 0.1f, Mathf.Sin(a) * radius));
        }
    }

    // 확장되는 파문(노바/폭발). duration 동안 0→maxRadius 로 커지며 페이드 아웃 후 소멸.
    public static IEnumerator ExpandRing(Vector3 center, float maxRadius, float duration, Color color)
    {
        var lr = Ring(center, 0.1f, color);
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            SetRing(lr, center, Mathf.Lerp(0.1f, maxRadius, k));
            var c = color; c.a = Mathf.Clamp01(1f - k);
            lr.startColor = lr.endColor = c;
            yield return null;
        }
        if (lr != null) Object.Destroy(lr.gameObject);
    }

    // 폭발 버스트: 코어 구체가 팽창하며 페이드 아웃.
    public static IEnumerator Burst(Vector3 pos, float maxRadius, float duration, Color color)
    {
        var orb = Orb(pos, color, 0.3f);
        var rend = orb.GetComponent<Renderer>();
        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float k = t / duration;
            orb.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, Mathf.Max(0.6f, maxRadius) * 2f, k);
            var c = color; c.a = Mathf.Clamp01(1f - k);
            rend.material.color = c;
            yield return null;
        }
        if (orb != null) Object.Destroy(orb);
    }
}
