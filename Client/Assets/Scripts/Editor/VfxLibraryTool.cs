using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// Assets/VFX 에셋 팩 이펙트를 게임에 연결한다.
///
///   1. 팩 머티리얼 교정: URP 전용 셰이더 → Built-in 파티클 셰이더(이 프로젝트는 Built-in 파이프라인)
///   2. Catalog 표대로 Resources/VfxLibrary.asset 을 다시 채운다(런타임 Vfx.Play 가 읽는다)
///   3. 검증: 프리팹 존재, 모든 렌더러가 Built-in 에서 그려지는 셰이더인지
///   4. (선택) 미리보기 PNG: 게임 카메라 각도로 캐릭터 옆에 재생 중간 프레임을 찍는다
///
/// 에셋은 매번 표로 다시 만든다. 인스펙터에서 고치지 말고 Catalog 를 고쳐라.
///
/// CLI: -executeMethod VfxLibraryTool.BuildAll [-previewDir 경로]
///      -executeMethod VfxLibraryTool.RenderCandidates -previewDir 경로   (팩 프리팹 전부를 찍어 고를 때)
///      (-nographics 를 빼야 PNG 가 나온다)
/// </summary>
public static class VfxLibraryTool
{
    private const string LibraryPath = "Assets/Resources/" + VfxLibrary.ResourcePath + ".asset";
    private const string PackRoot = "Assets/VFX";

    private const string Vefects = "Assets/VFX/Vefects/Flipbook VFX Bundle Lite/";
    private const string Combat = Vefects + "Combat Flipbook VFX/_ Lite/Particles/Lite_VFX_";
    private const string Flip = Vefects + "Flipbook VFX/_ Lite/Prefabs/Lite_VFX_";
    private const string Pixel = Vefects + "Pixel Craft VFX/_ Lite/Prefabs/Lite_VFX_";
    private const string Eric = "Assets/VFX/Eric VFX Studio/Free RPG VFX Sprite Sheet Starter Pack/Prefabs/";

    // Free Slash VFX 는 쓰지 않는다. 셰이더가 URP 전용 Shader Graph 이고 Scene Color/Depth(왜곡)에 기대는데,
    // Built-in 에는 불투명 텍스처가 없어 타깃을 추가해도 같은 모양이 나오지 않는다.

    // 색 상수(recolor). 팩 원색 대신 칠할 때 쓴다. Color.clear = 원색 유지.
    private static readonly Color Keep = Color.clear;
    private static readonly Color Cold = new Color(0.55f, 0.85f, 1f);
    private static readonly Color Poison = new Color(0.5f, 1f, 0.35f);
    private static readonly Color Holy = new Color(1f, 0.92f, 0.55f);
    private static readonly Color Arcane = new Color(0.75f, 0.5f, 1f);
    private static readonly Color Life = new Color(0.45f, 1f, 0.55f);

