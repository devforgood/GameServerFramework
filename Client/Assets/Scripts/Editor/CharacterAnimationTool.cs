using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// 걷기/대기 애니메이션 클립과 로코모션 컨트롤러를 코드로 만들어 낸다.
///
/// 왜 코드로 만드는가:
///   Polytope 무료 팩에는 정지 포즈(PT_Pose_01) 하나뿐이라 이동 클립이 없다.
///   리그가 Humanoid 라 근육(muscle) 커브만 채우면 뼈 이름과 무관하게 동작한다.
///
/// 근육 커브 바인딩 이름은 HumanTrait.MuscleName 문자열을 그대로 쓴다.
/// (예: "Left Upper Leg Front-Back" — 띄어쓰기를 빼면 조용히 무시된다. 실측으로 확인함.)
///
/// 에디터: Tools > Character Resource > Rebuild Locomotion Clips
/// CLI  : -executeMethod CharacterAnimationTool.Rebuild
/// </summary>
public static class CharacterAnimationTool
{
    public const string OutputDir = "Assets/Animations/Character";
    public const string ControllerPath = OutputDir + "/Locomotion.controller";
    public const string WalkPath = OutputDir + "/Walk.anim";
    public const string IdlePath = OutputDir + "/Idle.anim";

    /// <summary>블렌드 트리에서 걷기가 완전히 켜지는 속도(m/s). 서버 이동 속도와 맞춘다.</summary>
    public const float WalkSpeed = 3.0f;

    // ── 걷기 사이클 진폭 (근육 단위, -1..1) ──
    const float CycleLength   = 1.0f;   // 한 바퀴 = 두 걸음
    const int   SampleCount   = 24;     // 한 바퀴를 몇 키프레임으로 쪼갤지

    const float HipSwing      = 0.42f;  // 허벅지 앞뒤 흔들기
    const float KneeBend      = 0.45f;  // 무릎 굽힘 최대치
    // 다리가 뒤에서 앞으로 넘어오는 구간(유각기)에서 가장 많이 굽어야 발이 땅에 끌리지 않는다.
    const float KneeBendPhase = 1.75f * Mathf.PI;
    const float AnkleSwing    = 0.22f;  // 발목
    const float ArmSwing      = 0.28f;  // 팔 앞뒤 흔들기
    const float SpineLean     = 0.06f;  // 살짝 앞으로

    // ── 정지 자세 보정 ──
    // Humanoid 근육이 전부 0이면 팔이 옆으로 벌어진 자세가 된다. 차렷에 가깝게 내려 준다.
    const float ArmRest       = -0.62f; // "Arm Down-Up" : 음수가 아래

    /// <summary>
    /// 팔꿈치("Forearm Stretch"). <b>양수가 펴는 방향</b>이다(무릎과 같은 규칙).
    /// 어깨~손 거리 실측: -0.6 → 0.21m, 0 → 0.39m, +0.9 → 0.53m.
    /// 0.5 면 0.49m 로, 완전히 펴지 않은 자연스러운 팔이 된다.
    /// </summary>
    const float ElbowRest     = 0.5f;

    /// <summary>
    /// 루트 높이(RootT.y). 이 커브가 없으면 휴머노이드 클립은 <b>엉덩이를</b> 원점에 놓아서
    /// 캐릭터가 허리까지 땅에 묻힌다. 클립을 손으로 만들 때 반드시 같이 넣어야 한다.
    ///
    /// 값은 이 리그에서 실측해 구했다(모델을 바꾸면 다시 재야 한다):
    ///   RootT.y 0 → 발목 y = -0.696, 기울기 = 1.017 m/단위, 기본 자세 발목 y = 0.108
    ///   ⇒ (0.108 + 0.696) / 1.017 = 0.790
    /// Rebuild 끝의 검증이 실제로 발이 지면에 오는지 다시 확인한다.
    /// </summary>
    const float RootHeight    = 0.790f;

    // 근육 이름 (HumanTrait.MuscleName 과 철자까지 동일해야 한다)
    const string LHip = "Left Upper Leg Front-Back";
    const string RHip = "Right Upper Leg Front-Back";
    const string LKnee = "Left Lower Leg Stretch";
    const string RKnee = "Right Lower Leg Stretch";
    const string LAnkle = "Left Foot Up-Down";
    const string RAnkle = "Right Foot Up-Down";
    const string LArmDU = "Left Arm Down-Up";
    const string RArmDU = "Right Arm Down-Up";
    const string LArmFB = "Left Arm Front-Back";
    const string RArmFB = "Right Arm Front-Back";
    const string LElbow = "Left Forearm Stretch";
    const string RElbow = "Right Forearm Stretch";
    const string Spine = "Spine Front-Back";
    /// <summary>근육이 아니라 루트 이동 커브. 이름 그대로 써야 한다.</summary>
    const string RootY = "RootT.y";

