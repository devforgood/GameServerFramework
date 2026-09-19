using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

/// <summary>
/// 게임 전용 이펙트를 코드로 만든다(Assets/VFX/Game). 텍스처도 절차적으로 그린다.
///
/// 왜 직접 만드나: 맵(Flooded Grounds)·캐릭터가 사실적인 PBR 인데, 받아 둔 Vefects 팩은 검은 외곽선의 셀 셰이딩·도트라
/// 탑뷰에서 스티커처럼 떠 보였다. 부드러운 발광·불꽃 튐·연기·순간 조명으로 조립하면 어두운 안개 맵에 녹아든다.
///
///   - 텍스처는 흰색 + 알파 모양만 갖는다. 색은 파티클 색(정점색)으로 입히므로 속성별 재사용·Vfx.Play 의 tint 가 그대로 먹는다.
///   - 셰이더는 Built-in Legacy 파티클(가산/알파). 안개를 받고 모든 플랫폼에서 돈다.
///   - 크기는 실제 미터로 저작한다(카탈로그 size 0 = 재배율 없음). 광역은 지름 2 m 기준.
///
/// 매번 통째로 다시 만든다 — 프리팹을 손으로 고치지 말고 이 코드를 고쳐라. VfxLibraryTool.Build 가 먼저 부른다.
/// </summary>
public static class VfxAuthoringTool
{
    public const string Root = "Assets/VFX/Game";
    private const string TexDir = Root + "/Textures";
    private const string MatDir = Root + "/Materials";
    public const string PrefabDir = Root + "/Prefabs";
    private const string EricExplosion = "Assets/VFX/Eric VFX Studio/Free RPG VFX Sprite Sheet Starter Pack/Prefabs/Explosion 01.prefab";

    // ── 색 ──
    private static readonly Color White = Color.white;
    private static readonly Color Warm = new Color(1f, 0.84f, 0.6f);
    private static readonly Color Fire = new Color(1f, 0.5f, 0.15f);
    private static readonly Color Ember = new Color(1f, 0.62f, 0.22f);
    private static readonly Color Cold = new Color(0.6f, 0.86f, 1f);
    private static readonly Color Lightning = new Color(0.68f, 0.82f, 1f);
    private static readonly Color Poison = new Color(0.5f, 0.95f, 0.32f);
    private static readonly Color Holy = new Color(1f, 0.87f, 0.5f);
    private static readonly Color Arcane = new Color(0.72f, 0.48f, 1f);
    private static readonly Color Vital = new Color(0.45f, 1f, 0.5f);
    private static readonly Color Smoke = new Color(0.22f, 0.2f, 0.19f);
    // 먼지·연기는 밝은 풀밭과 명도가 비슷하면 사라진다. 바닥보다 확실히 어둡게 둔다.
    private static readonly Color Dust = new Color(0.3f, 0.25f, 0.19f);

    private const float GroundLift = 0.25f;

    // 저작 중에 쓰는 자산
    private static Texture2D texGlow, texSpark, texSmoke, texRing, texCrescent, texBeam;
    private static Material matAdd, matAddHot, matAlpha, matRing, matCrescent, matBeam, matSmokeAdd;
    private static Light flashLight;

    [MenuItem("Tools/VFX/Author Game Effects")]
    public static void AuthorMenu()
    {
        Debug.Log("[VfxAuthoring]\n" + string.Join("\n", Author()));
    }

    /// <summary>텍스처 → 머티리얼 → 프리팹 순으로 전부 다시 만든다. 로그 줄을 돌려준다.</summary>
    public static List<string> Author()
    {
        var log = new List<string>();
        EnsureFolder(TexDir);
        EnsureFolder(MatDir);
        EnsureFolder(PrefabDir);

        BuildTextures();
        BuildMaterials();
        flashLight = BuildFlashLight();

        Save("hit_physical", Hit(Warm, sparks: 18, flash: 1.9f));
        Save("hit_fire", Hit(Fire, sparks: 12, flash: 2f, embers: true, smoke: true));
        Save("hit_cold", Hit(Cold, sparks: 14, flash: 1.8f, mist: true));
        Save("hit_lightning", Hit(Lightning, sparks: 24, flash: 2.1f, fastSparks: true));
        Save("hit_poison", PoisonHit());
        Save("hit_holy", Hit(Holy, sparks: 10, flash: 2.1f, motes: true));

        Save("slash", Slash());
        Save("explosion", Explosion());
        Save("nova", Nova());
        Save("circle", Circle());
        Save("projectile", Projectile(White, fire: false));
        Save("projectile_fire", Projectile(Fire, fire: true));
        Save("strike", Strike(Lightning));
        Save("pillar", Pillar());
        Save("cone", Cone());
        Save("teleport_out", TeleportOut());
        Save("teleport_in", TeleportIn());
        Save("dash", DashDust());
        Save("tornado", Tornado());
        Save("heal", Heal());
        Save("death", Death());

        AssetDatabase.SaveAssets();
        log.Add($"이펙트 프리팹 저작 → {PrefabDir}");
        return log;
    }

    public static string PrefabPath(string name) => $"{PrefabDir}/{name}.prefab";

    // ────────────────────────────────────────────────────────────────────
    // 이펙트 조립
    // ────────────────────────────────────────────────────────────────────

    /// <summary>타격: 번쩍임 + 불꽃 튐(+ 속성별 덧붙임). 몸통 높이에서 터진다. 지름 약 1.5 m.</summary>
    private static GameObject Hit(Color color, int sparks, float flash, bool embers = false, bool smoke = false,
                                  bool mist = false, bool motes = false, bool fastSparks = false)
    {
        var root = new GameObject("Hit");

        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1);
        Life(f, 0.13f); Size(f, flash); Col(f, color);
        SizeCurve(f, (0f, 0.55f), (1f, 1.15f));
        Fade(f, (0f, 1f), (1f, 0f));

        var core = Sys(root, "Core", matAddHot);
        Burst(core, 1);
        Life(core, 0.08f); Size(core, flash * 0.45f); Col(core, Color.Lerp(color, White, 0.7f));
        Fade(core, (0f, 1f), (1f, 0f));

