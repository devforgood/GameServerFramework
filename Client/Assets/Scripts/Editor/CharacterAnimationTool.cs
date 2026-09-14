using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 캐릭터 로코모션 컨트롤러를 만든다.
///
/// 클립은 직접 만들지 않고 <b>RPG Animations Pack FREE</b>(DoubleL, 에셋스토어 무료) 것을 쓴다.
/// 손으로 사인파를 그려 만든 걸음은 아무리 다듬어도 기계적이라, 표준 팩을 가져오는 편이 낫다.
///
/// 쓰는 클립은 전부 <b>InPlace</b> 변형이다. 루트 모션이 빠져 있어 제자리에서 걷고,
/// 실제 위치는 서버가 준 좌표를 ActorSync 가 넣는다. 이 프로젝트 구조에 그대로 맞는다.
///
/// 리그는 양쪽 다 Humanoid 라 뼈 이름이 달라도 아바타를 통해 리타게팅된다.
///
/// 에디터: Tools > Character Resource > Rebuild Locomotion
/// CLI  : -executeMethod CharacterAnimationTool.Rebuild
/// </summary>
public static class CharacterAnimationTool
{
    public const string OutputDir = "Assets/Animations/Character";
    public const string ControllerPath = OutputDir + "/Locomotion.controller";

    // Warrior Pack Bundle 2 FREE (ExplosiveLLC) 의 Knight 세트.
    //
    // 처음에는 RPG Animations Pack 의 "One Hand Up" 을 썼는데, 그쪽은 한손 무기를 든
    // 전투 자세라 몸통이 24도쯤 앞으로 숙여져 있다. 임포트 설정을 고쳐도 그대로다 —
    // 굽기 설정 문제가 아니라 동작 자체가 그렇게 만들어져 있다.
    // 키 큰 이 캐릭터에 리타게팅하면 더 과장돼서 웅크린 채 달리는 것처럼 보였다.
    // Knight 세트는 똑바로 선 기본 이동이라 이쪽이 맞다.
    const string PackRoot = "Assets/ExplosiveLLC/Warrior Pack Bundle 2 FREE/" +
                            "Knight Warrior Mecanim Animation Pack/Animations";

    /// <summary>블렌드 트리에 넣을 클립. 문턱값은 클립에서 실측해 정하므로 여기 적지 않는다.</summary>
    class Entry
    {
        public string Name;
        public string Fbx;
        /// <summary>제자리 클립(대기)은 이동 속도가 0 이라 실측 대상이 아니다.</summary>
        public bool Stationary;
    }

    static readonly Entry[] Clips =
    {
        new Entry { Name = "Idle", Fbx = PackRoot + "/Knight@Idle.FBX", Stationary = true },
        new Entry { Name = "Walk", Fbx = PackRoot + "/Knight@Walk.FBX" },
        new Entry { Name = "Run",  Fbx = PackRoot + "/Knight@Run.FBX" },
    };

    /// <summary>서버가 쓰는 캐릭터 이동 속도(Engine/Actor/Character.cpp). 문턱값 상한 점검용.</summary>
    public const float ServerMoveSpeed = 4.5f;

    static void Log(string m) => Debug.Log("[CharacterAnim] " + m);

    [MenuItem("Tools/Character Resource/Rebuild Locomotion")]
    public static void RebuildMenu() => RebuildAll();

