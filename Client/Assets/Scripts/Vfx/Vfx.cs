using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 에셋 팩 파티클 이펙트를 한 번 재생하고 스스로 치우는 진입점.
///
/// 팩 프리팹은 데모용이라 대부분 반복(loop) 재생으로 만들어져 있다. 여기서 인스턴스마다
///   - 반복을 끄고(한 주기만), 필요하면 emitTime 뒤 방출을 멈춘다
///   - 크기 조절이 자식까지 먹도록 scalingMode 를 Hierarchy 로 바꾼다
///   - 마지막 파티클이 사라질 시간에 오브젝트를 파괴한다
/// 그래서 호출 측은 위치만 넘기면 되고 정리할 필요가 없다. 프리팹 원본은 건드리지 않는다.
///
/// 키가 없거나 라이브러리가 없으면 null 을 돌려준다. 호출 측은 그때 절차적 연출(SkillFx)로 대신한다.
/// </summary>
public static class Vfx
{
    private static Dictionary<string, VfxLibrary.Entry> entries;

    private static Dictionary<string, VfxLibrary.Entry> Entries
    {
        get
        {
            if (entries == null)
            {
                entries = new Dictionary<string, VfxLibrary.Entry>();
                var library = Resources.Load<VfxLibrary>(VfxLibrary.ResourcePath);
                if (library == null)
                    Debug.LogWarning($"[Vfx] Resources/{VfxLibrary.ResourcePath} 가 없다. Tools > VFX > Build Library 를 실행하라.");
                else
                    foreach (var e in library.entries)
                        if (e != null && e.prefab != null && !string.IsNullOrEmpty(e.key))
                            entries[e.key] = e;
            }
            return entries;
        }
    }

    public static bool Has(string key) => key != null && Entries.ContainsKey(key);

    /// <summary>
    /// key 이펙트를 pos 에 한 번 재생한다.
    /// </summary>
    /// <param name="rotation">방향이 있는 이펙트(베기·분사)의 정면. 빌보드 이펙트는 무시된다.</param>
    /// <param name="scale">라이브러리 크기(VfxLibraryTool.Catalog 의 size)에 곱할 배율. 광역 이펙트는 size 2 m(지름)로
    /// 맞춰 두었으므로 반경을 넘기면 지름 = 2 × 반경이 된다.</param>
    /// <param name="tint">색에 곱할 색(속성 색). null 이면 그대로.</param>
    /// <param name="follow">지정하면 그 Transform 을 따라다닌다(캐스터에 붙는 이펙트).</param>
    /// <param name="looping">true 면 반복 재생을 유지하고 스스로 파괴하지 않는다. 날아가는 투사체 몸체처럼
    /// 끝나는 시점을 호출 측이 정할 때 쓴다 — follow 오브젝트를 파괴하면 함께 사라진다.</param>
    public static GameObject Play(string key, Vector3 pos, Quaternion rotation, float scale = 1f, Color? tint = null, Transform follow = null, bool looping = false)
    {
        if (key == null || !Entries.TryGetValue(key, out var entry))
            return null;

        var go = Object.Instantiate(entry.prefab, pos, rotation, follow);
        go.name = "Vfx_" + key;
        go.transform.localScale = entry.prefab.transform.localScale * (entry.scale * scale);

        float lifetime = Prepare(go, entry, tint, looping);

        // 반복을 끈 뒤 처음부터 다시 재생한다(프리팹의 playOnAwake 는 반복 설정으로 이미 시작됐다).
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        foreach (var ps in RootSystems(go))
            ps.Play(true);

        if (looping)
            return go;

        if (entry.emitTime > 0f)
            go.AddComponent<VfxStopEmitting>().after = entry.emitTime;

        Object.Destroy(go, Mathf.Max(lifetime, 0.1f));
        return go;
    }