    static void Log(string m) => Debug.Log("[CharacterAnim] " + m);

    [MenuItem("Tools/Character Resource/Rebuild Locomotion Clips")]
    public static void RebuildMenu() => Rebuild();

    /// <summary>클립 두 개와 컨트롤러를 다시 만든다. CLI 진입점.</summary>
    public static void Rebuild()
    {
        try
        {
            EnsureFolder(OutputDir);

            var walk = BuildWalk();
            var idle = BuildIdle();
            SaveClip(walk, WalkPath);
            SaveClip(idle, IdlePath);

            BuildController(idle, walk);

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
    /// 걷기 한 바퀴. 왼발과 오른발은 위상을 반대로 두고, 팔은 같은 쪽 다리와 반대로 흔든다.
    /// </summary>
    static AnimationClip BuildWalk()
    {
        var clip = new AnimationClip { frameRate = 30f };

        // 다리: 왼쪽 기준 위상 0, 오른쪽은 반 바퀴 뒤
        SetCurve(clip, LHip, t => HipSwing * Mathf.Sin(Tau(t)));
        SetCurve(clip, RHip, t => HipSwing * Mathf.Sin(Tau(t) + Mathf.PI));

        // 무릎: 한 바퀴에 한 번, 뒤로 찬 직후에 가장 많이 굽는다.
        SetCurve(clip, LKnee, t => Bend(Tau(t)));
        SetCurve(clip, RKnee, t => Bend(Tau(t) + Mathf.PI));

        // 발목: 허벅지보다 1/4 바퀴 늦게 따라온다.
        SetCurve(clip, LAnkle, t => AnkleSwing * Mathf.Sin(Tau(t) - Mathf.PI * 0.5f));
        SetCurve(clip, RAnkle, t => AnkleSwing * Mathf.Sin(Tau(t) + Mathf.PI * 0.5f));

        // 팔: 같은 쪽 다리와 반대로.
        SetCurve(clip, LArmFB, t => -ArmSwing * Mathf.Sin(Tau(t)));
        SetCurve(clip, RArmFB, t => ArmSwing * Mathf.Sin(Tau(t)));

        // 팔은 계속 내린 채로 유지 — 대기 자세와 같은 값이라 블렌드해도 튀지 않는다.
        SetConstant(clip, LArmDU, ArmRest);
        SetConstant(clip, RArmDU, ArmRest);
        SetConstant(clip, LElbow, ElbowRest);
        SetConstant(clip, RElbow, ElbowRest);
        SetConstant(clip, Spine, SpineLean);

        // 발을 지면에 올려 놓는다 — 없으면 허리까지 묻힌다.
        SetConstant(clip, RootY, RootHeight);

        MakeLooping(clip);
        return clip;
    }

    /// <summary>대기. 숨쉬기만 아주 약하게 넣는다.</summary>
    static AnimationClip BuildIdle()
    {
        var clip = new AnimationClip { frameRate = 30f };
        const float breathLen = 3.0f;

        SetConstant(clip, LArmDU, ArmRest, breathLen);
        SetConstant(clip, RArmDU, ArmRest, breathLen);
        SetConstant(clip, LElbow, ElbowRest, breathLen);
        SetConstant(clip, RElbow, ElbowRest, breathLen);
        SetConstant(clip, RootY, RootHeight, breathLen);

        // 숨쉬기: 3초에 한 번 아주 얕게
        var breath = new AnimationCurve();
        for (int i = 0; i <= 12; i++)
        {
            float t = i / 12f;
            breath.AddKey(t * breathLen, 0.03f * Mathf.Sin(t * Mathf.PI * 2f));
        }
        Smooth(breath);
        clip.SetCurve("", typeof(Animator), Spine, breath);

        MakeLooping(clip);
        return clip;
    }

    /// <summary>
    /// Speed(m/s) 하나로 대기↔걷기를 섞는 블렌드 트리.
    /// 상태 전이 대신 트리를 쓰면 가감속에서 끊기지 않는다.
    /// </summary>
    static void BuildController(AnimationClip idle, AnimationClip walk)
    {
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

        tree.AddChild(idle, 0f);
        tree.AddChild(walk, WalkSpeed);

        var layer = controller.layers[0];
        var state = layer.stateMachine.AddState("Locomotion");
        state.motion = tree;
        layer.stateMachine.defaultState = state;

        EditorUtility.SetDirty(controller);
        Log($"컨트롤러 생성: {ControllerPath} (Speed 0 → 대기, {WalkSpeed} → 걷기)");
    }

    // ── 도우미 ──

    static float Tau(float t) => t / CycleLength * Mathf.PI * 2f;

    /// <summary>
    /// 무릎 굽힘: 한 바퀴에 한 번만 솟는 곡선(0..-KneeBend).
    /// "Lower Leg Stretch" 는 <b>양수가 무릎을 편다</b>(발이 앞아래로 밀린다). 실측으로 확인했으니
    /// 굽히려면 음수여야 한다 — 부호를 뒤집으면 다리가 뻣뻣해지고 발이 땅을 뚫는다.
    /// </summary>
    static float Bend(float theta) =>
        -KneeBend * 0.5f * (1f - Mathf.Cos(theta - KneeBendPhase));

    static void SetCurve(AnimationClip clip, string muscle, Func<float, float> f)
    {
        var curve = new AnimationCurve();
        for (int i = 0; i <= SampleCount; i++)
        {
            float t = i / (float)SampleCount * CycleLength;
            curve.AddKey(t, f(t));
        }
        Smooth(curve);
        clip.SetCurve("", typeof(Animator), muscle, curve);
    }

    static void SetConstant(AnimationClip clip, string muscle, float v, float length = CycleLength)
    {
        var curve = AnimationCurve.Constant(0f, length, v);
        clip.SetCurve("", typeof(Animator), muscle, curve);
    }

    /// <summary>키 사이를 부드럽게. 끝점 접선을 맞춰 한 바퀴가 이어지게 한다.</summary>
    static void Smooth(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
            curve.SmoothTangents(i, 0f);
    }

    static void MakeLooping(AnimationClip clip)
    {
        var s = AnimationUtility.GetAnimationClipSettings(clip);
        s.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, s);
    }

    static void SaveClip(AnimationClip clip, string path)
    {
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        Log($"클립 저장: {path} (길이 {clip.length:F2}s, humanMotion={clip.humanMotion})");
    }

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

    /// <summary>디스크에서 다시 읽어 실제로 쓸 수 있는 상태인지 본다.</summary>
    static void Verify()
    {
        bool ok = true;

        foreach (var path in new[] { WalkPath, IdlePath })
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { Debug.LogError("[CharacterAnim] 클립 없음 " + path); ok = false; continue; }

            var bindings = AnimationUtility.GetCurveBindings(clip);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            Log($"검증 {System.IO.Path.GetFileName(path)}: 커브 {bindings.Length}개, " +
                $"humanMotion={clip.humanMotion}, loop={settings.loopTime}, 길이 {clip.length:F2}s");

            // humanMotion 이 false 면 근육 이름을 잘못 적은 것이다 — 조용히 아무 일도 안 일어난다.
            if (!clip.humanMotion) { Debug.LogError("[CharacterAnim] " + path + " 가 휴머노이드 클립이 아니다"); ok = false; }
            if (bindings.Length == 0) { Debug.LogError("[CharacterAnim] " + path + " 에 커브가 없다"); ok = false; }
        }

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ctrl == null) { Debug.LogError("[CharacterAnim] 컨트롤러 없음"); ok = false; }
        else Log($"검증 컨트롤러: 파라미터 {ctrl.parameters.Length}개, 상태 {ctrl.layers[0].stateMachine.states.Length}개");

