using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 몬스터(Dungeon Skeletons Demo) 애니메이터 컨트롤러를 만든다.
///
/// 캐릭터(<see cref="CharacterAnimationTool"/>)와 달리 이 팩은 <b>Generic 리그</b>다.
/// 클립이 뼈 이름 경로("Bip001/Bip001 Pelvis/...")로 직접 걸리므로 리타게팅이 없고,
/// 같은 팩의 모델에만 쓸 수 있다. 대신 아바타 설정을 신경 쓸 필요도 없다.
///
/// 상태 구성:
///   Locomotion  — Speed(m/s) 1D 블렌드: 대기 ↔ 걷기
///   Attack      — Attack(bool) 이 켜져 있는 동안 공격 클립 반복
///
/// 서버는 사거리 안에서 공격 중인 몬스터를 AIState.Attack 으로 둔다(MonsterAISystem::RunAttack).
/// 공격은 쿨다운마다 한 번씩이지만 상태는 교전 내내 Attack 이라, 반복 재생이 맞다.
///
/// 에디터: Tools > Character Resource > Rebuild Monster
/// CLI  : -executeMethod MonsterAnimationTool.RebuildAll
/// </summary>
public static class MonsterAnimationTool
{
    public const string OutputDir = "Assets/Animations/Monster";
    public const string ControllerPath = OutputDir + "/Skeleton.controller";
    public const string PrefabPath = "Assets/Resources/Monster.prefab";

    const string PackRoot = "Assets/DungeonCharacters/Skeletons_demo";
    public const string ModelPrefab = PackRoot + "/DungeonSkeleton_demoPrefab.prefab";

    const string IdleFbx = PackRoot + "/animation/DS_onehand_idle_A.FBX";
    const string WalkFbx = PackRoot + "/animation/DS_onehand_walk.FBX";
    const string AttackFbx = PackRoot + "/animation/DS_onehand_attack_A.FBX";

    /// <summary>서버 몬스터 이동 속도(Engine/Actor/Monster.cpp). 문턱값 점검용.</summary>
    const float ServerMoveSpeed = 3.5f;

    /// <summary>이만큼 넘게 루트 뼈가 떠나면 제자리 클립이 아니다 — 루프마다 뒤로 튄다.</summary>
    const float MaxRootDrift = 0.1f;

    public const string SpeedParam = "Speed";
    public const string AttackParam = "Attack";

    static void Log(string m) => Debug.Log("[MonsterAnim] " + m);

    static void Fail(string m)
    {
        Debug.LogError("[MonsterAnim] " + m);
        if (Application.isBatchMode) EditorApplication.Exit(1);
    }

    [MenuItem("Tools/Character Resource/Rebuild Monster")]
    public static void RebuildMenu() => RebuildAll();

    /// <summary>
    /// 컨트롤러 재생성 → 프리팹 재연결 → 검증. 컨트롤러 GUID 가 바뀌므로 셋을 묶어 돌린다.
    /// </summary>
    public static void RebuildAll()
    {
        if (!Rebuild()) return;
        CharacterResourceTool.ApplyAll();
        if (!VerifyPrefab()) Fail("프리팹 검증 실패");
        else Log("완료: 이상 없음");
    }

    public static bool Rebuild()
    {
        try
        {
            EnsureFolder(OutputDir);
            ConfigureImporters();

            var idle = FindClip(IdleFbx);
            var walk = FindClip(WalkFbx);
            var attack = FindClip(AttackFbx);
            if (idle == null || walk == null || attack == null)
            {
                Fail("클립을 찾을 수 없음 — Dungeon Skeletons Demo 가 임포트돼 있는지 확인할 것");
                return false;
            }

            float walkSpeed = MeasureWalkSpeed(walk);
            BuildController(idle, walk, attack, walkSpeed);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return true;
        }
        catch (Exception e)
        {
            Fail("실패: " + e);
            return false;
        }
    }

    /// <summary>
    /// 반복 재생할 클립은 루프를 켠다. 팩 기본값은 공격이 1회 재생이라
    /// Attack 상태에 오래 머물면 마지막 프레임에서 굳는다.
    /// </summary>
    static void ConfigureImporters()
    {
        foreach (var path in new[] { IdleFbx, WalkFbx, AttackFbx })
        {
            var imp = AssetImporter.GetAtPath(path) as ModelImporter;
            if (imp == null) { Debug.LogError("[MonsterAnim] 임포터 없음: " + path); continue; }

            var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
            bool changed = false;
            foreach (var c in clips)
                if (!c.loopTime) { c.loopTime = true; changed = true; }

            if (!changed) continue;
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
            Log("루프 켬: " + System.IO.Path.GetFileName(path));
        }
    }