    /// <summary>
    /// 인스턴스의 파티클 설정을 한 번 재생용으로 바꾸고, 마지막 파티클이 사라지는 시각을 돌려준다.
    /// 에디터 미리보기(VfxLibraryTool)도 같은 설정으로 찍으려고 공개한다.
    /// </summary>
    public static float Prepare(GameObject go, VfxLibrary.Entry entry, Color? tint, bool looping = false)
    {
        float lifetime = 0f;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var main = ps.main;
            if (!looping)
                main.loop = false;
            main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            main.stopAction = ParticleSystemStopAction.None;

            float emit = main.duration;
            if (entry.emitTime > 0f)
                emit = Mathf.Min(emit, entry.emitTime);
            lifetime = Mathf.Max(lifetime, main.startDelay.constantMax + emit + main.startLifetime.constantMax);
        }

        if (entry.recolor.a > 0f || tint.HasValue)
            foreach (var r in go.GetComponentsInChildren<ParticleSystemRenderer>(true))
                Recolor(r, entry.recolor, tint);
        return lifetime;
    }

    private static readonly MaterialPropertyBlock block = new MaterialPropertyBlock();

    /// <summary>
    /// 머티리얼의 색 속성을 인스턴스 단위로 바꾼다(MaterialPropertyBlock — 공용 머티리얼은 그대로).
    /// 파티클 정점색(startColor)으로는 안 된다: Vefects 셰이더는 색을 머티리얼의 _R/_G/_B/_Outline 에서 가져오고
    /// 정점색은 투명도에만 쓴다. 그래서 셰이더의 색 속성을 전부 훑어
    ///   recolor: 원래 밝기(가장 큰 채널)는 두고 색조만 recolor 로 — 명암 단계(R 밝음 → Outline 어두움)가 유지된다
    ///   tint:    원래 색에 곱한다
    /// </summary>
    private static void Recolor(Renderer r, Color recolor, Color? tint)
    {
        var mat = r.sharedMaterial;
        if (mat == null)
            return;
        var shader = mat.shader;
        r.GetPropertyBlock(block);
        for (int i = 0; i < shader.GetPropertyCount(); i++)
        {
            if (shader.GetPropertyType(i) != UnityEngine.Rendering.ShaderPropertyType.Color)
                continue;
            int id = shader.GetPropertyNameId(i);
            Color c = mat.GetColor(id);
            if (recolor.a > 0f)
            {
                float v = Mathf.Max(c.r, c.g, c.b);
                c = new Color(recolor.r * v, recolor.g * v, recolor.b * v, c.a);
            }
            if (tint.HasValue)
                c = new Color(c.r * tint.Value.r, c.g * tint.Value.g, c.b * tint.Value.b, c.a);
            block.SetColor(id, c);
        }
        r.SetPropertyBlock(block);
        block.Clear();
    }

    /// <summary>
    /// 위에 다른 파티클 시스템이 없는 시스템들. Play/Simulate 는 여기에만 건다 — 자식은 withChildren 으로
    /// 따라오고, 서브 이미터를 직접 Play 하면 부모 없이 제멋대로 방출한다.
    /// </summary>
    public static IEnumerable<ParticleSystem> RootSystems(GameObject go)
    {
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
        {
            var parent = ps.transform.parent;
            if (ps.transform == go.transform || parent == null || parent.GetComponentInParent<ParticleSystem>(true) == null)
                yield return ps;
        }
    }

    /// <summary>방향이 없는 이펙트용 단축형.</summary>
    public static GameObject Play(string key, Vector3 pos, float scale = 1f, Color? tint = null)
    {
        return Play(key, pos, Quaternion.identity, scale, tint);
    }

    /// <summary>수평 방향(dir)을 향하게 재생한다. dir 이 0 이면 +Z.</summary>
    public static GameObject PlayFacing(string key, Vector3 pos, Vector3 dir, float scale = 1f, Color? tint = null)
    {
        dir.y = 0f;
        var rot = dir.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(dir) : Quaternion.identity;
        return Play(key, pos, rot, scale, tint);
    }
}
