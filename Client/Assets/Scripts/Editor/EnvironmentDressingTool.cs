using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// 맵 씬에 Flooded_Grounds 배경을 입힌다.
///
/// 게임 지오메트리(바닥·장애물·경사로)와 navmesh 는 **그대로** 둔다. 서버 이동 판정이 그것으로
/// 구워져 있기 때문이다. 대신 그 렌더러만 숨기고, 같은 자리에 보기 좋은 껍데기를 덧입힌다.
///
///   바닥      → Terrain. 플레이 영역 안은 높이 정확히 0, 바깥은 북쪽 언덕 / 남쪽 침수지
///   장애물    → 바위 무더기(원래 큐브 크기에 맞춤)
///   경사로    → 나무 판자 길
///   경계      → 울타리(게이트 자리는 문과 바깥으로 이어지는 길)
///   바깥      → 나무·건물·묘지·물·원경, 스카이박스·안개·조명
///
/// 카메라는 남쪽에서 북쪽을 내려다본다(DiabloCamera yaw 0). 그래서 키 큰 것(나무·건물)은 북·동·서에만
/// 두고 남쪽은 물과 낮은 것만 둔다. 남쪽에 나무를 세우면 캐릭터가 가장자리에 설 때 화면을 가린다.
///
/// 결과물은 전부 씬 루트 "Environment"(EnvironmentDressing) 밑과 Assets/Environment/{씬} 에 모인다.
/// 다시 실행하면 통째로 새로 만든다 — 손으로 고친 배치는 덮어써지니 바꿀 것은 이 코드에 반영하라.
///
/// CLI: -executeMethod EnvironmentDressingTool.BuildStartingVillage [-previewDir 경로]
///      (-nographics 를 빼야 미리보기 PNG 가 나온다)
/// </summary>
public static class EnvironmentDressingTool
{
    public const string StartingVillageScene = "Assets/Scenes/GameField/Starting Village.unity";

    private const string RootName = "Environment";
    private const string OutputFolder = "Assets/Environment";
    private const string Pack = "Assets/Flooded_Grounds";
    private const string Prefabs = Pack + "/Prefabs";
    private const string SourceTerrain = Pack + "/Scenes/Scene_A_Terrain.asset";

    // ── 지형 치수 ──
    private const float TerrainSize = 240f;
    private const int HeightmapResolution = 257;   // 약 0.94 m 간격
    private const float TerrainBaseY = -6f;         // 침수지가 물 아래로 내려갈 여유
    private const float TerrainHeight = 36f;
    private const float FlatMargin = 3f;            // 플레이 영역 밖으로 높이 0 을 더 이어 가는 폭
    private const float RiseDistance = 14f;         // 평지에서 언덕 최고 높이까지 올라가는 거리
    private const float WaterY = -1.2f;

    // Scene_A 지형 레이어 순서(Moss, Dirt, Asphalt)
    private const int LayerMoss = 0;
    private const int LayerDirt = 1;