        var s = Sys(root, "Sparks", matAdd);
        Burst(s, sparks);
        Life(s, 0.16f, 0.32f); Speed(s, fastSparks ? 8f : 5f, fastSparks ? 13f : 9f); Size(s, 0.07f, 0.12f);
        Col(s, Color.Lerp(color, White, 0.4f));
        Shape(s, ParticleSystemShapeType.Sphere, 0.08f);
        var sm = s.main; sm.gravityModifier = 1.2f;
        Stretch(s, 0.045f);
        Fade(s, (0f, 1f), (0.7f, 1f), (1f, 0f));
        Drag(s, 3f);

        if (embers)
        {
            var e = Sys(root, "Embers", matAdd, world: true);
            Burst(e, 8);
            Life(e, 0.5f, 0.9f); Speed(e, 1f, 2.5f); Size(e, 0.05f, 0.1f); Col(e, Ember);
            Shape(e, ParticleSystemShapeType.Sphere, 0.2f);
            var em = e.main; em.gravityModifier = -0.3f;
            Fade(e, (0f, 1f), (1f, 0f));
        }
        if (smoke)
            SmokePuffs(root, 3, Smoke, 0.5f, 1.3f, 0.9f, 0.5f);
        if (mist)
            SmokePuffs(root, 4, new Color(0.7f, 0.85f, 1f), 0.5f, 1.4f, 0.6f, 0.35f, additive: true);
        if (motes)
        {
            var m = Sys(root, "Motes", matAdd, world: true);
            Burst(m, 12);
            Life(m, 0.5f, 0.9f); Speed(m, 0.3f, 1f); Size(m, 0.06f, 0.12f); Col(m, color);
            Shape(m, ParticleSystemShapeType.Sphere, 0.4f);
            var mm = m.main; mm.gravityModifier = -0.6f;
            Fade(m, (0f, 0f), (0.2f, 1f), (1f, 0f));
        }
        return root;
    }

    /// <summary>독 타격: 불꽃 대신 퍼지는 독 안개와 떨어지는 방울.</summary>
    private static GameObject PoisonHit()
    {
        var root = new GameObject("PoisonHit");
        var f = Sys(root, "Flash", matAdd);
        Burst(f, 1); Life(f, 0.15f); Size(f, 1.2f); Col(f, Poison);
        Fade(f, (0f, 0.8f), (1f, 0f));
        SmokePuffs(root, 5, new Color(0.35f, 0.75f, 0.2f), 0.6f, 1.6f, 0.9f, 0.55f, additive: true);
        var d = Sys(root, "Drops", matAdd, world: true);
        Burst(d, 10);
        Life(d, 0.35f, 0.6f); Speed(d, 2f, 4f); Size(d, 0.06f, 0.1f); Col(d, Poison);
        Shape(d, ParticleSystemShapeType.Hemisphere, 0.1f);
        var dm = d.main; dm.gravityModifier = 2f;
        Fade(d, (0f, 1f), (1f, 0f));
        return root;
    }

    /// <summary>
    /// 베기: 바닥과 평행한 초승달 두 겹 + 날 끝 불꽃. 피벗이 초승달의 중심(캐스터)이고 +Z 쪽으로 휜다.
    /// 지름 2 m(호 반경 약 0.9 m) — 호출 측이 사거리를 곱한다.
    /// </summary>
    private static GameObject Slash()
    {
        var root = new GameObject("Slash");

        var a = Sys(root, "Arc", matCrescent, flat: true);
        Burst(a, 1); Life(a, 0.22f); Size(a, 2f); Col(a, Warm);
        SizeCurve(a, (0f, 0.8f), (1f, 1.06f));
        Fade(a, (0f, 0f), (0.12f, 1f), (1f, 0f));

        var b = Sys(root, "ArcInner", matCrescent, flat: true);
        Burst(b, 1, 0.04f); Life(b, 0.2f); Size(b, 1.6f); Col(b, White);
        SizeCurve(b, (0f, 0.85f), (1f, 1.05f));
        Fade(b, (0f, 0f), (0.15f, 0.8f), (1f, 0f));

        // 날이 지나간 호를 따라 불꽃이 바깥으로 튄다(원 모양 방출을 앞쪽 150° 로 자름).
        var s = Sys(root, "Sparks", matAdd, flat: true);
        Burst(s, 14, 0.02f);
        Life(s, 0.15f, 0.3f); Speed(s, 2f, 5f); Size(s, 0.04f, 0.07f); Col(s, Warm);
        var sh = Shape(s, ParticleSystemShapeType.Circle, 0.85f);
        sh.arc = 150f; sh.radiusThickness = 0f; sh.rotation = new Vector3(0f, 0f, 15f);
        Stretch(s, 0.04f);
        Fade(s, (0f, 1f), (1f, 0f));
        return root;
    }

    /// <summary>폭발: Eric 화염 구 + 번쩍임·조명 + 바닥 충격파 + 연기 + 불씨. 지름 2 m.</summary>
    private static GameObject Explosion()
    {
        var root = new GameObject("Explosion");

        var eric = AssetDatabase.LoadAssetAtPath<GameObject>(EricExplosion);
        if (eric != null)
        {
            var fire = (GameObject)PrefabUtility.InstantiatePrefab(eric);
            fire.transform.SetParent(root.transform, false);
            fire.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            fire.transform.localScale = Vector3.one * 0.5f;   // 원본 사각형 3.5 m → 약 1.7 m
        }

        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1); Life(f, 0.18f); Size(f, 2.4f); Col(f, Ember);
        f.transform.localPosition = new Vector3(0f, 0.6f, 0f);
        SizeCurve(f, (0f, 0.5f), (1f, 1.2f));
        Fade(f, (0f, 1f), (1f, 0f));
        AddLight(f, 2.5f);

        Shockwave(root, "Shock", Ember, 0.1f, 2f, 0.35f, 0f);

        SmokePuffs(root, 7, Smoke, 0.9f, 1.8f, 1.6f, 0.65f, rise: 1f, radius: 0.5f);

        var e = Sys(root, "Embers", matAdd, world: true);
        Burst(e, 22);
        Life(e, 0.4f, 0.9f); Speed(e, 3f, 7f); Size(e, 0.05f, 0.1f); Col(e, Ember);
        Shape(e, ParticleSystemShapeType.Hemisphere, 0.3f);
        var em = e.main; em.gravityModifier = 1.4f;
        Stretch(e, 0.035f);
        Fade(e, (0f, 1f), (0.8f, 1f), (1f, 0f));
        Drag(e, 1.5f);
        return root;
    }

    /// <summary>파동: 바닥을 훑는 충격파 두 겹 + 번쩍임 + 바깥으로 쓸려 나가는 가루. 흰색(호출 측이 속성 색을 곱한다). 지름 2 m.</summary>
    private static GameObject Nova()
    {
        var root = new GameObject("Nova");
        Shockwave(root, "Shock", White, 0.15f, 2f, 0.4f, 0f);
        Shockwave(root, "Shock2", White, 0.1f, 1.6f, 0.35f, 0.07f, alpha: 0.6f);

        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1); Life(f, 0.2f); Size(f, 1.6f); Col(f, White);
        f.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        Fade(f, (0f, 0.9f), (1f, 0f));
        AddLight(f, 2f);

        var s = Sys(root, "Sweep", matAdd, flat: true);
        Burst(s, 36);
        Life(s, 0.25f, 0.4f); Speed(s, 3f, 5.5f); Size(s, 0.06f, 0.12f); Col(s, White);
        var sh = Shape(s, ParticleSystemShapeType.Circle, 0.15f); sh.radiusThickness = 0f;
        Stretch(s, 0.05f);
        Fade(s, (0f, 1f), (1f, 0f));
        Drag(s, 2f);
        return root;
    }

    /// <summary>예고 마법진: 바닥 고리가 떠오르고, 안쪽 고리가 가운데로 모여들며, 옅은 원판이 범위를 칠한다. 약 1초. 지름 2 m.</summary>
    private static GameObject Circle()
    {
        var root = new GameObject("Circle");

        var ring = Sys(root, "Ring", matRing, flat: true);
        Burst(ring, 1); Life(ring, 1f); Size(ring, 2f); Col(ring, White);
        Fade(ring, (0f, 0f), (0.15f, 1f), (0.85f, 1f), (1f, 0f));

        var disk = Sys(root, "Disk", matAdd, flat: true);
        Burst(disk, 1); Life(disk, 1f); Size(disk, 2.1f); Col(disk, new Color(1f, 1f, 1f, 0.35f));
        Fade(disk, (0f, 0f), (0.3f, 1f), (0.9f, 1f), (1f, 0f));

        var closing = Sys(root, "Closing", matRing, flat: true);
        var em = closing.emission; em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1), new ParticleSystem.Burst(0.33f, 1), new ParticleSystem.Burst(0.66f, 1) });
        Life(closing, 0.5f); Size(closing, 2f); Col(closing, White);
        SizeCurve(closing, (0f, 1f), (1f, 0.15f));
        Fade(closing, (0f, 0f), (0.3f, 0.7f), (1f, 0f));
        var main = closing.main; main.duration = 1f;
        return root;
    }

    /// <summary>투사체 몸체(반복 재생, 호출 측이 파괴): 빛나는 핵 + 월드 공간 꼬리 + 흩날리는 불티. 지름 약 0.8 m.</summary>
    private static GameObject Projectile(Color color, bool fire)
    {
        var root = new GameObject("Projectile");

        var core = Sys(root, "Core", matAddHot);
        Loop(core, 30f); Life(core, 0.1f); Size(core, 0.9f, 1.05f); Col(core, Color.Lerp(color, White, 0.35f));
        Fade(core, (0f, 1f), (1f, 0.6f));

        var trail = Sys(root, "Trail", matAdd, world: true);
        Loop(trail, 70f); Life(trail, 0.3f); Size(trail, 0.5f, 0.75f); Col(trail, color);
        SizeCurve(trail, (0f, 1f), (1f, 0f));
        Fade(trail, (0f, 0.9f), (1f, 0f));

        var sp = Sys(root, "Sparks", matAdd, world: true);
        Loop(sp, 40f); Life(sp, 0.2f, 0.4f); Speed(sp, 0.5f, 2f); Size(sp, 0.04f, 0.07f); Col(sp, Color.Lerp(color, White, 0.3f));
        Shape(sp, ParticleSystemShapeType.Sphere, 0.15f);
        Fade(sp, (0f, 1f), (1f, 0f));

        if (fire)
        {
            var sm = Sys(root, "Smoke", matAlpha, world: true);
            Loop(sm, 18f); Life(sm, 0.5f, 0.8f); Size(sm, 0.4f, 0.7f); Col(sm, new Color(Smoke.r, Smoke.g, Smoke.b, 0.5f));
            SizeCurve(sm, (0f, 0.6f), (1f, 1.5f));
            Fade(sm, (0f, 0f), (0.2f, 1f), (1f, 0f));
            RandomRotation(sm);
            SheetFrames(sm);
        }
        return root;
    }

    /// <summary>번개 낙뢰 지점: 푸른 번쩍임·조명 + 사방으로 튀는 불꽃 + 작은 충격파. 바닥에서 터진다.</summary>
    private static GameObject Strike(Color color)
    {
        var root = new GameObject("Strike");
        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1); Life(f, 0.15f); Size(f, 2.2f); Col(f, color);
        f.transform.localPosition = new Vector3(0f, 0.8f, 0f);
        Fade(f, (0f, 1f), (1f, 0f));
        AddLight(f, 3f);

        var s = Sys(root, "Sparks", matAdd);
        Burst(s, 26);
        Life(s, 0.15f, 0.35f); Speed(s, 6f, 12f); Size(s, 0.04f, 0.08f); Col(s, Color.Lerp(color, White, 0.5f));
        Shape(s, ParticleSystemShapeType.Hemisphere, 0.1f);
        var sm = s.main; sm.gravityModifier = 1.5f;
        Stretch(s, 0.04f);
        Fade(s, (0f, 1f), (1f, 0f));
        Drag(s, 2f);

        Shockwave(root, "Shock", color, 0.1f, 1.6f, 0.3f, 0f);
        return root;
    }

    /// <summary>빛기둥(천상의 주먹): 하늘에서 떨어지는 세로 광선 + 발밑 번쩍임·조명 + 떠오르는 빛 알갱이. 금빛. 너비 약 1.2 m.</summary>
    private static GameObject Pillar()
    {
        var root = new GameObject("Pillar");

        var beam = Sys(root, "Beam", matBeam);
        Burst(beam, 1); Life(beam, 0.45f); Col(beam, Holy);
        var main = beam.main; main.startSize3D = true;
        main.startSizeX = 1.2f; main.startSizeY = 14f; main.startSizeZ = 1f;
        beam.transform.localPosition = new Vector3(0f, 7f, 0f);
        var r = beam.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        var sol = beam.sizeOverLifetime; sol.enabled = true; sol.separateAxes = true;
        sol.x = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 0.3f), (0.15f, 1f), (1f, 0.2f)));
        sol.y = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 1f), (1f, 1f)));
        sol.z = new ParticleSystem.MinMaxCurve(1f, Curve((0f, 1f), (1f, 1f)));
        Fade(beam, (0f, 0f), (0.1f, 1f), (0.6f, 0.8f), (1f, 0f));

        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1, 0.05f); Life(f, 0.3f); Size(f, 2.6f); Col(f, Holy);
        f.transform.localPosition = new Vector3(0f, 0.4f, 0f);
        Fade(f, (0f, 1f), (1f, 0f));
        AddLight(f, 3f);

        var m = Sys(root, "Motes", matAdd, world: true);
        Burst(m, 24, 0.05f);
        Life(m, 0.6f, 1.1f); Speed(m, 0.5f, 1.5f); Size(m, 0.06f, 0.12f); Col(m, Holy);
        var sh = Shape(m, ParticleSystemShapeType.Circle, 0.8f); sh.rotation = new Vector3(90f, 0f, 0f);
        var mm = m.main; mm.gravityModifier = -0.8f;
        Fade(m, (0f, 0f), (0.2f, 1f), (1f, 0f));
        return root;
    }

    /// <summary>분사(인페르노): 정면(+Z) 원뿔로 뿜어지는 불꽃 + 뒤따르는 연기. 길이 약 2 m — 호출 측이 사거리/2 를 곱한다.</summary>
    private static GameObject Cone()
    {
        var root = new GameObject("Cone");

        var fl = Sys(root, "Flame", matSmokeAdd, world: false);
        var main = fl.main; main.duration = 0.3f;
        var em = fl.emission; em.rateOverTime = 160f;
        Life(fl, 0.22f, 0.32f); Speed(fl, 6f, 8f); Size(fl, 0.25f, 0.4f);
        var sh = Shape(fl, ParticleSystemShapeType.Cone, 0.05f); sh.angle = 18f;
        SizeCurve(fl, (0f, 0.6f), (1f, 3f));
        Grad(fl, new[] { (0f, new Color(1f, 0.9f, 0.6f)), (0.4f, Fire), (1f, new Color(0.6f, 0.12f, 0.05f)) },
                 new[] { (0f, 0f), (0.1f, 1f), (0.7f, 0.8f), (1f, 0f) });
        RandomRotation(fl);
        SheetFrames(fl);

        var sm = Sys(root, "Smoke", matAlpha);
        var smain = sm.main; smain.duration = 0.3f; smain.startDelay = 0.08f;
        var sem = sm.emission; sem.rateOverTime = 25f;
        Life(sm, 0.5f, 0.8f); Speed(sm, 3f, 4.5f); Size(sm, 0.4f, 0.6f); Col(sm, new Color(Smoke.r, Smoke.g, Smoke.b, 0.45f));
        var ssh = Shape(sm, ParticleSystemShapeType.Cone, 0.1f); ssh.angle = 15f;
        SizeCurve(sm, (0f, 0.8f), (1f, 2.2f));
        Fade(sm, (0f, 0f), (0.3f, 1f), (1f, 0f));
        RandomRotation(sm);
        SheetFrames(sm);
        return root;
    }

    /// <summary>순간이동 출발: 보랏빛 알갱이가 안으로 빨려 들고 번쩍인 뒤 연기가 남는다.</summary>
    private static GameObject TeleportOut()
    {
        var root = new GameObject("TeleportOut");
        var m = Sys(root, "Implode", matAdd);
        Burst(m, 30);
        Life(m, 0.25f); Speed(m, -5f, -4f); Size(m, 0.05f, 0.1f); Col(m, Arcane);
        Shape(m, ParticleSystemShapeType.Sphere, 1.1f);
        m.transform.localPosition = new Vector3(0f, 1f, 0f);
        Stretch(m, 0.05f);
        Fade(m, (0f, 0f), (0.3f, 1f), (1f, 0.5f));

        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1, 0.15f); Life(f, 0.22f); Size(f, 2.2f); Col(f, Arcane);
        f.transform.localPosition = new Vector3(0f, 1f, 0f);
        Fade(f, (0f, 1f), (1f, 0f));
        AddLight(f, 1.5f);

        SmokePuffs(root, 5, new Color(0.35f, 0.25f, 0.5f), 0.7f, 1.5f, 1.2f, 0.5f, delay: 0.2f, rise: 0.8f, additive: true);
        return root;
    }

    /// <summary>순간이동 도착: 번쩍임 + 바닥 고리 + 솟아오르는 알갱이.</summary>
    private static GameObject TeleportIn()
    {
        var root = new GameObject("TeleportIn");
        var f = Sys(root, "Flash", matAddHot);
        Burst(f, 1); Life(f, 0.2f); Size(f, 2.2f); Col(f, Arcane);
        f.transform.localPosition = new Vector3(0f, 1f, 0f);
        Fade(f, (0f, 1f), (1f, 0f));
        AddLight(f, 1.5f);

        Shockwave(root, "Ring", Arcane, 0.2f, 2f, 0.35f, 0f);

        var m = Sys(root, "Rise", matAdd, world: true);
        Burst(m, 24);
        Life(m, 0.4f, 0.8f); Speed(m, 2f, 4f); Size(m, 0.05f, 0.09f); Col(m, Arcane);
        var sh = Shape(m, ParticleSystemShapeType.Circle, 0.6f); sh.rotation = new Vector3(90f, 0f, 0f);
        sh.radiusThickness = 0f;
        Stretch(m, 0.04f);
        Fade(m, (0f, 1f), (1f, 0f));
        Drag(m, 2f);
        return root;
    }

    /// <summary>돌진 흙먼지: 발밑에서 뒤로(+Z = 호출 측이 준 방향) 번지는 흙먼지.</summary>
    private static GameObject DashDust()
    {
        var root = new GameObject("Dash");
        SmokePuffs(root, 4, Dust, 0.8f, 1.8f, 0.6f, 0.85f, rise: 0.3f, radius: 0.25f, height: 0.4f, forward: 1.5f);
        return root;
    }

    /// <summary>회오리 한 조각: 도는 흙먼지와 부스러기가 위로 감겨 오른다. 호출 측이 여러 번 뿌린다.</summary>
    private static GameObject Tornado()
    {
        var root = new GameObject("Tornado");
        var d = Sys(root, "Swirl", matAlpha);
        var main = d.main; main.duration = 0.3f;
        var em = d.emission; em.rateOverTime = 45f;
        Life(d, 0.6f, 0.9f); Speed(d, 0f, 0.2f); Size(d, 0.6f, 1.1f); Col(d, new Color(Dust.r, Dust.g, Dust.b, 0.8f));
        var sh = Shape(d, ParticleSystemShapeType.Circle, 0.8f); sh.rotation = new Vector3(90f, 0f, 0f);
        var vel = d.velocityOverLifetime; vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
        vel.orbitalY = new ParticleSystem.MinMaxCurve(6f);
        vel.y = new ParticleSystem.MinMaxCurve(3.5f);
        vel.x = new ParticleSystem.MinMaxCurve(0f); vel.z = new ParticleSystem.MinMaxCurve(0f);
        SizeCurve(d, (0f, 0.6f), (1f, 1.5f));
        Fade(d, (0f, 0f), (0.25f, 1f), (1f, 0f));
        RandomRotation(d);
        SheetFrames(d);

        var bits = Sys(root, "Debris", matAlpha);
        var bm = bits.main; bm.duration = 0.3f;
        var bem = bits.emission; bem.rateOverTime = 25f;
        Life(bits, 0.5f, 0.8f); Size(bits, 0.05f, 0.1f); Col(bits, new Color(0.3f, 0.27f, 0.22f, 1f));
        var bsh = Shape(bits, ParticleSystemShapeType.Circle, 0.6f); bsh.rotation = new Vector3(90f, 0f, 0f);
        var bv = bits.velocityOverLifetime; bv.enabled = true; bv.space = ParticleSystemSimulationSpace.Local;
        bv.orbitalY = new ParticleSystem.MinMaxCurve(8f);
        bv.y = new ParticleSystem.MinMaxCurve(5f);
        bv.x = new ParticleSystem.MinMaxCurve(0f); bv.z = new ParticleSystem.MinMaxCurve(0f);
        Fade(bits, (0f, 1f), (1f, 0f));

        // 흰 바람 줄기: 도는 방향으로 늘어난 가산 줄. 흙먼지만으로는 소용돌이가 읽히지 않는다.
        var wind = Sys(root, "Wind", matAdd);
        var wmn = wind.main; wmn.duration = 0.3f;
        var wem = wind.emission; wem.rateOverTime = 50f;
        Life(wind, 0.3f, 0.5f); Size(wind, 0.1f, 0.16f); Col(wind, new Color(1f, 1f, 1f, 0.7f));
        var wsh = Shape(wind, ParticleSystemShapeType.Circle, 0.9f); wsh.rotation = new Vector3(90f, 0f, 0f); wsh.radiusThickness = 0f;
        var wv = wind.velocityOverLifetime; wv.enabled = true; wv.space = ParticleSystemSimulationSpace.Local;
        wv.orbitalY = new ParticleSystem.MinMaxCurve(9f);
        wv.y = new ParticleSystem.MinMaxCurve(4f);
        wv.x = new ParticleSystem.MinMaxCurve(0f); wv.z = new ParticleSystem.MinMaxCurve(0f);
        Stretch(wind, 0.12f);
        Fade(wind, (0f, 0f), (0.3f, 1f), (1f, 0f));
        return root;
    }

    /// <summary>회복(캐스터에 붙음): 발밑 초록 고리 + 몸을 감싸는 빛 + 올라가는 빛 알갱이. 약 1초.</summary>
    private static GameObject Heal()
    {
        var root = new GameObject("Heal");
        Shockwave(root, "Ring", Vital, 0.4f, 1.8f, 0.6f, 0f);

        var g = Sys(root, "Glow", matAdd);
        Burst(g, 1); Life(g, 0.7f); Size(g, 2f); Col(g, new Color(Vital.r, Vital.g, Vital.b, 0.6f));
        g.transform.localPosition = new Vector3(0f, 1f, 0f);
        Fade(g, (0f, 0f), (0.25f, 1f), (1f, 0f));

        var m = Sys(root, "Motes", matAdd);
        var main = m.main; main.duration = 0.6f;
        var em = m.emission; em.rateOverTime = 40f;
        Life(m, 0.6f, 1f); Speed(m, 1.2f, 2.2f); Size(m, 0.06f, 0.12f); Col(m, Vital);
        var sh = Shape(m, ParticleSystemShapeType.Circle, 0.6f); sh.rotation = new Vector3(90f, 0f, 0f);
        Fade(m, (0f, 0f), (0.2f, 1f), (1f, 0f));
        return root;
    }

    /// <summary>몬스터 사망: 뼛가루 흙먼지가 퍼지고 부스러기가 떨어진다.</summary>
    private static GameObject Death()
    {
        var root = new GameObject("Death");
        SmokePuffs(root, 8, new Color(0.34f, 0.3f, 0.26f), 1f, 2f, 1.2f, 0.85f, rise: 0.6f, radius: 0.5f, height: 0.7f);
        SmokePuffs(root, 4, Smoke, 1.2f, 2.4f, 1.6f, 0.6f, delay: 0.1f, rise: 1f, radius: 0.4f, height: 0.6f);

        // 몸에서 빠져나가는 희미한 빛(영혼). 흙먼지만으로는 밝은 바닥에서 눈에 잘 안 띈다.
        var soul = Sys(root, "Soul", matAddHot);
        Burst(soul, 1); Life(soul, 0.6f); Size(soul, 1.6f); Col(soul, new Color(0.75f, 0.8f, 1f, 0.8f));
        soul.transform.localPosition = new Vector3(0f, 1.1f, 0f);
        SizeCurve(soul, (0f, 0.4f), (1f, 1.2f));
        Fade(soul, (0f, 1f), (1f, 0f));
        var wisp = Sys(root, "Wisps", matAdd, world: true);
        Burst(wisp, 16);
        Life(wisp, 0.7f, 1.2f); Speed(wisp, 0.5f, 1.2f); Size(wisp, 0.07f, 0.14f); Col(wisp, new Color(0.75f, 0.85f, 1f));
        Shape(wisp, ParticleSystemShapeType.Sphere, 0.4f);
        wisp.transform.localPosition = new Vector3(0f, 1f, 0f);
        var wm = wisp.main; wm.gravityModifier = -0.8f;
        Fade(wisp, (0f, 0f), (0.2f, 1f), (1f, 0f));

        var bits = Sys(root, "Bones", matAlpha, world: true);
        Burst(bits, 14);
        Life(bits, 0.5f, 0.9f); Speed(bits, 2f, 4.5f); Size(bits, 0.06f, 0.12f); Col(bits, new Color(0.82f, 0.78f, 0.68f, 1f));
        Shape(bits, ParticleSystemShapeType.Hemisphere, 0.4f);
        bits.transform.localPosition = new Vector3(0f, 1f, 0f);
        var bm = bits.main; bm.gravityModifier = 2f;
        Fade(bits, (0f, 1f), (0.8f, 1f), (1f, 0f));
        return root;
    }

    // ── 공통 조각 ──

    /// <summary>바닥에 눕힌 고리가 from → to 지름으로 퍼지며 사라진다.</summary>
    private static void Shockwave(GameObject root, string name, Color color, float from, float to, float life, float delay, float alpha = 1f)
    {
        var w = Sys(root, name, matRing, flat: true);
        Burst(w, 1, delay); Life(w, life); Size(w, to); Col(w, new Color(color.r, color.g, color.b, alpha));
        w.transform.localPosition = new Vector3(0f, GroundLift, 0f);
        SizeCurve(w, (0f, from / to), (0.5f, 0.85f), (1f, 1f));
        Fade(w, (0f, 1f), (1f, 0f));
    }

    /// <summary>
    /// 연기 덩어리 count 개. 크기 size0 → size1, 수명 life, 불투명도 alpha. 가산(additive)이면 빛나는 안개.
    /// forward 가 있으면 +Z 쪽으로 밀려 나간다(돌진 흙먼지).
    /// </summary>
    private static void SmokePuffs(GameObject root, int count, Color color, float size0, float size1, float life, float alpha,
                                   float delay = 0f, float rise = 0.4f, float radius = 0.3f, float height = 1f,
                                   bool additive = false, float forward = 0f)
    {
        var s = Sys(root, "Smoke", additive ? matSmokeAdd : matAlpha, world: true);
        Burst(s, count, delay);
        Life(s, life * 0.8f, life * 1.2f); Speed(s, 0.2f, 0.8f); Size(s, size0 * 0.8f, size0 * 1.2f);
        Col(s, new Color(color.r, color.g, color.b, alpha));
        Shape(s, ParticleSystemShapeType.Sphere, radius);
        s.transform.localPosition = new Vector3(0f, height, 0f);
        var v = s.velocityOverLifetime; v.enabled = true; v.space = ParticleSystemSimulationSpace.Local;
        v.x = new ParticleSystem.MinMaxCurve(0f); v.y = new ParticleSystem.MinMaxCurve(rise); v.z = new ParticleSystem.MinMaxCurve(forward);
        SizeCurve(s, (0f, 1f), (1f, size1 / size0));
        Fade(s, (0f, 0f), (0.15f, 1f), (1f, 0f));
        Drag(s, 1.5f);
        RandomRotation(s);
        SheetFrames(s);
        var rot = s.rotationOverLifetime; rot.enabled = true;
        rot.z = new ParticleSystem.MinMaxCurve(-0.8f, 0.8f);
    }

    // ────────────────────────────────────────────────────────────────────
    // 파티클 시스템 헬퍼
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 한 번 재생·가속도 없음·크기는 계층 배율을 따르는 기본 시스템. flat 이면 바닥에 눕힌다(텍스처 위쪽 = +Z).
    /// </summary>
    private static ParticleSystem Sys(GameObject parent, string name, Material mat, bool flat = false, bool world = false)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        if (flat)
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        // 바닥에 눕힌 것은 25 cm 띄운다. 게임은 바닥 콜라이더 높이에 놓는데 실제 지형 표면이 그보다 조금 높아
        // 8 cm 로 두면 고리가 땅에 묻혀 안 보였다(씬 미리보기로 확인).
        go.transform.localPosition = flat ? new Vector3(0f, GroundLift, 0f) : new Vector3(0f, 0f, 0f);

        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.duration = 1f;
        main.loop = false;
        main.playOnAwake = true;
        main.startSpeed = 0f;
        main.startLifetime = 0.5f;
        main.startSize = 1f;
        main.startColor = Color.white;
        main.startRotation = 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.simulationSpace = world ? ParticleSystemSimulationSpace.World : ParticleSystemSimulationSpace.Local;
        main.maxParticles = 200;
        var em = ps.emission; em.rateOverTime = 0f;
        var shape = ps.shape; shape.enabled = false;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.renderMode = ParticleSystemRenderMode.Billboard;
        r.alignment = flat ? ParticleSystemRenderSpace.Local : ParticleSystemRenderSpace.View;
        r.maxParticleSize = 10f;   // 기본 0.5(화면 절반)면 가까이서 큰 번쩍임이 잘린다
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        r.receiveShadows = false;
        return ps;
    }

    private static void Burst(ParticleSystem ps, int count, float time = 0f)
    {
        var em = ps.emission;
        em.SetBursts(new[] { new ParticleSystem.Burst(time, (short)count) });
    }

    private static void Loop(ParticleSystem ps, float rate)
    {
        var main = ps.main; main.loop = true; main.duration = 1f;
        var em = ps.emission; em.rateOverTime = rate;
    }

    private static void Life(ParticleSystem ps, float min, float max = -1f)
    {
        var main = ps.main;
        main.startLifetime = max < 0f ? new ParticleSystem.MinMaxCurve(min) : new ParticleSystem.MinMaxCurve(min, max);
    }

    private static void Speed(ParticleSystem ps, float min, float max)
    {
        var main = ps.main; main.startSpeed = new ParticleSystem.MinMaxCurve(min, max);
    }

    private static void Size(ParticleSystem ps, float min, float max = -1f)
    {
        var main = ps.main;
        main.startSize = max < 0f ? new ParticleSystem.MinMaxCurve(min) : new ParticleSystem.MinMaxCurve(min, max);
    }

    private static void Col(ParticleSystem ps, Color c)
    {
        var main = ps.main; main.startColor = c;
    }

    private static ParticleSystem.ShapeModule Shape(ParticleSystem ps, ParticleSystemShapeType type, float radius)
    {
        var sh = ps.shape;
        sh.enabled = true;
        sh.shapeType = type;
        sh.radius = radius;
        return sh;
    }

    private static void Stretch(ParticleSystem ps, float velocityScale)
    {
        var r = ps.GetComponent<ParticleSystemRenderer>();
        r.renderMode = ParticleSystemRenderMode.Stretch;
        r.velocityScale = velocityScale;
        r.lengthScale = 1f;
    }

    private static void Drag(ParticleSystem ps, float drag)
    {
        var lv = ps.limitVelocityOverLifetime;
        lv.enabled = true;
        lv.drag = drag;
        lv.limit = 100f;
    }

    private static void SizeCurve(ParticleSystem ps, params (float t, float v)[] keys)
    {
        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        sol.size = new ParticleSystem.MinMaxCurve(1f, Curve(keys));
    }

    private static void Fade(ParticleSystem ps, params (float t, float a)[] alpha)
    {
        Grad(ps, new[] { (0f, Color.white), (1f, Color.white) }, alpha);
    }

    private static void Grad(ParticleSystem ps, (float t, Color c)[] colors, (float t, float a)[] alpha)
    {
        var g = new Gradient();
        g.SetKeys(Array.ConvertAll(colors, k => new GradientColorKey(k.c, k.t)),
                  Array.ConvertAll(alpha, k => new GradientAlphaKey(k.a, k.t)));
        var col = ps.colorOverLifetime;
        col.enabled = true;
        col.color = new ParticleSystem.MinMaxGradient(g);
    }

    private static void RandomRotation(ParticleSystem ps)
    {
        var main = ps.main; main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
    }

    /// <summary>연기 텍스처는 2×2 네 모양이다. 파티클마다 한 장을 골라 끝까지 쓴다.</summary>
    private static void SheetFrames(ParticleSystem ps)
    {
        var ts = ps.textureSheetAnimation;
        ts.enabled = true;
        ts.numTilesX = 2;
        ts.numTilesY = 2;
        ts.animation = ParticleSystemAnimationType.WholeSheet;
        ts.frameOverTime = new ParticleSystem.MinMaxCurve(0f, 0.999f);
        ts.cycleCount = 1;
    }

    /// <summary>파티클에 점광원을 붙인다(크기에 비례한 범위, 투명도에 따라 꺼짐). 번쩍임 한 개에만 쓴다.</summary>
    private static void AddLight(ParticleSystem ps, float rangePerSize)
    {
        var lights = ps.lights;
        lights.enabled = true;
        lights.light = flashLight;
        lights.ratio = 1f;
        lights.useParticleColor = true;
        lights.sizeAffectsRange = true;
        lights.alphaAffectsIntensity = true;
        lights.rangeMultiplier = rangePerSize;
        lights.intensityMultiplier = 1f;
        lights.maxLights = 1;
    }

    private static AnimationCurve Curve(params (float t, float v)[] keys)
    {
        var c = new AnimationCurve(Array.ConvertAll(keys, k => new Keyframe(k.t, k.v)));
        for (int i = 0; i < c.length; i++)
            c.SmoothTangents(i, 0f);
        return c;
    }

    private static void Save(string name, GameObject root)
    {
        root.name = name;
        PrefabUtility.SaveAsPrefabAsset(root, PrefabPath(name));
        Object.DestroyImmediate(root);
    }

    // ────────────────────────────────────────────────────────────────────
    // 텍스처·머티리얼
    // ────────────────────────────────────────────────────────────────────

    private static void BuildTextures()
    {
        WriteTexture("glow", 128, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float a = Mathf.Exp(-r * r * 5f) * Mathf.Clamp01(1f - r);
            return a;
        });
        WriteTexture("spark", 64, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(Mathf.Exp(-r * r * 14f) * 1.4f) * Mathf.Clamp01(1f - r);
        });
        WriteTexture("ring", 256, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float edge = Mathf.Exp(-Mathf.Pow((r - 0.9f) / 0.045f, 2f));
            float inner = Mathf.Exp(-Mathf.Pow((r - 0.78f) / 0.12f, 2f)) * 0.35f;
            return Mathf.Clamp01(edge + inner) * Mathf.Clamp01((1f - r) * 20f);
        });
        // 초승달: 위쪽(+V)으로 볼록한 150° 호. 가운데가 두껍고 양끝으로 가늘어지며, 바깥 날이 가장 밝다.
        WriteTexture("crescent", 256, (x, y) =>
        {
            float r = Mathf.Sqrt(x * x + y * y);
            float ang = Mathf.Abs(Mathf.Atan2(x, y)) * Mathf.Rad2Deg;   // 0 = 위쪽
            float taper = Mathf.Clamp01(1f - ang / 78f);
            if (taper <= 0f) return 0f;
            float width = 0.03f + 0.11f * taper;
            float d = (0.86f - r) / width;                                 // 바깥 날(0.86) 에서 안쪽으로
            if (d < -0.15f) return 0f;
            float blade = d < 0f ? Mathf.Exp(-d * d * 60f) : Mathf.Exp(-d * d * 1.2f) * Mathf.Clamp01(1f - d * 0.5f);
            return Mathf.Clamp01(blade * Mathf.Pow(taper, 0.6f));
        });
        // 광선: 가로로 가운데가 밝고, 세로로 위쪽이 옅어진다.
        WriteTexture("beam", 128, (x, y) =>
        {
            float across = Mathf.Exp(-x * x * 10f) + Mathf.Exp(-x * x * 80f) * 0.8f;
            float along = Mathf.Clamp01((1f - y) * 0.8f + 0.2f) * Mathf.Clamp01((y + 1f) * 6f);
            return Mathf.Clamp01(across * along);
        });
        // 연기: 2×2 아틀라스, 칸마다 다른 잡음 덩어리.
        WriteSmokeAtlas("smoke", 256);
        AssetDatabase.Refresh();
        texGlow = ImportFx("glow");
        texSpark = ImportFx("spark");
        texRing = ImportFx("ring");
        texCrescent = ImportFx("crescent");
        texBeam = ImportFx("beam");
        texSmoke = ImportFx("smoke");
    }

    /// <summary>흰색 RGB + 알파 모양. shape(x,y) 는 -1..1 좌표(y 위쪽 +)를 받아 0..1 을 돌려준다.</summary>
    private static void WriteTexture(string name, int size, Func<float, float, float> shape)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int j = 0; j < size; j++)
            for (int i = 0; i < size; i++)
            {
                float x = (i + 0.5f) / size * 2f - 1f;
                float y = (j + 0.5f) / size * 2f - 1f;
                px[j * size + i] = new Color(1f, 1f, 1f, Mathf.Clamp01(shape(x, y)));
            }
        tex.SetPixels(px);
        File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void WriteSmokeAtlas(string name, int size)
    {
        int half = size / 2;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var px = new Color[size * size];
        for (int cell = 0; cell < 4; cell++)
        {
            int ox = (cell % 2) * half, oy = (cell / 2) * half;
            float seed = cell * 17.3f;
            for (int j = 0; j < half; j++)
                for (int i = 0; i < half; i++)
                {
                    float x = (i + 0.5f) / half * 2f - 1f;
                    float y = (j + 0.5f) / half * 2f - 1f;
                    float r = Mathf.Sqrt(x * x + y * y);
                    float n = Fbm(x * 2.2f + seed, y * 2.2f - seed);
                    float body = Mathf.Clamp01(1f - r * (1.05f + (n - 0.5f) * 0.9f));
                    float a = Mathf.Pow(body, 1.1f) * (0.75f + 0.25f * n);
                    // 가장자리가 밝고 안쪽이 살짝 어두운 볼륨감
                    float shade = 0.8f + 0.2f * n;
                    px[(oy + j) * size + ox + i] = new Color(shade, shade, shade, Mathf.Clamp01(a * 1.3f));
                }
        }
        tex.SetPixels(px);
        File.WriteAllBytes($"{TexDir}/{name}.png", tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static float Fbm(float x, float y)
    {
        float v = 0f, amp = 0.5f, f = 1f;
        for (int o = 0; o < 4; o++)
        {
            v += amp * Mathf.PerlinNoise(x * f + 31.7f, y * f + 11.3f);
            f *= 2.1f;
            amp *= 0.5f;
        }
        return v / 0.9375f;
    }

    private static Texture2D ImportFx(string name)
    {
        string path = $"{TexDir}/{name}.png";
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Default;
        imp.alphaSource = TextureImporterAlphaSource.FromInput;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.mipmapEnabled = true;
        imp.sRGBTexture = true;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static void BuildMaterials()
    {
        var add = Shader.Find("Legacy Shaders/Particles/Additive");
        var alpha = Shader.Find("Legacy Shaders/Particles/Alpha Blended");
        // _TintColor 는 2배로 곱해진다. 0.5 = 원색, 1 = 두 배(번쩍임이 바닥을 하얗게 태우도록).
        matAdd = WriteMaterial("fx_add", add, texSpark, 0.5f);
        matAddHot = WriteMaterial("fx_add_hot", add, texGlow, 0.9f);
        matAlpha = WriteMaterial("fx_smoke", alpha, texSmoke, 0.5f);
        matSmokeAdd = WriteMaterial("fx_smoke_add", add, texSmoke, 0.5f);
        matRing = WriteMaterial("fx_ring", add, texRing, 0.6f);
        matCrescent = WriteMaterial("fx_crescent", add, texCrescent, 0.75f);
        matBeam = WriteMaterial("fx_beam", add, texBeam, 0.8f);
    }

    private static Material WriteMaterial(string name, Shader shader, Texture tex, float tint)
    {
        string path = $"{MatDir}/{name}.mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }
        mat.shader = shader;
        mat.SetTexture("_MainTex", tex);
        mat.SetColor("_TintColor", new Color(tint, tint, tint, 0.5f));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    /// <summary>파티클 조명 모듈이 복제해 쓰는 점광원 틀. 그림자 없음.</summary>
    private static Light BuildFlashLight()
    {
        string path = $"{PrefabDir}/_FlashLight.prefab";
        var go = new GameObject("_FlashLight");
        var light = go.AddComponent<Light>();
        light.type = LightType.Point;
        light.range = 1f;
        light.intensity = 2.2f;
        light.shadows = LightShadows.None;
        light.renderMode = LightRenderMode.ForcePixel;
        var saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved.GetComponent<Light>();
    }

    private static void EnsureFolder(string path)
    {
        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