        ok &= VerifyFeetOnGround();

        Log(ok ? "완료: 이상 없음" : "완료: 문제 있음");
        if (!ok && Application.isBatchMode) EditorApplication.Exit(1);
    }

    /// <summary>
    /// 실제 Animator 로 걷기를 한 바퀴 돌리며 발이 지면 근처에 머무는지 본다.
    /// RootT.y 를 빠뜨리면 허리까지 묻히는데, 클립 자체는 멀쩡해 보여서 이 검사로만 잡힌다.
    /// </summary>
    static bool VerifyFeetOnGround()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/Character2.prefab");
        if (prefab == null) { Log("검증 건너뜀: Character2 프리팹이 아직 없다"); return true; }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            root.transform.position = Vector3.zero;
            var animator = root.GetComponentInChildren<Animator>();
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                Log("검증 건너뜀: 프리팹에 Animator/컨트롤러가 아직 없다");
                return true;
            }

            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
            animator.SetFloat("Speed", WalkSpeed);
            animator.Update(0f);

            float lowest = float.MaxValue, highest = float.MinValue;
            const int Steps = 20;
            for (int i = 0; i < Steps; i++)
            {
                animator.Update(CycleLength / Steps);
                var lf = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                var rf = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                float low = Mathf.Min(lf.position.y, rf.position.y);
                lowest = Mathf.Min(lowest, low);
                highest = Mathf.Max(highest, low);
            }

            // 기본 자세의 발목 높이(실측 0.108m)를 기준으로 삼는다. 걷는 동안 무릎이 굽으므로
            // 위아래로 어느 정도는 움직이는 게 정상이고, 땅을 뚫는지만 본다.
            Log($"검증 접지: 걷기 한 바퀴 동안 발목 최저 {lowest:F3}m, 최고 {highest:F3}m");
            if (lowest < -0.1f)
            {
                Debug.LogError($"[CharacterAnim] 발이 지면 아래로 내려간다({lowest:F3}m). RootT.y({RootHeight}) 재보정 필요.");
                return false;
            }
            return true;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
        }
    }
}