    /// <summary>
    /// 키 → 프리팹. 키 규약: 용도.속성(속성 없는 것은 용도만). SkillFxDispatcher·Actor 가 이 키를 부른다.
    ///
    /// size 는 배율 1 에서의 크기(m, 재생 중 파티클 사각형 경계의 가장 긴 변)다. 도구가 실측해 배율을 맞춘다.
    /// 그림이 사각형의 일부만 채우는 이펙트(불꽃·신성 타격)는 눈에 보이는 크기가 맞도록 size 를 크게 잡았다.
    /// recolor 는 머티리얼 색 속성을 바꾸므로 색이 텍스처에 구워진 Eric 팩에는 먹지 않는다(Vefects 전용).
    /// 광역(폭발·파동·마법진·빛기둥·분사·회오리) 은 2 m 로 두어 호출 측이 반경을 곱하면 지름이 2 × 반경이 되게 한다.
    ///
    /// 고른 기준(후보 전부를 RenderCandidates 로 찍어 비교, 2026-09-19):
    ///   - Vefects Combat/Flipbook(셀 셰이딩) 과 Eric(스프라이트 시트) 를 쓴다. 둘 다 탑뷰 카메라에서 형태가 또렷하다.
    ///   - Vefects Pixel Craft 는 도트 그림이라 다른 이펙트와 섞이면 어색해 투사체 하나만 쓴다.
    ///   - Poison_Burst 는 머티리얼 참조가 팩에서 빠져 있어(분홍) 못 쓴다. 독은 흰 계열 이펙트를 초록으로 칠한다.
    ///   - Electric_Impact Bunch 는 버스트 없이 초당 7개를 흘려 첫 번개가 언제 나올지 들쭉날쭉하다. 번개 파동은 흰 고리를 물들인다.
    /// </summary>
    private static readonly (string key, string prefab, float size, float emitTime, Color recolor)[] Catalog =
    {
        // 피격(맞은 자리, 몸통 높이). 속성별로 다른 모양.
        ("hit.physical",   Combat + "Hit_04_Loop.prefab",                                        1.4f, 0f, Keep),
        ("hit.fire",       Flip + "Flame_Burst_01.prefab",                                       2.6f, 0f, Keep),
        ("hit.lightning",  Combat + "Electric_Impact_01_Loop_CV_02.prefab",                      1.6f, 0f, Keep),
        ("hit.cold",       Flip + "Charge_01.prefab",                                            1.4f, 0.3f, Keep),
        ("hit.poison",     Combat + "Radial_Charge_Skill_01_Random_Rotate_Loop.prefab",          1.4f, 0f, Poison),
        ("hit.holy",       Eric + "Magic Hit 01.prefab",                                         2.4f, 0f, Keep),

        // 근접 베기(캐스터 정면). 호출 측이 사거리(반경)를 곱한다.
        ("slash",          Eric + "Magic Slash 01.prefab",                                       2f, 0f, Keep),

        // 폭발(착탄·낙하·돌진 끝).
        ("explosion",      Eric + "Explosion 01.prefab",                                         2f, 0f, Keep),
        ("explosion.big",  Eric + "Explosion 02.prefab",                                         2f, 0f, Keep),

        // 캐스터 중심 파동. 흰 고리라 속성 색을 곱하면 그대로 물든다.
        ("nova",           Combat + "Radial_Charge_Skill_01_GS_Random_Rotate_Bunch_01_Loop.prefab", 2f, 0f, Keep),
        ("nova.fire",      Combat + "Radial_Spiky_Hit_01_Random_Rotate_Bunch_Loop.prefab",       2f, 0f, Keep),

        // 예고 마법진(낙하·강림 전). 색이 텍스처에 구워져 있어(파랑) 다시 칠할 수 없다 — 원색으로 쓴다.
        ("circle",         Eric + "Magic Circle 01.prefab",                                      2f, 0f, Keep),

        // 투사체 몸체(날아가는 동안 따라다님).
        ("projectile.fire",      Pixel + "Fireball_Projectile_01_Color_Loop_Static.prefab",      1.2f, 0f, Keep),
        ("projectile.lightning", Combat + "Lightning_Projectile_02_Billboard_Bunch_Loop_01_CV_02.prefab", 1.2f, 1f, Keep),   // 한 주기 10초
        ("projectile",           Flip + "Charge_02.prefab",                                      2f, 0f, Color.white),

        // 번개 줄기 끝·빛기둥.
        ("strike",         Flip + "Lightning_05.prefab",                                         2.2f, 0f, Keep),
        ("strike.holy",    Flip + "Lightning_08.prefab",                                         2f, 0f, Holy),

        // 분사(인페르노). 불꽃 덩어리를 분사 길이를 따라 여러 번 터뜨린다.
        ("cone.fire",      Flip + "Flame_Burst_01.prefab",                                       3.5f, 0f, Keep),

        // 이동 계열.
        ("teleport.out",   Pixel + "Vanish_01_Color_Bunch_Loop.prefab",                          2.6f, 0f, Arcane),
        ("teleport.in",    Combat + "Radial_Charge_Skill_01_Random_Rotate_Loop.prefab",          1.8f, 0f, Arcane),
        ("dash",           Flip + "Dust_Directional_Small_01.prefab",                            1.4f, 0.3f, Keep),
        ("tornado",        Flip + "Dust_Directional_01.prefab",                                  2f, 0.3f, Keep),

        // 회복(캐스터에 붙음).
        ("heal",           Combat + "Radial_Charge_Skill_01_GS_Random_Rotate_Bunch_01_Loop.prefab", 2f, 0f, Life),

        // 몬스터 사망(해골이 부서지는 흙먼지).
        ("death",          Combat + "Explosion_02_Pivot_Loop.prefab",                            2.2f, 0f, Keep),
    };