    [MenuItem("Tools/Environment/Dress Starting Village")]
    public static void DressStartingVillageMenu()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;
        var scene = EditorSceneManager.OpenScene(StartingVillageScene, OpenSceneMode.Single);
        if (Dress(scene, out string report))
            EditorSceneManager.SaveScene(scene);
        Debug.Log("[EnvDressing]\n" + report);
    }

    /// <summary>CLI: 꾸미기 → 저장 → 검증 → 미리보기. 실패하면 종료 코드 1.</summary>
    public static void BuildStartingVillage()
    {
        bool ok = false;
        try
        {
            var scene = EditorSceneManager.OpenScene(StartingVillageScene, OpenSceneMode.Single);
            ok = Dress(scene, out string report);
            Debug.Log("[EnvDressing]\n" + report);
            if (ok)
            {
                EditorSceneManager.SaveScene(scene);
                AssetDatabase.SaveAssets();

                string previewDir = CommandLineValue("-previewDir");
                // -nographics 로 띄우면 그래픽 장치가 없어 렌더할 수 없다.
                if (previewDir != null && SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null)
                    RenderPreviews(scene, previewDir);
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

    // ────────────────────────────────────────────────────────────────────
    // 씬 분석
    // ────────────────────────────────────────────────────────────────────

    /// <summary>게임 지오메트리에서 읽어 낸 배치. 배경은 전부 이것을 기준으로 놓는다.</summary>
    private class Layout
    {
        public Vector3 center;          // 플레이 영역 중심(바닥 높이)
        public Vector3 half;            // 플레이 영역 반폭(x, z. y 는 0)
        public MeshFilter ground;
        public List<MeshFilter> obstacles = new List<MeshFilter>();
        public List<MeshFilter> ramps = new List<MeshFilter>();
        public List<Vector3> gates = new List<Vector3>();

        public float MinX => center.x - half.x;
        public float MaxX => center.x + half.x;
        public float MinZ => center.z - half.z;
        public float MaxZ => center.z + half.z;
    }

    private static Layout Analyze(out string error)
    {
        error = null;
        // 게이트·스폰 같은 마커 프리팹에도 메시가 있다. 그것들은 지형이 아니니 건드리지 않는다.
        var meshes = MapPipeline.BakeableMeshes()
            .Where(m => m.GetComponentInParent<Gate>(true) == null
                        && m.GetComponentInParent<SpawnPoint>(true) == null
                        && m.GetComponentInParent<MapObjectMarker>(true) == null)
            .ToArray();
        if (meshes.Length == 0)
        {
            error = "게임 지오메트리(MeshFilter)가 없습니다.";
            return null;
        }

        var layout = new Layout();

        // 가장 넓은 메시가 바닥이다.
        layout.ground = meshes
            .Where(m => m.GetComponent<Renderer>() != null)
            .OrderByDescending(m => { var b = m.GetComponent<Renderer>().bounds; return b.size.x * b.size.z; })
            .First();
        Bounds gb = layout.ground.GetComponent<Renderer>().bounds;
        layout.center = new Vector3(gb.center.x, gb.max.y, gb.center.z);
        layout.half = new Vector3(gb.extents.x, 0f, gb.extents.z);

        foreach (var mf in meshes)
        {
            if (mf == layout.ground)
                continue;
            if (mf.sharedMesh.name == "Cube")
                layout.obstacles.Add(mf);
            else
                layout.ramps.Add(mf);
        }

        foreach (var gate in Object.FindObjectsByType<Gate>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            layout.gates.Add(gate.transform.position);

        return layout;
    }

    // ────────────────────────────────────────────────────────────────────
    // 꾸미기
    // ────────────────────────────────────────────────────────────────────

    public static bool Dress(Scene scene, out string report)
    {
        var log = new System.Text.StringBuilder();
        report = null;

        // 이전 결과를 먼저 지운다. 분석(BakeableMeshes)은 배경을 빼고 보지만, 깨끗한 상태에서 세는 편이 검증이 정확하다.
        foreach (var old in scene.GetRootGameObjects().Where(g => g.GetComponent<EnvironmentDressing>() != null || g.name == RootName))
            Object.DestroyImmediate(old);

        int bakeInputBefore = MapPipeline.BakeableMeshes().Length;
        Bounds? boundsBefore = MapPipeline.SceneBounds();

        Layout layout = Analyze(out string error);
        if (layout == null)
        {
            report = error;
            return false;
        }
        log.AppendLine($"플레이 영역 중심 {layout.center} 반폭 {layout.half}, 장애물 {layout.obstacles.Count}, 경사로 {layout.ramps.Count}, 게이트 {layout.gates.Count}");

        var rng = new System.Random(StableHash(scene.name));
        var noiseOffset = new Vector2(rng.Next(1000), rng.Next(1000));

        var root = new GameObject(RootName);
        root.AddComponent<EnvironmentDressing>();
        SceneManager.MoveGameObjectToScene(root, scene);

        var shape = new TerrainShape(layout, noiseOffset);

        // 건물 자리는 지형을 먼저 평평하게 다져야 하므로 배치 계획을 지형보다 먼저 세운다.
        var buildings = PlanBuildings(layout, shape);
        foreach (var b in buildings)
            shape.AddPad(b.position, b.padRadius);

        Terrain terrain = BuildTerrain(scene.name, layout, shape, root.transform, rng, log);
        if (terrain == null)
        {
            report = log + "지형 생성 실패";
            return false;
        }

        BuildWater(scene.name, layout, root.transform);
        HideGameplayRenderers(layout);
        DressObstacles(layout, root.transform, rng);
        DressRamps(layout, root.transform, rng);
        BuildFences(layout, root.transform, rng);
        PlaceBuildings(buildings, terrain, root.transform);
        ScatterOutskirts(layout, shape, terrain, root.transform, rng);
        ApplyAtmosphere(scene);

        int disabledColliders = DisableColliders(root);
        log.AppendLine($"배경 오브젝트 {root.GetComponentsInChildren<Transform>(true).Length}개, 콜라이더 {disabledColliders}개 끔");

        // ── 검증: 배경이 게임 판정에 끼어들지 않았는지 ──
        bool ok = true;
        int bakeInputAfter = MapPipeline.BakeableMeshes().Length;
        if (bakeInputAfter != bakeInputBefore)
        {
            log.AppendLine($"실패: NavMesh 입력 메시 수가 바뀜 {bakeInputBefore} → {bakeInputAfter}");
            ok = false;
        }
        Bounds? boundsAfter = MapPipeline.SceneBounds();
        // Map.json 크기는 x·z 만 쓴다(높이는 꺼 둔 NavMeshVisualizer 폴리곤만큼 줄어드는 게 정상).
        if (boundsBefore.HasValue && boundsAfter.HasValue &&
            (Mathf.Abs(boundsBefore.Value.size.x - boundsAfter.Value.size.x) > 0.01f ||
             Mathf.Abs(boundsBefore.Value.size.z - boundsAfter.Value.size.z) > 0.01f))
        {
            log.AppendLine($"실패: 맵 크기 계산이 바뀜 {boundsBefore.Value.size} → {boundsAfter.Value.size}");
            ok = false;
        }
        int activeColliders = root.GetComponentsInChildren<Collider>(true).Count(c => c.enabled);
        if (activeColliders > 0)
        {
            log.AppendLine($"실패: 켜진 콜라이더 {activeColliders}개(클릭 이동을 가로챈다)");
            ok = false;
        }
        float cornerHeight = terrain.SampleHeight(new Vector3(layout.MaxX, 0, layout.MaxZ)) + terrain.transform.position.y;
        float centerHeight = terrain.SampleHeight(layout.center) + terrain.transform.position.y;
        if (Mathf.Abs(cornerHeight - layout.center.y) > 0.02f || Mathf.Abs(centerHeight - layout.center.y) > 0.02f)
        {
            log.AppendLine($"실패: 플레이 영역 지형 높이가 바닥과 다름(중심 {centerHeight:F3}, 모서리 {cornerHeight:F3}, 바닥 {layout.center.y:F3})");
            ok = false;
        }
        log.AppendLine($"검증: NavMesh 입력 {bakeInputAfter}개 유지, 플레이 영역 지형 높이 중심 {centerHeight:F3} / 모서리 {cornerHeight:F3}");

        EditorSceneManager.MarkSceneDirty(scene);
        report = log.ToString();
        return ok;
    }

    // ────────────────────────────────────────────────────────────────────
    // 지형
    // ────────────────────────────────────────────────────────────────────

    /// <summary>월드 좌표 → 지형 높이. 지형·나무·소품이 모두 같은 함수를 본다.</summary>
    private class TerrainShape
    {
        private readonly Layout layout;
        private readonly Vector2 noise;
        private readonly List<(Vector3 center, float radius, float height)> pads = new List<(Vector3, float, float)>();

        public TerrainShape(Layout layout, Vector2 noise)
        {
            this.layout = layout;
            this.noise = noise;
        }

        /// <summary>플레이 영역 가장자리에서 바깥으로 떨어진 거리(안쪽은 0 이하).</summary>
        public float Outside(float x, float z)
        {
            float dx = Mathf.Abs(x - layout.center.x) - layout.half.x;
            float dz = Mathf.Abs(z - layout.center.z) - layout.half.z;
            if (dx > 0 && dz > 0)
                return Mathf.Sqrt(dx * dx + dz * dz);
            return Mathf.Max(dx, dz);
        }

        /// <summary>1 = 북쪽(화면 위), 0 = 남쪽(카메라 쪽).</summary>
        public float Northness(float x, float z)
        {
            var d = new Vector2(x - layout.center.x, z - layout.center.z);
            return d.sqrMagnitude < 1f ? 0.5f : Mathf.Clamp01(0.5f + 0.5f * d.y / d.magnitude);
        }

        /// <summary>게이트에서 바깥으로 뻗는 길 위일수록 1.</summary>
        public float Road(float x, float z)
        {
            float best = 0f;
            foreach (var gate in layout.gates)
            {
                // 게이트가 붙은 변의 바깥 방향으로만 길을 낸다.
                float gx = gate.x - layout.center.x, gz = gate.z - layout.center.z;
                bool eastWest = Mathf.Abs(gx) / layout.half.x > Mathf.Abs(gz) / layout.half.z;
                float along = eastWest ? (x - gate.x) * Mathf.Sign(gx) : (z - gate.z) * Mathf.Sign(gz);
                float across = eastWest ? Mathf.Abs(z - gate.z) : Mathf.Abs(x - gate.x);
                if (along < 0f)
                    continue;
                best = Mathf.Max(best, 1f - Smooth01((across - 2.5f) / 4f));
            }
            return best;
        }

        public float RawHeight(float x, float z)
        {
            float outside = Outside(x, z);
            if (outside <= FlatMargin)
                return layout.center.y;

            float t = Smooth01((outside - FlatMargin) / RiseDistance);
            float north = Northness(x, z);

            // 북쪽은 언덕, 남쪽은 물에 잠긴 저지대. 동서는 그 중간.
            float trend = Mathf.Lerp(-3.6f, 7.5f, north * north);
            float broad = (Mathf.PerlinNoise(noise.x + x * 0.018f, noise.y + z * 0.018f) - 0.5f) * 6f;
            float fine = (Mathf.PerlinNoise(noise.y + x * 0.07f, noise.x + z * 0.07f) - 0.5f) * 1.6f;
            float h = t * (trend + broad) + Smooth01((outside - FlatMargin) / 4f) * fine;

            // 게이트 길은 바닥 높이로 곧게 뻗는다(물 위에서는 둑길).
            h = Mathf.Lerp(h, 0.05f, Road(x, z));
            return layout.center.y + h;
        }

        public void AddPad(Vector3 center, float radius)
        {
            float h = Mathf.Max(RawHeight(center.x, center.z), WaterY + 0.6f);
            pads.Add((center, radius, h));
        }

        public float Height(float x, float z)
        {
            float h = RawHeight(x, z);
            foreach (var pad in pads)
            {
                float d = new Vector2(x - pad.center.x, z - pad.center.z).magnitude;
                float w = 1f - Smooth01((d - pad.radius) / 6f);
                h = Mathf.Lerp(h, pad.height, w);
            }
            return h;
        }
    }

    private static Terrain BuildTerrain(string sceneName, Layout layout, TerrainShape shape, Transform parent,
                                        System.Random rng, System.Text.StringBuilder log)
    {
        var source = AssetDatabase.LoadAssetAtPath<TerrainData>(SourceTerrain);
        if (source == null)
        {
            log.AppendLine($"원본 지형을 찾지 못했습니다: {SourceTerrain}");
            return null;
        }

        string folder = $"{OutputFolder}/{sceneName}";
        EnsureFolder(folder);
        string dataPath = $"{folder}/{sceneName}_Terrain.asset";
        AssetDatabase.DeleteAsset(dataPath);

        var data = new TerrainData
        {
            heightmapResolution = HeightmapResolution,
            alphamapResolution = 256,
            baseMapResolution = 512,
        };
        data.size = new Vector3(TerrainSize, TerrainHeight, TerrainSize);
        data.terrainLayers = source.terrainLayers;
        data.treePrototypes = source.treePrototypes;
        data.detailPrototypes = source.detailPrototypes;
        data.SetDetailScatterMode(source.detailScatterMode);
        data.SetDetailResolution(512, 32);
        AssetDatabase.CreateAsset(data, dataPath);

        Vector3 origin = new Vector3(layout.center.x - TerrainSize / 2f, TerrainBaseY, layout.center.z - TerrainSize / 2f);
        Func<int, int, int, Vector2> cellToWorld = (col, row, res) =>
            new Vector2(origin.x + col / (float)(res - 1) * TerrainSize, origin.z + row / (float)(res - 1) * TerrainSize);

        // 높이
        int hr = data.heightmapResolution;
        var heights = new float[hr, hr];
        for (int row = 0; row < hr; row++)
        for (int col = 0; col < hr; col++)
        {
            Vector2 w = cellToWorld(col, row, hr);
            heights[row, col] = Mathf.Clamp01((shape.Height(w.x, w.y) - TerrainBaseY) / TerrainHeight);
        }
        data.SetHeights(0, 0, heights);

        // 텍스처: 플레이 영역은 이끼 낀 흙, 가장자리·길·물가는 흙, 언덕은 이끼
        int ar = data.alphamapResolution;
        int layers = data.terrainLayers.Length;
        var alphas = new float[ar, ar, layers];
        for (int row = 0; row < ar; row++)
        for (int col = 0; col < ar; col++)
        {
            Vector2 w = cellToWorld(col, row, ar);
            float outside = shape.Outside(w.x, w.y);
            float h = shape.Height(w.x, w.y);
            float patches = Mathf.PerlinNoise(w.x * 0.09f + 31f, w.y * 0.09f + 17f);

            float dirt;
            if (outside <= 0f)
                dirt = Mathf.Lerp(0.25f, 0.85f, Smooth01((patches - 0.35f) / 0.35f));
            else
                dirt = Mathf.Max(1f - Smooth01((outside - 1f) / 4f), Smooth01((patches - 0.62f) / 0.2f) * 0.6f);
            dirt = Mathf.Max(dirt, shape.Road(w.x, w.y));
            dirt = Mathf.Max(dirt, 1f - Smooth01((h - WaterY) / 0.8f));   // 물가 진흙

            alphas[row, col, LayerMoss] = 1f - dirt;
            alphas[row, col, LayerDirt] = dirt;
        }
        data.SetAlphamaps(0, 0, alphas);

        var terrainObject = Terrain.CreateTerrainGameObject(data);
        terrainObject.name = "Terrain";
        Object.DestroyImmediate(terrainObject.GetComponent<TerrainCollider>());
        terrainObject.transform.SetParent(parent, false);
        terrainObject.transform.position = origin;
        var terrain = terrainObject.GetComponent<Terrain>();
        terrain.heightmapPixelError = 3f;
        terrain.drawInstanced = true;
        terrain.basemapDistance = 250f;
        terrain.detailObjectDistance = 70f;
        terrain.treeDistance = 400f;
        terrain.treeBillboardDistance = 120f;

        int trees = PlantTrees(data, layout, shape, origin, rng);
        int grass = PaintGrass(data, layout, shape, origin, rng);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();

        log.AppendLine($"지형 {dataPath}: 나무 {trees}그루, 풀 칸 {grass}개 (원본 풀 종류 {data.detailPrototypes.Length}, 모드 {data.detailScatterMode})");
        return terrain;
    }

    private static int PlantTrees(TerrainData data, Layout layout, TerrainShape shape, Vector3 origin, System.Random rng)
    {
        // 원본 프로토타입의 실제 키. 팩 나무는 40 m 가 넘어 탑뷰 규모에 맞게 줄여 심는다.
        var prototypes = data.treePrototypes;
        var nativeHeights = prototypes.Select(p => Mathf.Max(1f, PrefabBounds(p.prefab).size.y)).ToArray();
        int deadTree = Array.FindIndex(prototypes, p => p.prefab != null && p.prefab.name.Contains("Dead"));

        var placed = new List<Vector2>();
        var instances = new List<TreeInstance>();
        const float spacing = 5f;

        for (int attempt = 0; attempt < 6000 && instances.Count < 420; attempt++)
        {
            float x = origin.x + (float)rng.NextDouble() * TerrainSize;
            float z = origin.z + (float)rng.NextDouble() * TerrainSize;
            float outside = shape.Outside(x, z);
            if (outside < 8f)
                continue;

            float h = shape.Height(x, z);
            if (h < WaterY + 0.25f || shape.Road(x, z) > 0.05f)
                continue;

            // 남쪽(카메라 쪽)은 가까이에 키 큰 나무를 두지 않는다.
            bool south = z < layout.MinZ;
            if (south && outside < 32f)
                continue;

            float north = shape.Northness(x, z);
            if (rng.NextDouble() > 0.3 + 0.7 * north)
                continue;
            if (placed.Any(p => (p - new Vector2(x, z)).sqrMagnitude < spacing * spacing))
                continue;

            int proto = rng.Next(prototypes.Length);
            float targetHeight = proto == deadTree ? Lerp(rng, 7f, 11f) : Lerp(rng, 9f, 16f);
            float scale = targetHeight / nativeHeights[proto];

            placed.Add(new Vector2(x, z));
            instances.Add(new TreeInstance
            {
                prototypeIndex = proto,
                position = new Vector3((x - origin.x) / TerrainSize, 0f, (z - origin.z) / TerrainSize),
                heightScale = scale,
                widthScale = scale * Lerp(rng, 0.85f, 1.15f),
                rotation = Lerp(rng, 0f, Mathf.PI * 2f),
                color = Color.Lerp(Color.white, new Color(0.8f, 0.85f, 0.75f), (float)rng.NextDouble()),
                lightmapColor = Color.white,
            });
        }

        data.SetTreeInstances(instances.ToArray(), true);
        return instances.Count;
    }

    private static int PaintGrass(TerrainData data, Layout layout, TerrainShape shape, Vector3 origin, System.Random rng)
    {
        int res = data.detailResolution;
        int types = data.detailPrototypes.Length;
        if (types == 0)
            return 0;

        bool coverage = data.detailScatterMode == DetailScatterMode.CoverageMode;
        int painted = 0;
        var layers = new int[types][,];
        for (int i = 0; i < types; i++)
            layers[i] = new int[res, res];

        for (int row = 0; row < res; row++)
        for (int col = 0; col < res; col++)
        {
            float x = origin.x + (col + 0.5f) / res * TerrainSize;
            float z = origin.z + (row + 0.5f) / res * TerrainSize;
            float h = shape.Height(x, z);
            if (h < WaterY + 0.05f)
                continue;

            float outside = shape.Outside(x, z);
            float clump = Mathf.PerlinNoise(x * 0.12f + 5f, z * 0.12f + 9f);

            // 플레이 영역은 드문드문(몬스터·이펙트가 묻히지 않게), 바깥은 무성하게.
            float threshold = outside <= 0f ? 0.68f : 0.42f;
            if (shape.Road(x, z) > 0.5f)
                threshold = 0.8f;
            if (clump < threshold)
                continue;

            int type = rng.Next(types);
            int amount = coverage ? (outside <= 0f ? 90 : 200) : (outside <= 0f ? 1 : 1 + rng.Next(2));
            layers[type][row, col] = amount;
            painted++;
        }

        for (int i = 0; i < types; i++)
            data.SetDetailLayer(0, 0, i, layers[i]);
        return painted;
    }

    private static void BuildWater(string sceneName, Layout layout, Transform parent)
    {
        // FG_PBR_Water 는 파도를 오브젝트 공간 높이(v.vertex.y = sin * 0.1)로 만든다. 기본 Plane 을 키워 쓰면
        // 파도도 같은 배율로 커져(×43 이면 ±4 m) 물이 플레이 영역 위로 솟는다. 그래서 실제 크기 메시를 스케일 1 로 쓴다.
        const float size = TerrainSize * 1.8f;
        const int cells = 48;
        var vertices = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        for (int z = 0; z <= cells; z++)
        for (int x = 0; x <= cells; x++)
        {
            var p = new Vector3((x / (float)cells - 0.5f) * size, 0f, (z / (float)cells - 0.5f) * size);
            vertices.Add(p);
            uvs.Add(new Vector2(p.x, p.z) / 1280f);   // Scene_A 물(1280 m)과 같은 텍스처 밀도
        }
        for (int z = 0; z < cells; z++)
        for (int x = 0; x < cells; x++)
        {
            int i = z * (cells + 1) + x;
            triangles.AddRange(new[] { i, i + cells + 1, i + 1, i + 1, i + cells + 1, i + cells + 2 });
        }
        var mesh = new Mesh { name = "Water" };
        mesh.SetVertices(vertices);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateNormals();
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();

        string meshPath = $"{OutputFolder}/{sceneName}/{sceneName}_Water.asset";
        AssetDatabase.DeleteAsset(meshPath);
        AssetDatabase.CreateAsset(mesh, meshPath);

        var water = new GameObject("Water");
        water.transform.SetParent(parent, false);
        water.transform.position = new Vector3(layout.center.x, WaterY, layout.center.z);
        water.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = water.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{Pack}/Content/Materials/BGR_Water.mat");
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    // ────────────────────────────────────────────────────────────────────
    // 게임 지오메트리 덧입히기
    // ────────────────────────────────────────────────────────────────────

    private static void HideGameplayRenderers(Layout layout)
    {
        // MeshFilter·콜라이더는 남긴다: 앞은 NavMesh 굽기 입력, 뒤는 클릭 이동 레이캐스트가 쓴다.
        foreach (var mf in layout.obstacles.Concat(layout.ramps).Append(layout.ground))
        {
            var r = mf.GetComponent<Renderer>();
            if (r != null)
                r.enabled = false;
        }

        // 지난 굽기 결과를 반투명 폴리곤으로 덮어 두던 시각화. 배경을 가린다.
        foreach (var viz in Object.FindObjectsByType<RecastNavigation.Unity.NavMeshVisualizer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            viz.gameObject.SetActive(false);
    }

    // Rock_A 는 세로로 긴 판석이라 탑뷰에서 비석처럼 보여 뺐다.
    private static readonly string[] ObstacleRocks =
    {
        Prefabs + "/Nature/Rocks/CobbleRock_A.prefab",
        Prefabs + "/Nature/Rocks/CobbleRock_E.prefab",
    };

    private static readonly string[] Pebbles =
    {
        Prefabs + "/Nature/Rocks/CobbleRock_C.prefab",
        Prefabs + "/Nature/Rocks/CobbleRock_D.prefab",
        Prefabs + "/Nature/Rocks/CobbleRock_F.prefab",
    };

    private static void DressObstacles(Layout layout, Transform root, System.Random rng)
    {
        var group = Group(root, "Obstacles");
        foreach (var obstacle in layout.obstacles)
        {
            Bounds b = obstacle.GetComponent<Renderer>().bounds;
            var ground = new Vector3(b.center.x, b.min.y, b.center.z);
            float footprint = Mathf.Max(b.size.x, b.size.z);

            // 본체는 큐브보다 조금 크게. navmesh 구멍이 큐브 + 에이전트 반경이라 살짝 넘쳐도 캐릭터와 겹치지 않는다.
            Place(Pick(rng, ObstacleRocks), ground, Lerp(rng, 0, 360), group, fitFootprint: footprint * 1.2f, sink: 0.15f);

            int pebbles = 1 + rng.Next(3);
            for (int i = 0; i < pebbles; i++)
            {
                float angle = Lerp(rng, 0, Mathf.PI * 2f);
                float radius = footprint * Lerp(rng, 0.55f, 0.75f);
                var at = ground + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
                Place(Pick(rng, Pebbles), at, Lerp(rng, 0, 360), group, fitFootprint: Lerp(rng, 0.5f, 0.9f), sink: 0.05f);
            }
        }
    }

    private static void DressRamps(Layout layout, Transform root, System.Random rng)
    {
        // TerrainBuilder 경사로는 로컬 +Z 로 올라가는 한 장짜리 사면이다(뒤·옆이 뚫려 있음).
        // 판자를 얹어 봤지만 허공에 뜬 것처럼 보여, 사면을 그대로 두고 뒷벽·옆면을 막은 흙 둔덕으로 만든다.
        // 윗면이 원래 사면과 정확히 겹치므로 보이는 경사와 이동 판정이 어긋나지 않는다.
        var group = Group(root, "Ramps");
        Material dirt = GroundMaterial("Ground_Dirt", "GR_Dirt1");

        foreach (var ramp in layout.ramps)
        {
            Bounds b = ramp.sharedMesh.bounds;
            Vector3 P(float x, float y, float z) => ramp.transform.TransformPoint(new Vector3(x, y, z));

            Vector3 frontL = P(b.min.x, b.min.y, b.min.z), frontR = P(b.max.x, b.min.y, b.min.z);
            Vector3 topL = P(b.min.x, b.max.y, b.max.z), topR = P(b.max.x, b.max.y, b.max.z);
            Vector3 baseL = P(b.min.x, b.min.y, b.max.z), baseR = P(b.max.x, b.min.y, b.max.z);

            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();
            // 면마다 정점을 따로 둬서 모서리가 각지게 음영된다. 텍스처는 지형 레이어(10 m 반복)와 같은 월드 좌표.
            void Face(Vector2 uvScale, params Vector3[] corners)
            {
                int start = vertices.Count;
                foreach (var v in corners)
                {
                    vertices.Add(v);
                    uvs.Add(uvScale.x > 0 ? new Vector2(v.x, v.z) / 10f : new Vector2(v.x + v.z, v.y) / 10f);
                }
                for (int i = 1; i + 1 < corners.Length; i++)
                    triangles.AddRange(new[] { start, start + i, start + i + 1 });
            }
            Face(Vector2.one, frontL, topL, topR, frontR);    // 사면
            Face(Vector2.zero, baseR, topR, topL, baseL);     // 뒷벽
            Face(Vector2.zero, frontL, baseL, topL);          // 왼쪽
            Face(Vector2.zero, frontR, topR, baseR);          // 오른쪽

            var mesh = new Mesh { name = ramp.name };
            mesh.SetVertices(vertices);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();

            var mound = new GameObject(ramp.name);
            mound.transform.SetParent(group, false);
            mound.AddComponent<MeshFilter>().sharedMesh = mesh;
            mound.AddComponent<MeshRenderer>().sharedMaterial = dirt;

            // 반듯한 모서리가 상자처럼 보이지 않게 뒷벽·옆면 밑동에 자갈을 흩는다(사면 위는 비워 둔다).
            var edges = new[] { (baseL, baseR), (frontL, baseL), (frontR, baseR) };
            foreach (var (from, to) in edges)
            {
                Vector3 outward = Vector3.Cross(to - from, Vector3.up).normalized;
                if (Vector3.Dot(outward, from - ramp.transform.TransformPoint(b.center)) < 0f)
                    outward = -outward;
                for (int i = 0; i < 2; i++)
                {
                    Vector3 at = Vector3.Lerp(from, to, Lerp(rng, 0.2f, 0.9f)) + outward * Lerp(rng, 0.1f, 0.4f);
                    Place(Pick(rng, Pebbles), at, Lerp(rng, 0, 360), group, fitFootprint: Lerp(rng, 0.5f, 1.1f), sink: 0.1f);
                }
            }
        }
    }

    /// <summary>지형 레이어와 같은 텍스처의 Standard 머티리얼(지형이 아닌 메시에 흙을 입힐 때).</summary>
    private static Material GroundMaterial(string name, string texture)
    {
        string folder = $"{OutputFolder}/Materials";
        EnsureFolder(folder);
        string path = $"{folder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetTexture("_MainTex", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Pack}/Content/Textures/{texture}_AS.tif"));
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>($"{Pack}/Content/Textures/{texture}_N.tif"));
        material.EnableKeyword("_NORMALMAP");
        material.SetFloat("_Glossiness", 0.15f);
        EditorUtility.SetDirty(material);
        return material;
    }

    // ────────────────────────────────────────────────────────────────────
    // 경계와 바깥
    // ────────────────────────────────────────────────────────────────────

    private static void BuildFences(Layout layout, Transform root, System.Random rng)
    {
        var group = Group(root, "Fences");
        const float offset = 1.6f;
        string tall = Prefabs + "/Buildings/Structures1/Struct_Fence1_Mid_B.prefab";
        string tallAlt = Prefabs + "/Buildings/Structures1/Struct_Fence1_Mid_C.prefab";
        string low = Prefabs + "/Buildings/Structures1/Struct_Fence2_Mid_A.prefab";
        string lowBroken = Prefabs + "/Buildings/Structures1/Struct_Fence2_Mid_A_DM.prefab";
        string gatePrefab = Prefabs + "/Buildings/Structures1/Struct_Fence1_Gate_A.prefab";

        float segment = PrefabBounds(AssetDatabase.LoadAssetAtPath<GameObject>(tall)).size.x;

        // (시작, 끝, 고정 좌표, 동서 변인가, 남쪽 변인가)
        var edges = new[]
        {
            (layout.MinX - offset, layout.MaxX + offset, layout.MaxZ + offset, false, false),  // 북
            (layout.MinX - offset, layout.MaxX + offset, layout.MinZ - offset, false, true),   // 남
            (layout.MinZ - offset, layout.MaxZ + offset, layout.MinX - offset, true, false),   // 서
            (layout.MinZ - offset, layout.MaxZ + offset, layout.MaxX + offset, true, false),   // 동
        };

        foreach (var (from, to, fixedCoord, eastWest, south) in edges)
        {
            // 이 변에 붙은 게이트 자리는 비우고 문을 세운다.
            var gatesOnEdge = layout.gates
                .Where(g => eastWest
                    ? Mathf.Abs(g.x - fixedCoord) < layout.half.x * 0.3f
                    : Mathf.Abs(g.z - fixedCoord) < layout.half.z * 0.3f)
                .Select(g => eastWest ? g.z : g.x)
                .ToList();

            foreach (float along in gatesOnEdge)
            {
                var at = eastWest ? new Vector3(fixedCoord, layout.center.y, along) : new Vector3(along, layout.center.y, fixedCoord);
                Place(gatePrefab, at, eastWest ? 90 : 0, group);
            }

            for (float s = from + segment / 2f; s < to; s += segment)
            {
                if (gatesOnEdge.Any(g => Mathf.Abs(g - s) < segment * 1.2f))
                    continue;
                if (rng.NextDouble() < (south ? 0.25 : 0.1))
                    continue;   // 무너진 틈

                string prefab = south
                    ? (rng.NextDouble() < 0.4 ? lowBroken : low)
                    : (rng.NextDouble() < 0.25 ? tallAlt : tall);
                var at = eastWest ? new Vector3(fixedCoord, layout.center.y, s) : new Vector3(s, layout.center.y, fixedCoord);
                float yaw = (eastWest ? 90 : 0) + (rng.NextDouble() < 0.5 ? 180 : 0) + Lerp(rng, -3f, 3f);
                Place(prefab, at, yaw, group, sink: 0.1f);
            }
        }
    }

    private struct BuildingPlan
    {
        public string prefab;
        public Vector3 position;
        public float yaw;
        public float padRadius;
        public float sink;
        public float fitHeight;
    }

    private static List<BuildingPlan> PlanBuildings(Layout layout, TerrainShape shape)
    {
        Vector3 c = layout.center;
        float hx = layout.half.x, hz = layout.half.z;
        var plans = new List<BuildingPlan>
        {
            // 북쪽 마을 끝자락
            new BuildingPlan { prefab = Prefabs + "/Buildings/Cabins/Cabin1.prefab", position = new Vector3(c.x - 12, 0, c.z + hz + 15), yaw = 180, padRadius = 9 },
            new BuildingPlan { prefab = Prefabs + "/Buildings/Cabins/Outhouse_A.prefab", position = new Vector3(c.x - 1, 0, c.z + hz + 12), yaw = 200, padRadius = 3 },
            new BuildingPlan { prefab = Prefabs + "/Buildings/GuardHouse/GuardHouse_A.prefab", position = new Vector3(c.x + 17, 0, c.z + hz + 17), yaw = 190, padRadius = 7 },
            new BuildingPlan { prefab = Prefabs + "/Buildings/Structures1/Struct_RadioTower_A.prefab", position = new Vector3(c.x + 42, 0, c.z + hz + 42), yaw = 20, padRadius = 6, fitHeight = 26 },
            // 동쪽 게이트 길가
            new BuildingPlan { prefab = Prefabs + "/Buildings/Structures1/Struct_Kiosk_A.prefab", position = new Vector3(c.x + hx + 9, 0, c.z + 16), yaw = 250, padRadius = 3 },
            // 원경(안개 속 실루엣)
            new BuildingPlan { prefab = Prefabs + "/Backgrounds/BGR_Factory_B.prefab", position = new Vector3(c.x - 35, 0, c.z + hz + 90), yaw = 0, padRadius = 0, sink = 3 },
            new BuildingPlan { prefab = Prefabs + "/Backgrounds/BGR_LargePipe_A.prefab", position = new Vector3(c.x + 30, 0, c.z + hz + 95), yaw = 0, padRadius = 0, sink = 3 },
        };

        // 게이트 길을 막는 건물은 뺀다(게이트 위치는 맵마다 다르다).
        return plans.Where(p => shape.Road(p.position.x, p.position.z) < 0.05f).ToList();
    }

    private static void PlaceBuildings(List<BuildingPlan> plans, Terrain terrain, Transform root)
    {
        var group = Group(root, "Buildings");
        foreach (var plan in plans)
        {
            var at = plan.position;
            at.y = GroundY(terrain, at);
            Place(plan.prefab, at, plan.yaw, group, sink: plan.sink + 0.2f, fitHeight: plan.fitHeight);
        }
    }

    private static void ScatterOutskirts(Layout layout, TerrainShape shape, Terrain terrain, Transform root, System.Random rng)
    {
        Vector3 c = layout.center;
        float hx = layout.half.x, hz = layout.half.z;

        // 서쪽 언덕의 묘지
        var graves = Group(root, "Graveyard");
        var graveCenter = new Vector3(c.x - hx - 13, 0, c.z + 6);
        for (int i = 0; i < 14; i++)
        {
            var at = graveCenter + new Vector3(Lerp(rng, -4f, 4f), 0, (i % 5 - 2) * 2.4f + Lerp(rng, -0.4f, 0.4f));
            at.x += (i / 5 - 1) * 3f;
            at.y = GroundY(terrain, at);
            string prefab = $"{Prefabs}/Props/Prop_Gravestone_{(char)('A' + rng.Next(5))}.prefab";
            Place(prefab, at, 90 + Lerp(rng, -12f, 12f), graves, sink: Lerp(rng, 0.05f, 0.3f));
        }

        // 동쪽 게이트 길: 전봇대와 버려진 차
        var road = Group(root, "Roadside");
        foreach (var gate in layout.gates)
        {
            float dirX = Mathf.Sign(gate.x - c.x);
            bool eastWest = Mathf.Abs(gate.x - c.x) / hx > Mathf.Abs(gate.z - c.z) / hz;
            if (!eastWest)
                continue;

            for (int i = 0; i < 3; i++)
            {
                var pole = new Vector3(c.x + dirX * (hx + 10 + i * 22), 0, gate.z + 5f);
                pole.y = GroundY(terrain, pole);
                Place(Prefabs + "/Buildings/Structures1/Struct_Pole_A.prefab", pole, 90 + Lerp(rng, -6f, 6f), road, sink: 0.3f, fitHeight: 9f);
            }

            var car = new Vector3(c.x + dirX * (hx + 20), 0, gate.z - 6.5f);
            car.y = GroundY(terrain, car);
            Place(Prefabs + "/Props/Prop_Car1_DM.prefab", car, 70, road, sink: 0.25f);
        }

        // 남쪽 침수지: 반쯤 잠긴 배와 판자 길
        var flooded = Group(root, "Flooded");
        var boat = new Vector3(c.x - 9, WaterY, c.z - hz - 16);
        if (shape.Height(boat.x, boat.z) < WaterY - 0.5f)
            Place(Prefabs + "/Props/Prop_Boat_A.prefab", boat, 35, flooded, sink: 0.9f);
        var ship = new Vector3(c.x + 28, WaterY, c.z - hz - 60);
        Place(Prefabs + "/Props/Prop_Ship_A.prefab", ship, -25, flooded, sink: 6f, fitHeight: 16f);
        for (int i = 0; i < 4; i++)
        {
            var plank = new Vector3(c.x + 6 + i * 3.2f, 0, c.z - hz - 5 - i * 1.3f);
            plank.y = Mathf.Max(GroundY(terrain, plank), WaterY) + 0.05f;
            Place(Prefabs + "/Buildings/Structures1/Struct_WoodPath_A.prefab", plank, 90 + i * 7, flooded, fitFootprint: 4f);
        }

        // 바위와 덤불: 울타리 바깥 북·동·서
        var nature = Group(root, "Nature");
        string[] bigRocks = { Prefabs + "/Nature/Rocks/Rock_A.prefab", Prefabs + "/Nature/Rocks/Rock_B.prefab" };
        string[] bushes = { Prefabs + "/Nature/Bushes/DecoBush_A.prefab", Prefabs + "/Nature/Bushes/DecoBush_C.prefab" };
        int rocks = 0, bushCount = 0;
        for (int attempt = 0; attempt < 400 && (rocks < 16 || bushCount < 10); attempt++)
        {
            float angle = Lerp(rng, 0, Mathf.PI * 2f);
            float dist = Lerp(rng, Mathf.Max(hx, hz) + 5f, Mathf.Max(hx, hz) + 40f);
            var at = c + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * dist * 1.2f;
            float outside = shape.Outside(at.x, at.z);
            if (outside < 5f || at.z < layout.MinZ || shape.Road(at.x, at.z) > 0.05f)
                continue;
            at.y = GroundY(terrain, at);
            if (at.y < WaterY + 0.2f)
                continue;

            if (rocks < 16)
            {
                Place(Pick(rng, bigRocks), at, Lerp(rng, 0, 360), nature, fitFootprint: Lerp(rng, 2.5f, 5f), sink: 0.6f);
                rocks++;
            }
            else
            {
                Place(Pick(rng, bushes), at, Lerp(rng, 0, 360), nature, fitFootprint: Lerp(rng, 4f, 6f), sink: 0.3f);
                bushCount++;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 분위기
    // ────────────────────────────────────────────────────────────────────

    private static void ApplyAtmosphere(Scene scene)
    {
        // 값은 Scene_A 에서 가져오되, 탑뷰 거리에 맞게 조정했다.
        //   - 안개: 원본은 0~400 m 선형이라 카메라 거리(10~24 m)에서는 안 보인다. 가까운 원경부터 흐리게.
        //   - 환경광: 원본은 Skybox 모드인데 조명을 굽지 않은 씬에서는 결과가 들쭉날쭉해 Trilight 로 고정.
        //   - 해: 원본(고도 25도)은 그림자가 너무 길어 캐릭터를 덮는다. 고도를 올렸다.
        RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>($"{Pack}/Content/Materials/BGR_Sky1.mat");
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.52f, 0.56f, 0.60f);
        RenderSettings.ambientEquatorColor = new Color(0.38f, 0.41f, 0.43f);
        RenderSettings.ambientGroundColor = new Color(0.17f, 0.16f, 0.14f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.492f, 0.567f, 0.612f);
        RenderSettings.fogStartDistance = 30f;
        RenderSettings.fogEndDistance = 130f;
        RenderSettings.defaultReflectionMode = UnityEngine.Rendering.DefaultReflectionMode.Custom;
        RenderSettings.customReflectionTexture = AssetDatabase.LoadAssetAtPath<Cubemap>($"{Pack}/Content/Textures/BGR_RefCube1.tif");
        RenderSettings.reflectionIntensity = 0.6f;

        Light sun = scene.GetRootGameObjects()
            .SelectMany(g => g.GetComponentsInChildren<Light>(true))
            .FirstOrDefault(l => l.type == LightType.Directional);
        if (sun != null)
        {
            sun.color = new Color(0.965f, 0.98f, 1f);
            sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.75f;
            sun.transform.rotation = Quaternion.Euler(50f, 125f, 0f);
            RenderSettings.sun = sun;
        }
    }

    // ────────────────────────────────────────────────────────────────────
    // 미리보기
    // ────────────────────────────────────────────────────────────────────

    private static void RenderPreviews(Scene scene, string dir)
    {
        Directory.CreateDirectory(dir);
        Layout layout = Analyze(out _);

        // 비동기 컴파일이면 첫 렌더에 셰이더가 하늘색 대체물로 찍힌다.
        // EditorSettings.asyncShaderCompilation 은 ProjectSettings 파일에 저장되므로 세션 한정인 이쪽을 쓴다.
        bool asyncShaders = ShaderUtil.allowAsyncCompilation;
        ShaderUtil.allowAsyncCompilation = false;
        try
        {
            RenderShots(scene, dir, layout);
        }
        finally
        {
            ShaderUtil.allowAsyncCompilation = asyncShaders;
        }
    }

    private static void RenderShots(Scene scene, string dir, Layout layout)
    {
        Vector3 c = layout.center;

        var cameraObject = new GameObject("PreviewCamera");
        var cam = cameraObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.Skybox;
        cam.fieldOfView = 40f;
        cam.farClipPlane = 1000f;
        cam.aspect = 16f / 9f;

        // 크기 비교용 캐릭터·몬스터
        var actors = new List<GameObject>();
        GameObject SpawnActor(string resource, Vector3 at, float yaw)
        {
            var prefab = Resources.Load<GameObject>(resource);
            if (prefab == null)
                return null;
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            go.transform.SetPositionAndRotation(at, Quaternion.Euler(0, yaw, 0));
            actors.Add(go);
            return go;
        }

        Vector3 gate = layout.gates.Count > 0 ? layout.gates[0] : new Vector3(layout.MaxX - 2, c.y, c.z);
        var shots = new (string name, Vector3 focus, float zoom)[]
        {
            ("center", c + new Vector3(0, 0, 0), 0.55f),
            ("north_edge", new Vector3(c.x - 4, c.y, layout.MaxZ - 3), 0.55f),
            ("south_edge", new Vector3(c.x + 3, c.y, layout.MinZ + 3), 0.55f),
            ("gate", new Vector3(gate.x - 2, c.y, gate.z), 0.55f),
            ("west_far", new Vector3(layout.MinX + 3, c.y, c.z + 4), 1f),
            ("near", c + new Vector3(-6, 0, -8), 0f),
        };

        foreach (var shot in shots)
        {
            foreach (var a in actors)
                Object.DestroyImmediate(a);
            actors.Clear();
            SpawnActor("Character2", shot.focus, 150);
            SpawnActor("Monster", shot.focus + new Vector3(2.5f, 0, 2f), 230);

            float pitch = Mathf.Lerp(45f, 60f, shot.zoom);
            float distance = Mathf.Lerp(10f, 24f, shot.zoom);
            Quaternion rot = Quaternion.Euler(pitch, 0, 0);
            Vector3 focus = shot.focus + Vector3.up;
            cam.transform.SetPositionAndRotation(focus - rot * Vector3.forward * distance, rot);
            SavePng(cam, Path.Combine(dir, $"env_{shot.name}.png"));
        }

        // 전경(구성 확인용)
        foreach (var a in actors)
            Object.DestroyImmediate(a);
        // 안개 끝(130 m)보다 멀어 그대로 찍으면 회색뿐이다. 구성 확인용이라 잠깐 끄고 찍는다(씬은 이미 저장됨).
        bool fog = RenderSettings.fog;
        RenderSettings.fog = false;
        cam.transform.SetPositionAndRotation(c + new Vector3(0, 95, -95), Quaternion.Euler(42, 0, 0));
        SavePng(cam, Path.Combine(dir, "env_overview.png"));
        var water = Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None).FirstOrDefault(r => r.name == "Water");
        if (water != null)
        {
            water.enabled = false;
            SavePng(cam, Path.Combine(dir, "env_overview_nowater.png"));
            water.enabled = true;
        }
        RenderSettings.fog = fog;

        Object.DestroyImmediate(cameraObject);
        Debug.Log($"[EnvDressing] 미리보기 저장: {dir}");
    }

    private static void SavePng(Camera cam, string path)
    {
        const int width = 1600, height = 900;
        var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 4 };
        cam.targetTexture = rt;
        cam.Render();
        cam.Render();   // 첫 프레임은 지형 기본 텍스처·나무 빌보드가 덜 준비될 수 있다.
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

    // ────────────────────────────────────────────────────────────────────
    // 배치 도우미
    // ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// 프리팹을 놓는다. 팩 프리팹은 루트 좌표에 원본 씬 위치(수백 m)가 박혀 있어서 루트를 옮기는 게 아니라
    /// **렌더러 경계**를 기준으로 맞춘다: 경계 바닥 중심이 <paramref name="at"/> 에 오도록.
    /// </summary>
    private static GameObject Place(string prefabPath, Vector3 at, float yaw, Transform parent,
                                    float fitFootprint = 0f, float fitHeight = 0f, float sink = 0f)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            throw new FileNotFoundException($"프리팹 없음: {prefabPath}");

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        var t = go.transform;
        Vector3 baseScale = prefab.transform.localScale;
        Quaternion baseRotation = prefab.transform.rotation;

        if (fitFootprint > 0f || fitHeight > 0f)
        {
            Bounds raw = RendererBounds(go);
            float scale = fitFootprint > 0f
                ? fitFootprint / Mathf.Max(0.01f, Mathf.Max(raw.size.x, raw.size.z))
                : fitHeight / Mathf.Max(0.01f, raw.size.y);
            t.localScale = baseScale * scale;
        }

        t.rotation = Quaternion.Euler(0, yaw, 0) * baseRotation;
        Bounds b = RendererBounds(go);
        t.position += new Vector3(at.x - b.center.x, at.y - b.min.y - sink, at.z - b.center.z);
        return go;
    }

    private static Bounds RendererBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>()
            .Where(r => !(r is ParticleSystemRenderer) && r.bounds.size.sqrMagnitude > 0f)
            .ToArray();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);
        Bounds b = renderers[0].bounds;
        foreach (var r in renderers)
            b.Encapsulate(r.bounds);
        return b;
    }

    private static Bounds PrefabBounds(GameObject prefab)
    {
        if (prefab == null)
            return new Bounds(Vector3.zero, Vector3.one);
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        try
        {
            go.transform.rotation = Quaternion.identity * prefab.transform.rotation;
            return RendererBounds(go);
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    private static int DisableColliders(GameObject root)
    {
        int count = 0;
        foreach (var collider in root.GetComponentsInChildren<Collider>(true))
        {
            if (!collider.enabled)
                continue;
            collider.enabled = false;
            count++;
        }
        return count;
    }

    private static float GroundY(Terrain terrain, Vector3 at) =>
        terrain.SampleHeight(at) + terrain.transform.position.y;

    private static Transform Group(Transform root, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(root, false);
        return go.transform;
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

    private static string CommandLineValue(string key)
    {
        var args = Environment.GetCommandLineArgs();
        int i = Array.IndexOf(args, key);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static float Smooth01(float t)
    {
        t = Mathf.Clamp01(t);
        return t * t * (3f - 2f * t);
    }

    private static float Lerp(System.Random rng, float a, float b) => a + (float)rng.NextDouble() * (b - a);

    private static string Pick(System.Random rng, string[] options) => options[rng.Next(options.Length)];

    // string.GetHashCode 는 런타임에 따라 달라질 수 있어 배치 재현용으로 직접 계산한다.
    private static int StableHash(string s)
    {
        unchecked
        {
            int h = (int)2166136261;
            foreach (char ch in s)
                h = (h ^ ch) * 16777619;
            return h & 0x7fffffff;
        }
    }
}
