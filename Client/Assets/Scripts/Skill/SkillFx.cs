using System.Collections;
using UnityEngine;

// 스킬 연출용 절차적 VFX 헬퍼.
// 아트 에셋 없이 프리미티브/LineRenderer 로 즉석 생성한다(기존 ExplosionNoPhysics/DrawArc 스타일).
// 모든 오브젝트는 수명이 끝나면 스스로 Destroy 되므로 호출 측이 정리할 필요가 없다.
public static class SkillFx
{
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

    // 지면 원(반경 ring). 반환된 LineRenderer 로 코루틴에서 반경/색을 갱신할 수 있다.
    public static LineRenderer Ring(Vector3 center, float radius, Color color, float width = 0.15f, int segments = 48)
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
    public static void SetRing(LineRenderer lr, Vector3 center, float radius)
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