    [MenuItem("Tools/VFX/Build Library")]
    public static void BuildMenu()
    {
        bool ok = Build(out string report);
        Debug.Log("[VfxLibrary]\n" + report);
        if (!ok)
            EditorUtility.DisplayDialog("VFX Library", "검증 실패. 콘솔 로그를 확인하라.", "OK");
    }

    /// <summary>CLI: 교정 → 라이브러리 → 검증 → 미리보기. 실패하면 종료 코드 1.</summary>
    public static void BuildAll()
    {
        bool ok = false;
        try
        {
            ok = Build(out string report);
            Debug.Log("[VfxLibrary]\n" + report);

            string previewDir = CommandLineValue("-previewDir");
            if (ok && previewDir != null && CanRender())
            {
                var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
                RenderSheets(library.entries.Select(e => (e.key, e)).ToList(), previewDir);
            }
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ok = false;
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    /// <summary>CLI: 팩 안의 프리팹을 전부 찍는다. 카탈로그에 넣을 이펙트를 고를 때 쓴다.</summary>
    public static void RenderCandidates()
    {
        bool ok = false;
        try
        {
            FixPipelineMaterials(new List<string>());
            string previewDir = CommandLineValue("-previewDir") ?? "VfxCandidates";
            var list = new List<(string, VfxLibrary.Entry)>();
            foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { PackRoot }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("/Demo/") || path.Contains("/_ Extra/") || path.Contains("Thumbnail") || path.EndsWith("Lite VFX.prefab") || path.EndsWith("Lite _ VFX.prefab"))
                    continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null || prefab.GetComponentInChildren<ParticleSystem>(true) == null)
                    continue;
                list.Add((Path.GetFileNameWithoutExtension(path), new VfxLibrary.Entry { key = path, prefab = prefab, scale = 1f, recolor = Color.clear }));
            }
            ok = CanRender();
            if (ok)
                RenderSheets(list, previewDir);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            ok = false;
        }