    static AnimationClip FindClip(string fbxPath) =>
        AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath)?
            .OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));

    /// <summary>
    /// 걷기 클립이 암시하는 속도를 잰다. 디딘 발(더 낮은 쪽)이 뒤로 흐르는 속도가 곧 보행 속도다.
    /// 루트 뼈가 앞으로 나가는 클립이면 제자리가 아니라는 뜻이라 함께 잰다.
    /// </summary>
    static float MeasureWalkSpeed(AnimationClip clip)
    {
        var model = InstantiateModel();
        try
        {
            var lf = FindBone(model.transform, "L Foot");
            var rf = FindBone(model.transform, "R Foot");
            var hips = FindBone(model.transform, "Bip001");
            if (lf == null || rf == null || hips == null)
            {
                Log("뼈를 찾지 못해 걷기 속도 실측을 건너뛴다. 기본값 1 m/s");
                return 1f;
            }

            const int Steps = 60;
            float dt = clip.length / Steps;
            float sum = 0f; int n = 0;
            float prevZ = 0f; bool have = false;
            Vector3 hipStart = Vector3.zero;

            for (int i = 0; i <= Steps; i++)
            {
                clip.SampleAnimation(model, i * dt);
                if (i == 0) hipStart = hips.position;

                var planted = lf.position.y <= rf.position.y ? lf : rf;
                float z = planted.position.z;
                if (have)
                {
                    float dz = z - prevZ;
                    if (Mathf.Abs(dz) < 0.2f) { sum += -dz / dt; n++; }
                }
                prevZ = z; have = true;
            }

            var drift = hips.position - hipStart;
            drift.y = 0f;
            Log($"걷기 루트 이동량: {drift.magnitude:F3}m (길이 {clip.length:F2}s)");
            if (drift.magnitude > MaxRootDrift)
                Debug.LogWarning($"[MonsterAnim] 걷기 클립이 제자리가 아니다(루트 {drift.magnitude:F2}m 이동). " +
                                 "루프마다 몸이 뒤로 튈 수 있다.");

            float signed = n > 0 ? sum / n : 1f;
            // 부호가 음수면 모델이 -Z 를 보고 걷는다는 뜻이다. 속도는 크기만 쓴다.
            Log($"실측 Walk: {signed:+0.00;-0.00} m/s (표본 {n}개, 양수=+Z 를 보고 걷는다)");
            return Mathf.Max(Mathf.Abs(signed), 0.1f);
        }
        finally { UnityEngine.Object.DestroyImmediate(model); }
    }

    static void BuildController(AnimationClip idle, AnimationClip walk, AnimationClip attack, float walkSpeed)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter(SpeedParam, AnimatorControllerParameterType.Float);
        controller.AddParameter(AttackParam, AnimatorControllerParameterType.Bool);

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = SpeedParam,
            useAutomaticThresholds = false,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, walkSpeed);

        var sm = controller.layers[0].stateMachine;
        var loco = sm.AddState("Locomotion");
        loco.motion = tree;
        sm.defaultState = loco;

        var atk = sm.AddState("Attack");
        atk.motion = attack;

        var toAttack = loco.AddTransition(atk);
        toAttack.hasExitTime = false;
        toAttack.duration = 0.1f;
        toAttack.AddCondition(AnimatorConditionMode.If, 0f, AttackParam);

        var toLoco = atk.AddTransition(loco);
        toLoco.hasExitTime = false;
        toLoco.duration = 0.15f;
        toLoco.AddCondition(AnimatorConditionMode.IfNot, 0f, AttackParam);

        EditorUtility.SetDirty(controller);
        Log($"컨트롤러 생성: {ControllerPath} — Idle@0.00, Walk@{walkSpeed:F2}, Attack(bool)");

        if (walkSpeed < ServerMoveSpeed - 0.5f)
            Log($"걷기 클립 {walkSpeed:F2} m/s < 서버 {ServerMoveSpeed} m/s — Actor 가 재생 속도로 메운다(상한 1.8배).");
    }

    /// <summary>저장된 프리팹에 컨트롤러가 붙었고, 걷고 공격할 때 발이 지면 근처에 있는지 본다.</summary>
    static bool VerifyPrefab()
    {
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Debug.LogError("[MonsterAnim] 프리팹 없음: " + PrefabPath); return false; }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            root.transform.position = Vector3.zero;
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[MonsterAnim] 프리팹 Animator 에 컨트롤러가 없다");
                return false;
            }

            var lf = FindBone(animator.transform, "L Foot");
            var rf = FindBone(animator.transform, "R Foot");
            if (lf == null || rf == null) { Log("발 뼈가 없어 접지 검증을 건너뛴다"); return true; }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();

            bool ok = true;
            foreach (var (label, speed, attacking) in new[] { ("걷기", ServerMoveSpeed, false), ("공격", 0f, true) })
            {
                animator.SetFloat(SpeedParam, speed);
                animator.SetBool(AttackParam, attacking);
                animator.Update(0.5f); // 전이를 끝낸다

                float lowest = float.MaxValue, highest = float.MinValue;
                for (int i = 0; i < 40; i++)
                {
                    animator.Update(1f / 30f);
                    float low = Mathf.Min(lf.position.y, rf.position.y);
                    lowest = Mathf.Min(lowest, low);
                    highest = Mathf.Max(highest, low);
                }

                Log($"검증 접지({label}): 발목 최저 {lowest:F3}m, 최고 {highest:F3}m");
                if (lowest < -0.1f || highest > 0.6f)
                {
                    Debug.LogError($"[MonsterAnim] {label} 중 발이 지면에서 벗어난다");
                    ok = false;
                }
            }
            return ok;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }

    // ── 도우미 ──

    static GameObject InstantiateModel()
    {
        var src = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPrefab);
        if (src == null) throw new InvalidOperationException("모델 프리팹 없음: " + ModelPrefab);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
        go.transform.position = Vector3.zero;
        go.transform.rotation = Quaternion.identity;
        return go;
    }

    /// <summary>이름이 정확히 같거나 끝이 같은 뼈를 찾는다("L Foot" → "Bip001 L Foot").</summary>
    public static Transform FindBone(Transform root, string suffix) =>
        root.GetComponentsInChildren<Transform>(true)
            .FirstOrDefault(t => t.name == suffix || t.name.EndsWith(" " + suffix));

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        var parts = path.Split('/');
        var cur = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = cur + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(cur, parts[i]);
            cur = next;
        }
    }
}
