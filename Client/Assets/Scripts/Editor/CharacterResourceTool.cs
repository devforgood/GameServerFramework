using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Resources 의 액터 프리팹(Character2, Monster ...)에 실제 캐릭터 모델을 입히는 도구.
///
/// 런타임은 ActorSync.Create 에서 Resources.Load("Character2") / Resources.Load("Monster") 로
/// 이름을 찍어 부르기 때문에, 파일 이름만 유지하면 게임 코드는 건드릴 필요가 없다.
///
/// 만들어지는 프리팹 구조:
///   Character2                (루트: 게임플레이 스크립트 + CapsuleCollider)
///     └ Model                 (원본 캐릭터 프리팹 인스턴스 = 중첩 프리팹)
///
/// 모델을 루트에 합치지 않고 자식으로 두는 이유:
///   - 루트 Transform 을 서버 좌표 전용으로 깨끗하게 남긴다.
///   - 원본 에셋 팩이 갱신되면 중첩 프리팹을 타고 그대로 따라온다.
///
/// 에디터: Tools > Character Resource
/// CLI  : Unity.exe -batchmode -quit -nographics -projectPath &lt;Client&gt; -executeMethod CharacterResourceTool.ApplyAll
/// </summary>
public static class CharacterResourceTool
{
    /// <summary>Resources 프리팹 하나를 어떤 모델로 채울지에 대한 정의.</summary>
    class Binding
    {
        /// <summary>원본 캐릭터 프리팹 경로.</summary>
        public string Source;
        /// <summary>덮어쓸 Resources 프리팹 경로. 파일 이름이 Resources.Load 키가 된다.</summary>
        public string Target;
        /// <summary>루트에 붙일 게임플레이 MonoBehaviour 타입 이름 (Character, Monster ...).</summary>
        public string ScriptType;
        /// <summary>모델 Animator 에 물릴 AnimatorController. 없으면 비워 둔다.</summary>
        public string Controller;
    }

    const string PolytopeRoot = "Assets/Polytope Studio/Lowpoly_Characters";

    /// <summary>
    /// 캡슐 반지름의 상한을 키에 대한 비율로 정한다.
    /// 사람 체형은 키의 1/5 안쪽이라 0.2 면 몸통은 감싸고 망토는 흘린다.
    /// </summary>
    const float MaxRadiusRatio = 0.2f;

    /// <summary>
    /// 적용 표. 클라이언트는 몬스터 종류를 구분하지 않고 전부 "Monster" 하나로 그린다.
    /// </summary>
    static readonly Binding[] Bindings =
    {
        new Binding
        {
            Source     = PolytopeRoot + "/Prefabs/Modular_Armors/PT_Lowpoly_Armors_Male_Moduar_Free.prefab",
            Target     = "Assets/Resources/Character2.prefab",
            ScriptType = "Character",
            // 팩에 딸린 컨트롤러는 정지 포즈 하나뿐이라, 코드로 만든 로코모션을 쓴다.
            Controller = CharacterAnimationTool.ControllerPath,
        },
        new Binding
        {
            Source     = MonsterAnimationTool.ModelPrefab,
            Target     = MonsterAnimationTool.PrefabPath,
            ScriptType = "Monster",
            Controller = MonsterAnimationTool.ControllerPath,
        },
    };

    static void Log(string m) => Debug.Log("[CharacterResource] " + m);
    static void Warn(string m) => Debug.LogWarning("[CharacterResource] " + m);

    [MenuItem("Tools/Character Resource/Apply All")]
    public static void ApplyAllMenu() => ApplyAll();