        if (Application.isBatchMode)
            EditorApplication.Exit(ok ? 0 : 1);
    }

    private static bool Build(out string report)
    {
        var log = new List<string>();
        bool ok = true;

        FixPipelineMaterials(log);

        var library = AssetDatabase.LoadAssetAtPath<VfxLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<VfxLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }
        library.entries.Clear();

        foreach (var c in Catalog)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(c.prefab);
            if (prefab == null)
            {
                log.Add($"실패 {c.key}: 프리팹이 없다 {c.prefab}");
                ok = false;
                continue;
            }

            foreach (var problem in RenderProblems(prefab))
            {
                log.Add($"실패 {c.key}: {problem}");
                ok = false;
            }

            var entry = new VfxLibrary.Entry
            {
                key = c.key,
                prefab = prefab,
                size = c.size,
                emitTime = c.emitTime,
                recolor = c.recolor,
            };

            // 배율 1 로 재서 목표 크기에 맞춘다.
            float measured = MeasureSize(entry);
            if (measured < 0.01f)
            {
                log.Add($"실패 {c.key}: 재생해도 파티클이 보이지 않는다(크기 0)");
                ok = false;
                entry.scale = 1f;
            }
            else
                entry.scale = c.size / measured;
            library.entries.Add(entry);

            log.Add($"{c.key,-22} {Path.GetFileNameWithoutExtension(c.prefab),-62} 실측 {measured,5:F2} m → 배율 {entry.scale:F2}");
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        log.Add($"{library.entries.Count}개 등록 → {LibraryPath}");
        log.Add(ok ? "검증 통과" : "검증 실패");
        report = string.Join("\n", log);
        return ok;
    }

    // ────────────────────────────────────────────────────────────────────
    // 머티리얼
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 팩 안의 URP Particles 머티리얼을 Built-in Legacy 파티클 셰이더로 바꾼다(Eric VFX Studio 팩).
    /// 텍스처(_BaseMap → _MainTex)와 블렌드(알파/가산)는 원래 값을 따른다. Free Slash VFX 는 건너뛴다(위 주석).
    /// </summary>
    private static void FixPipelineMaterials(List<string> log)
    {
        var alpha = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        var additive = Shader.Find("Legacy Shaders/Particles/Additive");
        if (alpha == null || additive == null)
        {
            log.Add("경고: Legacy 파티클 셰이더를 찾지 못해 머티리얼 교정을 건너뛴다");
            return;
        }

        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { PackRoot }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Free Slash VFX/"))
                continue;
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null || mat.shader == null || !mat.shader.name.StartsWith("Universal Render Pipeline/Particles"))
                continue;

            var tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            // URP 파티클: _Blend 0 알파, 1 프리멀티플, 2 가산, 3 곱하기.
            bool isAdditive = mat.HasProperty("_Blend") && Mathf.RoundToInt(mat.GetFloat("_Blend")) == 2;

            mat.shader = isAdditive ? additive : alpha;
            mat.SetTexture("_MainTex", tex);
            // Legacy 파티클은 _TintColor 에 2를 곱한다. 0.5 가 원색.
            mat.SetColor("_TintColor", color * 0.5f);
            EditorUtility.SetDirty(mat);
            log.Add($"머티리얼 교정 {Path.GetFileName(path)} → {mat.shader.name}");
        }
        AssetDatabase.SaveAssets();
    }

    /// <summary>Built-in 파이프라인에서 그려지지 않을 렌더러를 찾는다(분홍색 방지).</summary>
    private static IEnumerable<string> RenderProblems(GameObject prefab)
    {
        foreach (var r in prefab.GetComponentsInChildren<Renderer>(true))
        {
            foreach (var m in r.sharedMaterials)
            {
                if (m == null)
                {
                    // 파티클 트레일 슬롯 등 비어 있어도 되는 자리가 있다. 파티클 본체 슬롯만 본다.
                    if (r is ParticleSystemRenderer psr && psr.sharedMaterial == null && psr.renderMode != ParticleSystemRenderMode.None)
                        yield return $"{r.name}: 머티리얼 없음";
                    continue;
                }
                var shader = m.shader;
                string shaderPath = shader != null ? AssetDatabase.GetAssetPath(shader) : "";
                if (shader == null || shader.name == "Hidden/InternalErrorShader")
                    yield return $"{r.name}/{m.name}: 셰이더 없음";
                else if (shader.name.StartsWith("Universal Render Pipeline/") || shaderPath.EndsWith(".shadergraph"))
                    yield return $"{r.name}/{m.name}: URP 전용 셰이더 {shader.name}";
                else if (ShaderUtil.ShaderHasError(shader))
                    yield return $"{r.name}/{m.name}: 셰이더 컴파일 에러 {shader.name}";
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 측정·미리보기
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 배율 1 로 재생했을 때 파티클 경계의 가장 긴 변(m). 여러 시점 중 가장 큰 값.
    /// 타격 이펙트는 수명이 0.3초 남짓이라 앞쪽을 촘촘히 잰다.
    /// </summary>
    private static float MeasureSize(VfxLibrary.Entry source)
    {
        var entry = new VfxLibrary.Entry { prefab = source.prefab, scale = 1f, emitTime = source.emitTime };
        var go = Spawn(entry, Vector3.zero, Quaternion.identity);
        try
        {
            float life = Vfx.Prepare(go, entry, null);
            float max = 0f;
            foreach (float t in new[] { 0.05f, 0.1f, 0.18f, 0.3f, 0.5f, 0.8f, 1.2f })
            {
                if (t > life + 0.05f)
                    break;
                SimulateTo(go, t);
                var b = Bounds(go);
                if (b.HasValue)
                    max = Mathf.Max(max, b.Value.size.x, b.Value.size.y, b.Value.size.z);
            }
            return max;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static GameObject Spawn(VfxLibrary.Entry entry, Vector3 pos, Quaternion rot)
    {
        var go = (GameObject)Object.Instantiate(entry.prefab, pos, rot);
        go.transform.localScale = entry.prefab.transform.localScale * entry.scale;
        go.hideFlags = HideFlags.DontSave;
        return go;
    }

    private static void SimulateTo(GameObject go, float t)
    {
        foreach (var ps in Vfx.RootSystems(go))
            ps.Simulate(Mathf.Max(t, 0.01f), true, true, true);
    }

    /// <summary>
    /// 살아 있는 파티클(위치 ± 현재 크기의 절반)을 감싸는 경계. 렌더러 bounds 는 최대 크기로 넉넉히 잡혀
    /// 실제보다 몇 배 커서(불꽃 1 m 가 12 m 로 잡힘) 배율 계산에 못 쓴다.
    /// </summary>
    private static Bounds? Bounds(GameObject go)
    {
        Bounds? total = null;
        foreach (var ps in go.GetComponentsInChildren<ParticleSystem>())
        {
            var r = ps.GetComponent<ParticleSystemRenderer>();
            if (r == null || !r.enabled || ps.particleCount == 0)
                continue;
            var particles = new ParticleSystem.Particle[ps.particleCount];
            int n = ps.GetParticles(particles);
            bool local = ps.main.simulationSpace == ParticleSystemSimulationSpace.Local;
            float s = ps.transform.lossyScale.x;
            for (int i = 0; i < n; i++)
            {
                Vector3 pos = local ? ps.transform.TransformPoint(particles[i].position) : particles[i].position;
                Vector3 size3 = particles[i].GetCurrentSize3D(ps);
                float half = Mathf.Max(size3.x, size3.y, size3.z) * 0.5f * s;   // Hierarchy 모드라 크기는 시뮬레이션 공간과 무관하게 변환 배율을 받는다
                var b = new Bounds(pos, Vector3.one * half * 2f);
                if (total.HasValue) { var t = total.Value; t.Encapsulate(b); total = t; }
                else total = b;
            }
        }
        return total;
    }

    private static bool CanRender()
    {
        // -nographics 로 띄우면 그래픽 장치가 없어 렌더할 수 없다.
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            Debug.LogWarning("[VfxLibrary] 그래픽 장치가 없어 미리보기를 건너뛴다(-nographics 를 빼라)");
            return false;
        }
        return true;
    }

    /// <summary>
    /// 이펙트마다 한 장: 게임 카메라 각도(피치 53°)에서 캐릭터 옆에 같은 이펙트를 재생 0.08/0.18/0.32/0.55초 시점으로 네 번 놓는다.
    /// 모든 장을 합친 목록 이미지(contact_sheet.png)도 만든다.
    /// </summary>
    private static void RenderSheets(List<(string name, VfxLibrary.Entry entry)> items, string dir)
    {
        Directory.CreateDirectory(dir);
        bool asyncShaders = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;

        var temp = new List<Object>();
        try
        {
            var camGo = new GameObject("VfxPreviewCamera") { hideFlags = HideFlags.DontSave };
            temp.Add(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.22f, 0.24f, 0.2f);
            cam.fieldOfView = 40f;
            cam.aspect = 2f;

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.hideFlags = HideFlags.DontSave;
            ground.transform.localScale = Vector3.one * 6f;
            var groundMat = new Material(Shader.Find("Standard")) { color = new Color(0.33f, 0.31f, 0.25f) };
            ground.GetComponent<Renderer>().sharedMaterial = groundMat;
            temp.Add(ground);
            temp.Add(groundMat);

            var lightGo = new GameObject("VfxPreviewLight") { hideFlags = HideFlags.DontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            temp.Add(lightGo);

            // 크기 비교용 캐릭터(2.16 m). 가운데에 세운다.
            var characterPrefab = Resources.Load<GameObject>("Character2");
            if (characterPrefab != null)
            {
                var character = (GameObject)PrefabUtility.InstantiatePrefab(characterPrefab);
                character.hideFlags = HideFlags.DontSave;
                character.transform.SetPositionAndRotation(new Vector3(0f, 0f, 0f), Quaternion.Euler(0f, 160f, 0f));
                temp.Add(character);
            }

            Quaternion rot = Quaternion.Euler(53f, 0f, 0f);
            Vector3 focus = new Vector3(0f, 1f, 0f);
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * 13f, rot);

            // 절대 시각으로 찍는다. 수명 비율로 찍으면 타격 이펙트(수명 0.3초)는 첫 칸부터 이미 사라져 있다.
            var times = new[] { 0.08f, 0.18f, 0.32f, 0.55f };
            var slots = new[] { -6f, -2.4f, 2.4f, 6f };
            var sheetFiles = new List<string>();

            foreach (var (name, entry) in items)
            {
                var spawned = new List<GameObject>();
                var counts = new List<string>();
                for (int i = 0; i < times.Length; i++)
                {
                    var go = Spawn(entry, new Vector3(slots[i], 1f, 0f), Quaternion.identity);   // 몸통 높이(피격 이펙트 위치)
                    Vfx.Prepare(go, entry, null);
                    SimulateTo(go, times[i]);
                    spawned.Add(go);
                    counts.Add($"{times[i]:F2}s:{go.GetComponentsInChildren<ParticleSystem>().Sum(p => p.particleCount)}");
                }
                // 파티클 수가 0 이면 시뮬레이션 문제, 수는 있는데 안 보이면 렌더(셰이더·크기) 문제다.
                Debug.Log($"[VfxLibrary] {name} 파티클 수 {string.Join(" ", counts)}");

                string file = Path.Combine(dir, Sanitize(name) + ".png");
                SavePng(cam, file, 1200, 600);
                sheetFiles.Add(file);
                foreach (var go in spawned)
                    Object.DestroyImmediate(go);
            }

            BuildContactSheet(sheetFiles, Path.Combine(dir, "contact_sheet.png"));
            Debug.Log($"[VfxLibrary] 미리보기 {sheetFiles.Count}장 저장: {dir}");
        }
        finally
        {
            foreach (var o in temp)
                if (o != null) Object.DestroyImmediate(o);
            ShaderUtil.allowAsyncCompilation = asyncShaders;
        }
    }

    private static void SavePng(Camera cam, string path, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        RenderTexture.active = null;
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
    }

    /// <summary>개별 PNG 를 3열 격자로 줄여 한 장에 모은다. 파일 이름은 각 칸 순서대로 로그에 남긴다.</summary>
    private static void BuildContactSheet(List<string> files, string path)
    {
        const int cols = 3, cellW = 600, cellH = 300;
        int rows = Mathf.CeilToInt(files.Count / (float)cols);
        if (rows == 0)
            return;
        var sheet = new Texture2D(cols * cellW, rows * cellH, TextureFormat.RGB24, false);
        var order = new List<string>();
        for (int i = 0; i < files.Count; i++)
        {
            var src = new Texture2D(2, 2);
            src.LoadImage(File.ReadAllBytes(files[i]));
            int cx = (i % cols) * cellW;
            int cy = (rows - 1 - i / cols) * cellH;
            for (int y = 0; y < cellH; y++)
                for (int x = 0; x < cellW; x++)
                    sheet.SetPixel(cx + x, cy + y, src.GetPixelBilinear((x + 0.5f) / cellW, (y + 0.5f) / cellH));
            order.Add($"{i + 1}. {Path.GetFileNameWithoutExtension(files[i])}");
            Object.DestroyImmediate(src);
        }
        sheet.Apply();
        File.WriteAllBytes(path, sheet.EncodeToPNG());
        Object.DestroyImmediate(sheet);
        Debug.Log("[VfxLibrary] contact_sheet 순서(왼→오, 위→아래)\n" + string.Join("\n", order));
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }

    private static string CommandLineValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, key);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }
}