    /// <summary>
    /// 컨트롤러를 다시 만든 뒤 프리팹까지 다시 연결한다. 보통은 이쪽을 부르면 된다.
    ///
    /// 컨트롤러는 지웠다 새로 만들기 때문에 GUID 가 바뀐다. 프리팹은 GUID 로 참조하므로
    /// 이어서 ApplyAll 을 돌리지 않으면 Animator 의 컨트롤러가 끊긴 채로 남는다.
    /// </summary>
    public static void RebuildAll()
    {
        Rebuild();
        CharacterResourceTool.ApplyAll();

        // 접지 검증은 반드시 프리팹을 다시 연결한 뒤에 해야 한다.
        // Rebuild 시점에는 컨트롤러 참조가 끊긴 상태라 검사가 통째로 건너뛰어진다.
        if (!VerifyFeetOnGround())
        {
            Debug.LogError("[CharacterAnim] 접지 검증 실패");
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    public static void Rebuild()
    {
        try
        {
            // 예전에 코드로 만들어 쓰던 클립은 더 이상 쓰지 않는다.
            foreach (var stale in new[] { OutputDir + "/Walk.anim", OutputDir + "/Idle.anim" })
                if (AssetDatabase.LoadAssetAtPath<AnimationClip>(stale) != null)
                {
                    AssetDatabase.DeleteAsset(stale);
                    Log("직접 만들었던 클립 삭제: " + stale);
                }

            EnsureFolder(OutputDir);

            ConfigureImporters();

            var loaded = new List<(Entry entry, AnimationClip clip)>();
            foreach (var e in Clips)
            {
                var clip = FindClip(e.Fbx);
                if (clip == null)
                {
                    Debug.LogError($"[CharacterAnim] 클립을 찾을 수 없음: {e.Fbx}\n" +
                                   "RPG Animations Pack FREE 를 먼저 임포트해야 한다.");
                    if (Application.isBatchMode) EditorApplication.Exit(1);
                    return;
                }
                loaded.Add((e, clip));
                Log($"클립 확보 {e.Name}: {clip.name} (길이 {clip.length:F2}s, humanMotion={clip.humanMotion}, loop={clip.isLooping})");
            }

            // 각 클립이 실제로 몇 m/s 로 걷는지 재서 블렌드 문턱값으로 쓴다.
            // 문턱값이 클립 속도와 어긋난 만큼 발이 땅에서 미끄러진다.
            var speeds = MeasureClipSpeeds(loaded);

            BuildController(loaded, speeds);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Verify();
        }
        catch (Exception e)
        {
            Debug.LogError("[CharacterAnim] 실패: " + e);
            if (Application.isBatchMode) EditorApplication.Exit(1);
        }
    }

    /// <summary>
    /// 팩 클립의 임포트 설정을 이 프로젝트에 맞게 고친다.
    ///
    /// 팩 기본값은 루트 회전을 <b>Original 기준</b>으로 포즈에 굽는다. 그러면 원본이 갖고 있던
    /// 기울기가 그대로 남아 몸통이 20~30도 앞으로 쏠린 채 달린다(실측으로 확인).
    /// Body Orientation 기준으로 바꾸면 몸통이 바로 선다.
    ///
    /// 위치는 XZ 까지 포즈에 구워 완전한 제자리 클립으로 만든다. 이동은 서버가 준 좌표로만 한다.
    /// 높이는 발 기준으로 맞춰야 리타게팅 후에도 지면에 선다.
    /// </summary>
    static void ConfigureImporters()
    {
        foreach (var e in Clips)
        {
            var imp = AssetImporter.GetAtPath(e.Fbx) as ModelImporter;
            if (imp == null) { Debug.LogError("[CharacterAnim] 임포터 없음: " + e.Fbx); continue; }

            var clips = imp.clipAnimations.Length > 0 ? imp.clipAnimations : imp.defaultClipAnimations;
            if (clips.Length == 0) { Debug.LogError("[CharacterAnim] 클립 정의 없음: " + e.Fbx); continue; }

            bool changed = false;
            foreach (var c in clips)
            {
                if (!c.loopTime)                 { c.loopTime = true;                 changed = true; }
                if (!c.lockRootRotation)         { c.lockRootRotation = true;         changed = true; }
                if (c.keepOriginalOrientation)   { c.keepOriginalOrientation = false; changed = true; }
                if (!c.lockRootHeightY)          { c.lockRootHeightY = true;          changed = true; }
                if (c.keepOriginalPositionY)     { c.keepOriginalPositionY = false;   changed = true; }
                if (!c.heightFromFeet)           { c.heightFromFeet = true;           changed = true; }
                if (!c.lockRootPositionXZ)       { c.lockRootPositionXZ = true;       changed = true; }
                if (c.keepOriginalPositionXZ)    { c.keepOriginalPositionXZ = false;  changed = true; }
            }

            if (!changed) { Log($"임포트 설정 이미 맞음: {e.Name}"); continue; }

            imp.clipAnimations = clips;
            imp.SaveAndReimport();
            Log($"임포트 설정 교정: {e.Name} (루트 회전=몸통 기준, 높이=발 기준, XZ 제자리, 루프 켬)");
        }
    }

    /// <summary>FBX 안의 AnimationClip 서브에셋을 꺼낸다(미리보기용 __preview__ 는 거른다).</summary>
    static AnimationClip FindClip(string fbxPath)
    {
        var subs = AssetDatabase.LoadAllAssetRepresentationsAtPath(fbxPath);
        if (subs == null) return null;
        return subs.OfType<AnimationClip>()
                   .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    /// <summary>
    /// 클립이 암시하는 전진 속도를 잰다.
    /// 디딘 발(더 낮은 쪽)이 뒤로 흐르는 속도가 곧 그 클립의 보행 속도다.
    /// </summary>
    static Dictionary<string, float> MeasureClipSpeeds(List<(Entry entry, AnimationClip clip)> loaded)
    {
        var result = new Dictionary<string, float>();
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Character2.prefab");
        if (prefab == null)
        {
            Log("속도 실측 건너뜀: Character2 프리팹이 없다. 문턱값은 기본값을 쓴다.");
            foreach (var (e, _) in loaded) result[e.Name] = e.Stationary ? 0f : 1f;
            return result;
        }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            root.transform.position = Vector3.zero;
            root.transform.rotation = Quaternion.identity;
            var animator = root.GetComponentInChildren<Animator>();
            var animGo = animator.gameObject;
            var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            foreach (var (e, clip) in loaded)
            {
                if (e.Stationary) { result[e.Name] = 0f; continue; }

                const int Steps = 60;
                float dt = clip.length / Steps;
                float sum = 0f; int n = 0;
                float prevZ = 0f; bool have = false;

                for (int i = 0; i <= Steps; i++)
                {
                    clip.SampleAnimation(animGo, i * dt);
                    bool leftPlanted = lf.position.y <= rf.position.y;
                    float z = leftPlanted ? lf.position.z : rf.position.z;
                    if (have)
                    {
                        float dz = z - prevZ;
                        // 디딘 발이 바뀌는 순간에는 z 가 크게 튄다 — 그 표본은 버린다.
                        if (Mathf.Abs(dz) < 0.2f) { sum += -dz / dt; n++; }
                    }
                    prevZ = z; have = true;
                }

                float speed = n > 0 ? sum / n : 1f;
                // 캐릭터가 +Z 를 보도록 만들어 두었지만, 팩이 반대로 만들었다면 부호가 뒤집힌다.
                speed = Mathf.Abs(speed);
                result[e.Name] = speed;
                Log($"실측 {e.Name}: 약 {speed:F2} m/s (표본 {n}개)");
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }

        return result;
    }

    /// <summary>Speed(m/s) 하나로 대기→걷기→달리기를 섞는 블렌드 트리.</summary>
    static void BuildController(List<(Entry entry, AnimationClip clip)> loaded, Dictionary<string, float> speeds)
    {
        AssetDatabase.DeleteAsset(ControllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);

        var tree = new BlendTree
        {
            name = "Locomotion",
            blendType = BlendTreeType.Simple1D,
            blendParameter = "Speed",
            useAutomaticThresholds = false,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);

        // 문턱값은 실측 속도. 오름차순이어야 블렌드 트리가 받는다.
        var ordered = loaded.OrderBy(x => speeds[x.entry.Name]).ToList();
        foreach (var (e, clip) in ordered)
            tree.AddChild(clip, speeds[e.Name]);

        var layer = controller.layers[0];
        var state = layer.stateMachine.AddState("Locomotion");
        state.motion = tree;
        layer.stateMachine.defaultState = state;

        EditorUtility.SetDirty(controller);
        Log("컨트롤러 생성: " + ControllerPath + " — " +
            string.Join(", ", ordered.Select(x => $"{x.entry.Name}@{speeds[x.entry.Name]:F2}")));

        float top = ordered.Max(x => speeds[x.entry.Name]);
        if (top < ServerMoveSpeed - 0.5f)
            Debug.LogWarning($"[CharacterAnim] 가장 빠른 클립이 {top:F2} m/s 인데 서버 이동 속도는 " +
                             $"{ServerMoveSpeed} m/s 다. 그 차이만큼 발이 미끄러진다 — " +
                             "Actor 가 재생 속도를 보정하도록 되어 있는지 확인할 것.");
    }

    // ── 도우미 ──

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

    static void Verify()
    {
        bool ok = true;

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) { Debug.LogError("[CharacterAnim] 컨트롤러 없음"); ok = false; }
        else Log($"검증 컨트롤러: 파라미터 {ctrl.parameters.Length}개, 상태 {ctrl.layers[0].stateMachine.states.Length}개");

        ok &= VerifyFeetOnGround();

        Log(ok ? "완료: 이상 없음" : "완료: 문제 있음");
        if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
    }

    /// <summary>
    /// 실제 Animator 로 걷기를 돌리며 발이 지면 근처에 머무는지 본다.
    /// 팩 클립은 루트 높이가 들어 있지만, 리타게팅 결과까지 맞는지는 돌려 봐야 안다.
    /// </summary>
    static bool VerifyFeetOnGround()
    {
        const string PrefabPath = "Assets/Resources/Character2.prefab";

        // 직전에 ApplyAll 이 저장한 내용을 확실히 읽도록 다시 임포트한다.
        // 이게 없으면 예전에 메모리에 올라온 프리팹을 잡아 컨트롤러가 없다고 나온다.
        AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null) { Log("접지 검증 건너뜀: Character2 프리팹이 없다"); return true; }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            root.transform.position = Vector3.zero;
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Log("접지 검증 건너뜀: 프리팹에 Animator 가 없다");
                return true;
            }
            if (animator.runtimeAnimatorController == null)
            {
                Debug.LogError("[CharacterAnim] 프리팹 Animator 에 컨트롤러가 붙어 있지 않다 — " +
                               "컨트롤러를 새로 만든 뒤 ApplyAll 로 다시 연결했는지 확인할 것.");
                return false;
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.SetFloat("Speed", ServerMoveSpeed);
            animator.Update(0f);

            var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);

            float lowest = float.MaxValue, highest = float.MinValue;
            for (int i = 0; i < 40; i++)
            {
                animator.Update(1f / 30f);
                float low = Mathf.Min(lf.position.y, rf.position.y);
                lowest = Mathf.Min(lowest, low);
                highest = Mathf.Max(highest, low);
            }

            Log($"검증 접지: 발목 최저 {lowest:F3}m, 최고 {highest:F3}m");
            if (lowest < -0.1f)
            {
                Debug.LogError($"[CharacterAnim] 발이 지면 아래로 내려간다({lowest:F3}m).");
                return false;
            }
            return true;
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