    /// <summary>표에 정의된 모든 프리팹을 다시 만든다. CLI 진입점.</summary>
    public static void ApplyAll()
    {
        int ok = 0, fail = 0;
        foreach (var b in Bindings)
        {
            if (Apply(b)) ok++;
            else fail++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Log($"완료: 성공 {ok}, 실패 {fail}");

        // 배치모드에서 실패를 종료 코드로 알린다.
        if (fail > 0 && Application.isBatchMode)
            EditorApplication.Exit(1);
    }

    static bool Apply(Binding b)
    {
        GameObject root = null;
        try
        {
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(b.Source);
            if (source == null)
            {
                Debug.LogError("[CharacterResource] 원본 프리팹을 찾을 수 없음: " + b.Source);
                return false;
            }

            var targetName = System.IO.Path.GetFileNameWithoutExtension(b.Target);

            // 1. 루트 생성 — 이름이 곧 Resources.Load 키라 반드시 맞춰야 한다.
            root = new GameObject(targetName);

            // 2. 모델을 자식으로. 중첩 프리팹으로 붙여 원본 갱신을 따라가게 한다.
            var model = (GameObject)PrefabUtility.InstantiatePrefab(source);
            model.name = "Model";
            model.transform.SetParent(root.transform, false);
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;

            FixPipelineMaterials(model, targetName);

            // 3. 애니메이터 컨트롤러 연결. 없으면 T 포즈로 서 있게 된다.
            var animator = model.GetComponent<Animator>();
            if (animator == null)
                Warn($"{targetName}: 모델에 Animator 가 없다.");
            else if (!string.IsNullOrEmpty(b.Controller))
            {
                var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(b.Controller);
                if (ctrl == null) Warn($"{targetName}: 컨트롤러를 찾을 수 없음 {b.Controller}");
                else
                {
                    animator.runtimeAnimatorController = ctrl;
                    // 서버가 위치를 주므로 루트 모션은 꺼야 제자리에서 논다.
                    animator.applyRootMotion = false;
                    Log($"{targetName}: 컨트롤러 {ctrl.name} 연결, applyRootMotion=false");
                }
            }

            // 4. 실제 렌더러 경계로 콜라이더를 맞춘다.
            var bounds = CalcLocalBounds(root);
            var cc = root.AddComponent<CapsuleCollider>();
            cc.direction = 1; // Y축
            cc.height = Mathf.Max(bounds.size.y, 0.1f);

            // 가로 경계를 그대로 쓰면 망토·무기 슬롯 같은 돌출물까지 반지름에 잡혀
            // 사람 몸통의 두 배가 된다. 키에 대한 비율로 상한을 둔다.
            var rawRadius = Mathf.Max(bounds.extents.x, bounds.extents.z);
            cc.radius = Mathf.Clamp(rawRadius, 0.05f, cc.height * MaxRadiusRatio);

            // 모델 피벗은 발바닥이므로 캡슐 밑면을 지면(y=0)에 맞춘다.
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);

            Log($"{targetName}: 모델 높이 {bounds.size.y:F2}m, 콜라이더 height={cc.height:F2} " +
                $"radius={cc.radius:F2}(원본 경계 {rawRadius:F2}) centerY={cc.center.y:F2}");

            // 5. 게임플레이 스크립트 부착. 어셈블리 이름이 바뀌어도 되게 타입 이름으로 찾는다.
            var type = FindMonoBehaviour(b.ScriptType);
            if (type == null)
            {
                Debug.LogError($"[CharacterResource] 스크립트 타입을 찾을 수 없음: {b.ScriptType}");
                return false;
            }
            root.AddComponent(type);

            // 6. 발 미끄러짐 보정값을 새긴다.
            // 런타임이 컨트롤러를 뜯어볼 수는 없으니, 여기서 블렌드 트리의 가장 높은 문턱값
            // (= 가장 빠른 클립의 보행 속도)을 읽어 Actor 에 넣어 준다.
            var actor = root.GetComponent<Actor>();
            if (actor != null)
            {
                float top = TopBlendThreshold(b.Controller);
                if (top > 0f)
                {
                    actor.locomotionTopSpeed = top;
                    Log($"{targetName}: 클립 최고 보행 속도 {top:F2} m/s 를 프리팹에 기록");
                }
                else Warn($"{targetName}: 블렌드 문턱값을 읽지 못해 기본값을 그대로 둔다");
            }

            // 7. 저장.
            PrefabUtility.SaveAsPrefabAsset(root, b.Target, out bool saved);
            if (!saved)
            {
                Debug.LogError("[CharacterResource] 프리팹 저장 실패: " + b.Target);
                return false;
            }

            Log($"{targetName}: 저장 완료 -> {b.Target}");
            return Verify(b.Target, b.ScriptType);
        }
        catch (Exception e)
        {
            Debug.LogError("[CharacterResource] 예외: " + e);
            return false;
        }
        finally
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
        }
    }

    /// <summary>
    /// 이 프로젝트는 Built-in 렌더 파이프라인이다. URP 로 만들어진 팩(Dungeon Skeletons 등)은
    /// 머티리얼 셰이더가 없는 URP Lit 을 가리켜 분홍색으로 그려진다.
    /// 셰이더를 잃은 머티리얼만 Standard 로 바꾸고 텍스처·색은 옮겨 준다.
    /// 팩 머티리얼 에셋 자체를 고치므로 한 번 바꾸면 이후엔 건드리지 않는다.
    /// </summary>
    static void FixPipelineMaterials(GameObject model, string targetName)
    {
        var standard = Shader.Find("Standard");
        if (standard == null) { Warn("Standard 셰이더를 찾지 못해 머티리얼 교정을 건너뛴다"); return; }

        var mats = model.GetComponentsInChildren<Renderer>(true)
            .SelectMany(r => r.sharedMaterials)
            .Where(m => m != null)
            .Distinct();

        foreach (var mat in mats)
        {
            bool broken = mat.shader == null || mat.shader.name == "Hidden/InternalErrorShader";
            if (!broken) continue;

            // URP 는 _BaseMap/_BaseColor, Standard 는 _MainTex/_Color 를 읽는다.
            var tex = mat.HasProperty("_BaseMap") ? mat.GetTexture("_BaseMap") : null;
            if (tex == null && mat.HasProperty("_MainTex")) tex = mat.GetTexture("_MainTex");
            var color = mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : Color.white;
            float smooth = mat.HasProperty("_Smoothness") ? mat.GetFloat("_Smoothness") : 0.2f;

            mat.shader = standard;
            if (tex != null) mat.SetTexture("_MainTex", tex);
            mat.SetColor("_Color", color);
            mat.SetFloat("_Glossiness", smooth);
            mat.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(mat);
            Log($"{targetName}: 머티리얼 {mat.name} 셰이더를 Standard 로 교정(텍스처 {(tex != null ? tex.name : "없음")})");
        }
    }

    /// <summary>루트 기준 로컬 공간에서 모든 렌더러를 감싸는 경계를 구한다.</summary>
    static Bounds CalcLocalBounds(GameObject root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return new Bounds(Vector3.zero, Vector3.one);

        // 루트를 원점/무회전으로 만들어 두었으므로 월드 경계가 곧 로컬 경계다.
        root.transform.position = Vector3.zero;
        root.transform.rotation = Quaternion.identity;

        var b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    /// <summary>
    /// 컨트롤러 블렌드 트리에서 가장 높은 문턱값을 찾는다.
    /// 문턱값은 그 클립이 실제로 걷는 속도(m/s)라 발 미끄러짐 보정의 기준이 된다.
    /// </summary>
    static float TopBlendThreshold(string controllerPath)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
        if (ctrl == null || ctrl.layers.Length == 0) return 0f;

        float top = 0f;
        foreach (var state in ctrl.layers[0].stateMachine.states)
        {
            if (!(state.state.motion is UnityEditor.Animations.BlendTree tree)) continue;
            foreach (var child in tree.children)
                top = Mathf.Max(top, child.threshold);
        }
        return top;
    }

    static Type FindMonoBehaviour(string name) =>
        AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => { try { return a.GetTypes(); } catch { return Type.EmptyTypes; } })
            .FirstOrDefault(t => t.Name == name && typeof(MonoBehaviour).IsAssignableFrom(t));

    /// <summary>디스크에서 다시 읽어 실제로 쓸 수 있는 프리팹인지 확인한다.</summary>
    static bool Verify(string path, string scriptType)
    {
        var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null)
        {
            Debug.LogError("[CharacterResource] 검증 실패: 프리팹을 읽을 수 없음 " + path);
            return false;
        }

        var comps = string.Join("|", go.GetComponents<Component>()
            .Select(c => c == null ? "MISSING" : c.GetType().Name));

        var renderers = go.GetComponentsInChildren<Renderer>(true);
        var skinned = renderers.OfType<SkinnedMeshRenderer>().Count();

        // ActorSync.Create 가 이 GetComponent 로 액터를 집는다. 여기서 null 이면 런타임에 조용히 깨진다.
        var actor = go.GetComponent(scriptType);

        Log($"검증 {System.IO.Path.GetFileName(path)}: 루트=[{comps}] 렌더러={renderers.Length}(스킨드 {skinned}) {scriptType}={(actor != null ? "OK" : "없음")}");

        if (actor == null)
        {
            Debug.LogError($"[CharacterResource] 검증 실패: 루트에 {scriptType} 없음");
            return false;
        }
        if (renderers.Length == 0)
        {
            Debug.LogError("[CharacterResource] 검증 실패: 렌더러가 하나도 없음");
            return false;
        }
        return true;
    }
}
