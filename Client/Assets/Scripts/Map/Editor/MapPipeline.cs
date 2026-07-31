using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 맵 제작 파이프라인의 실행 단위. 창(MapToolWindow)은 여기 있는 함수를 부르기만 한다.
///
/// 단계는 씬 이름 하나로 엮인다:
///   씬 "Foo" → OBJ "Assets/GeneratedObj/Foo.obj" → NavMesh "Assets/GeneratedNavMeshes/Foo_navmesh.bin"
///            → GameData/Foo_navmesh.bin → Map.json 의 scene=="Foo" 항목(navmesh_path)
/// 그래서 앞 단계 산출물을 뒤 단계가 자동으로 찾아 쓸 수 있고, 무엇이 빠졌는지도 파일 존재로 판단한다.
/// </summary>
public static class MapPipeline
{
    public const string SceneFolder = "Assets/Scenes";
    public const string ObjFolder = "Assets/GeneratedObj";
    public const string NavMeshFolder = "Assets/GeneratedNavMeshes";

    // ── 경로 규칙(단계들을 잇는 유일한 약속) ──
    public static string ScenePathOf(string sceneName) => $"{SceneFolder}/{sceneName}.unity";
    public static string ObjPathOf(string sceneName) => $"{ObjFolder}/{sceneName}.obj";
    public static string NavMeshFileNameOf(string sceneName) => $"{sceneName}_navmesh.bin";
    public static string NavMeshPathOf(string sceneName) => $"{NavMeshFolder}/{NavMeshFileNameOf(sceneName)}";

    public static string GameDataDir()
    {
        string mapJson = MapJsonUpdater.FindMapJsonPath();
        return mapJson == null ? null : Path.GetDirectoryName(mapJson);
    }

    public static string GameDataNavMeshPathOf(string sceneName)
    {
        string dir = GameDataDir();
        return dir == null ? null : Path.Combine(dir, NavMeshFileNameOf(sceneName));
    }

    // ── 1단계: 맵 씬 ──

    /// <summary>새 맵 씬을 만들고 저장한다. 이미 있으면 열기만 한다.</summary>
    public static bool CreateOrOpenScene(string sceneName, out string error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            error = "씬 이름이 비어 있습니다.";
            return false;
        }

        string path = ScenePathOf(sceneName);
        if (File.Exists(path))
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return false;
            EditorSceneManager.OpenScene(path);
            return true;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        Directory.CreateDirectory(SceneFolder);
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        if (!EditorSceneManager.SaveScene(scene, path))
        {
            error = $"씬 저장에 실패했습니다: {path}";
            return false;
        }

        AssetDatabase.Refresh();
        return true;
    }

    /// <summary>씬이 Build Settings 에 있는지. 없으면 런타임에 게이트 이동으로 로드할 수 없다.</summary>
    public static bool IsSceneInBuildSettings(string sceneName)
    {
        string path = ScenePathOf(sceneName);
        return EditorBuildSettings.scenes.Any(s => s.path.Replace("\\", "/") == path);
    }

    public static void AddSceneToBuildSettings(string sceneName)
    {
        string path = ScenePathOf(sceneName);
        if (IsSceneInBuildSettings(sceneName))
            return;

        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
        Debug.Log($"[MapTool] Build Settings 에 씬 추가: {path}");
    }

    /// <summary>
    /// 다른 맵 씬의 기본 세팅(카메라·조명 오브젝트 + 렌더 설정)을 현재 씬으로 복사한다.
    ///
    /// 새 씬은 기본 카메라/조명만 있어서 기존 맵과 밝기·스카이박스·안개가 제각각이 되기 쉽다.
    /// 맵을 여럿 만들 때 같은 룩을 맞추는 가장 확실한 방법은 이미 만들어 둔 씬을 본뜨는 것이다.
    ///
    /// 지형·마커(게이트/스폰/오브젝트)는 맵마다 달라야 하므로 복사하지 않는다.
    /// </summary>
    public static bool CopySceneSetupFrom(string referenceScenePath, bool replaceExisting, out string summary, out string error)
    {
        summary = null;
        error = null;

        Scene target = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(target.path))
        {
            error = "현재 씬을 먼저 저장하세요.";
            return false;
        }
        if (target.path == referenceScenePath)
        {
            error = "현재 씬과 같은 씬은 참고할 수 없습니다.";
            return false;
        }
        if (!File.Exists(referenceScenePath))
        {
            error = $"참고할 씬을 찾지 못했습니다: {referenceScenePath}";
            return false;
        }

        int removed = 0;
        if (replaceExisting)
        {
            foreach (GameObject root in target.GetRootGameObjects())
            {
                if (!IsSetupObject(root))
                    continue;
                Undo.DestroyObjectImmediate(root);
                removed++;
            }
        }

        Scene reference = EditorSceneManager.OpenScene(referenceScenePath, OpenSceneMode.Additive);
        int copied = 0;
        try
        {
            // 렌더 설정은 '활성 씬'의 것이 읽힌다. 참고 씬을 잠깐 활성으로 두고 값을 떠 온다.
            EditorSceneManager.SetActiveScene(reference);
            var settings = RenderSettingsSnapshot.Capture();

            EditorSceneManager.SetActiveScene(target);
            settings.Apply();

            foreach (GameObject root in reference.GetRootGameObjects())
            {
                if (!IsSetupObject(root))
                    continue;

                var copy = Object.Instantiate(root);
                copy.name = root.name;
                SceneManager.MoveGameObjectToScene(copy, target);
                Undo.RegisterCreatedObjectUndo(copy, "Copy Scene Setup");
                copied++;
            }
        }
        finally
        {
            EditorSceneManager.CloseScene(reference, true);
            EditorSceneManager.SetActiveScene(target);
        }

        EditorSceneManager.MarkSceneDirty(target);
        summary = $"{Path.GetFileNameWithoutExtension(referenceScenePath)} → 카메라/조명 {copied}개 복사" +
                  (removed > 0 ? $", 기존 {removed}개 교체" : "") + ", 렌더 설정 적용";
        return true;
    }

    // 복사 대상: 카메라/조명 같은 '씬 세팅' 오브젝트. 지형·마커·액터는 맵 고유라 제외한다.
    private static bool IsSetupObject(GameObject root)
    {
        if (root.name == TerrainBuilder.RootName || root.name == "MapDesign")
            return false;
        if (root.GetComponentInChildren<Gate>(true) != null ||
            root.GetComponentInChildren<SpawnPoint>(true) != null ||
            root.GetComponentInChildren<MapObjectMarker>(true) != null)
            return false;

        return root.GetComponentInChildren<Camera>(true) != null ||
               root.GetComponentInChildren<Light>(true) != null;
    }

    // 씬 단위 렌더 설정(조명 환경). 오브젝트가 아니라 씬에 붙는 값이라 따로 옮겨야 한다.
    private struct RenderSettingsSnapshot
    {
        private Material skybox;
        private Light sun;
        private UnityEngine.Rendering.AmbientMode ambientMode;
        private Color ambientLight, ambientSky, ambientEquator, ambientGround;
        private float ambientIntensity, reflectionIntensity;
        private bool fog;
        private Color fogColor;
        private FogMode fogMode;
        private float fogDensity, fogStart, fogEnd;

        public static RenderSettingsSnapshot Capture()
        {
            return new RenderSettingsSnapshot
            {
                skybox = RenderSettings.skybox,
                sun = RenderSettings.sun,
                ambientMode = RenderSettings.ambientMode,
                ambientLight = RenderSettings.ambientLight,
                ambientSky = RenderSettings.ambientSkyColor,
                ambientEquator = RenderSettings.ambientEquatorColor,
                ambientGround = RenderSettings.ambientGroundColor,
                ambientIntensity = RenderSettings.ambientIntensity,
                reflectionIntensity = RenderSettings.reflectionIntensity,
                fog = RenderSettings.fog,
                fogColor = RenderSettings.fogColor,
                fogMode = RenderSettings.fogMode,
                fogDensity = RenderSettings.fogDensity,
                fogStart = RenderSettings.fogStartDistance,
                fogEnd = RenderSettings.fogEndDistance,
            };
        }

        public void Apply()
        {
            RenderSettings.skybox = skybox;
            RenderSettings.ambientMode = ambientMode;
            RenderSettings.ambientLight = ambientLight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.ambientIntensity = ambientIntensity;
            RenderSettings.reflectionIntensity = reflectionIntensity;
            RenderSettings.fog = fog;
            RenderSettings.fogColor = fogColor;
            RenderSettings.fogMode = fogMode;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogStartDistance = fogStart;
            RenderSettings.fogEndDistance = fogEnd;
            // sun 은 참고 씬의 Light 를 가리키므로 그대로 두면 씬을 닫을 때 끊긴다.
            // 복사된 조명 중 Directional 을 찾아 다시 연결한다.
            RenderSettings.sun = Object.FindObjectsOfType<Light>()
                .FirstOrDefault(l => l.type == LightType.Directional);
        }
    }

    // ── 2단계: 지형 ──
    // 실제 생성은 TerrainBuilder 가 담당한다(여기서는 결과 판정만).

    /// <summary>씬에 지오메트리(MeshFilter)가 있는지. NavMesh 를 구울 수 있는 최소 조건이다.</summary>
    public static int CountSceneMeshes()
    {
        return Object.FindObjectsOfType<MeshFilter>()
            .Count(mf => mf != null && mf.sharedMesh != null);
    }

    // ── 3단계: NavMesh ──

    /// <summary>
    /// 씬의 모든 MeshFilter 를 하나의 OBJ 로 내보낸다(선택 영역이 아니라 씬 전체).
    /// NavMesh 는 이 OBJ 를 입력으로 굽는다.
    /// </summary>
    public static bool ExportSceneObj(string sceneName, out string objPath, out string error)
    {
        objPath = ObjPathOf(sceneName);
        error = null;

        var meshFilters = Object.FindObjectsOfType<MeshFilter>()
            .Where(mf => mf != null && mf.sharedMesh != null)
            .ToArray();

        if (meshFilters.Length == 0)
        {
            error = "씬에 내보낼 메시가 없습니다. 지형을 먼저 만드세요.";
            return false;
        }

        Directory.CreateDirectory(ObjFolder);
        if (!ObjExportor.ExportMeshesToFile(meshFilters, ObjFolder, sceneName, out error))
            return false;

        AssetDatabase.Refresh();
        return true;
    }

    /// <summary>OBJ 로부터 NavMesh 바이너리를 굽는다.</summary>
    public static bool GenerateNavMesh(string sceneName, string objPath, NavMeshBakeSettings settings, out string error)
    {
        error = null;
        if (!File.Exists(objPath))
        {
            error = $"OBJ 파일이 없습니다: {objPath}";
            return false;
        }

        Directory.CreateDirectory(NavMeshFolder);
        string outPath = NavMeshPathOf(sceneName);

        // 네이티브가 로그 파일을 새로 쓰므로, 실패했을 때 '이번 굽기'의 로그만 골라내기 위해 기준 시각을 잡아 둔다.
        System.DateTime startedAt = System.DateTime.Now;

        bool ok = RecastNavigation.Unity.RecastNavigationWrapper.GenerateNavMesh(
            objPath, outPath,
            settings.cellSize, settings.cellHeight,
            settings.walkableSlopeAngle, settings.walkableHeight,
            settings.walkableRadius, settings.walkableClimb,
            settings.minRegionArea, settings.mergeRegionArea,
            settings.maxSimplificationError, settings.maxEdgeLen,
            settings.detailSampleDistance, settings.detailSampleMaxError);

        if (!ok)
        {
            // 네이티브는 bool 만 돌려주고 실패 이유는 자기 로그 파일에만 남긴다. 그대로 두면
            // 콘솔에는 "NavMesh generation failed" 한 줄뿐이라 원인을 알 수 없다.
            error = DescribeBakeFailure(startedAt);
            return false;
        }

        AssetDatabase.Refresh();
        return true;
    }

    // ── NavMesh 예산 ──
    //
    // Recast 는 맵 하나를 타일 하나로 굽고(솔로 빌드), 폴리곤 메시 정점 인덱스가 unsigned short 다.
    // 그래서 컨투어 정점 합계가 65534 를 넘으면 rcBuildPolyMesh 가 실패한다
    // ("Could not triangulate contours."). 맵이 커서가 아니라 '장애물이 많아서' 걸리는 한계다.
    public const int MaxContourVerts = 65534;

    // 실측(1000x1000 맵, 2m 큐브 장애물, cellSize 0.3):
    //   장애물 2000개 → 컨투어 정점 15,136 / 5000개 → 34,026 / 20000개 → 139,372 (실패)
    // 오브젝트당 약 7 정점으로 거의 선형이었다. cellSize 를 키우면 이 값이 줄지만
    // (작은 장애물이 셀에 먹혀 사라진다) 비례하지는 않으므로 기본값 기준으로만 추정한다.
    private const float ContourVertsPerMesh = 7f;

    public struct NavMeshBudget
    {
        public int meshCount;
        public int gridWidth, gridHeight;   // 하이트필드 셀 수
        public long cellCount;
        public int estimatedVerts;

        public bool OverBudget => estimatedVerts > MaxContourVerts;
        public bool NearBudget => !OverBudget && estimatedVerts > MaxContourVerts * 0.7f;
        public bool HeavyGrid => cellCount > 4_000_000;   // 실측 11M 셀 = 약 13초

        /// <summary>몇 개까지 줄이면 한계 안에 들어오는지(대략).</summary>
        public int SuggestedMeshCount => SuggestedMaxMeshCount;
    }

    /// <summary>단일 타일 NavMesh 로 안전하게 구울 수 있는 대략적인 오브젝트 수.</summary>
    public static int SuggestedMaxMeshCount => Mathf.FloorToInt(MaxContourVerts * 0.7f / ContourVertsPerMesh);

    /// <summary>현재 씬을 이 설정으로 구우면 Recast 한계에 걸리는지 미리 가늠한다.</summary>
    public static NavMeshBudget EstimateBudget(NavMeshBakeSettings settings)
    {
        var budget = new NavMeshBudget { meshCount = CountSceneMeshes() };

        Bounds? bounds = SceneBounds();
        float cellSize = Mathf.Max(0.01f, settings != null ? settings.cellSize : 0.3f);
        if (bounds.HasValue)
        {
            budget.gridWidth = Mathf.CeilToInt(bounds.Value.size.x / cellSize);
            budget.gridHeight = Mathf.CeilToInt(bounds.Value.size.z / cellSize);
            budget.cellCount = (long)budget.gridWidth * budget.gridHeight;
        }

        budget.estimatedVerts = Mathf.RoundToInt(budget.meshCount * ContourVertsPerMesh);
        return budget;
    }

    // 네이티브 로그에서 이번 굽기의 실패 이유를 꺼내 온다.
    private static string DescribeBakeFailure(System.DateTime startedAt)
    {
        const string fallback = "NavMesh 생성에 실패했습니다. 콘솔 로그를 확인하세요.";

        string reason = ReadNativeLogTail(startedAt);
        if (reason == null)
            return fallback;

        // 아는 실패는 원인과 해결책까지 붙여 준다(로그만으로는 왜 걸렸는지 알 수 없다).
        if (reason.Contains("Could not triangulate contours"))
        {
            var budget = EstimateBudget(null);
            return $"NavMesh 정점 수가 Recast 한계({MaxContourVerts})를 넘었습니다 — \"{reason}\"\n" +
                   $"맵 크기가 아니라 장애물 개수가 원인입니다(현재 메시 {budget.meshCount}개). " +
                   $"장애물을 {budget.SuggestedMeshCount}개 이하로 줄이거나 Cell size 를 키우세요.";
        }

        return $"NavMesh 생성에 실패했습니다 — \"{reason}\"";
    }

    // 네이티브는 작업 폴더의 logs/ 에 recastnavigation_*.log 를 쓴다(유니티에서는 Client/Logs).
    private static string ReadNativeLogTail(System.DateTime startedAt)
    {
        try
        {
            var dir = new DirectoryInfo("Logs");
            if (!dir.Exists)
                dir = new DirectoryInfo("logs");
            if (!dir.Exists)
                return null;

            FileInfo log = dir.GetFiles("recastnavigation_*.log")
                .Where(f => f.LastWriteTime >= startedAt.AddSeconds(-5))
                .OrderByDescending(f => f.LastWriteTime)
                .FirstOrDefault();
            if (log == null)
                return null;

            // 컨투어 덤프가 수만 줄이라 마지막 의미 있는 줄만 고른다.
            string last = null;
            foreach (string line in File.ReadLines(log.FullName))
            {
                string trimmed = line.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("Contour ") || trimmed.StartsWith("==="))
                    continue;
                last = trimmed;
            }
            return last;
        }
        catch (IOException)
        {
            return null;   // 네이티브가 아직 쥐고 있을 수 있다. 원인 못 찾으면 기본 메시지로 간다.
        }
    }

    /// <summary>NavMesh 가 씬보다 오래됐는지(씬을 고치고 다시 굽지 않은 상태인지).</summary>
    public static bool IsNavMeshStale(string sceneName)
    {
        string navMesh = NavMeshPathOf(sceneName);
        string scenePath = ScenePathOf(sceneName);
        if (!File.Exists(navMesh) || !File.Exists(scenePath))
            return false;

        return File.GetLastWriteTimeUtc(scenePath) > File.GetLastWriteTimeUtc(navMesh);
    }

    /// <summary>GameData 사본이 없거나 오래됐는지. 서버는 GameData 쪽만 읽는다.</summary>
    public static bool IsGameDataNavMeshStale(string sceneName)
    {
        string src = NavMeshPathOf(sceneName);
        string dst = GameDataNavMeshPathOf(sceneName);
        if (!File.Exists(src) || dst == null)
            return false;
        if (!File.Exists(dst))
            return true;

        return File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst);
    }

    // ── 4단계: Map.json 등록 ──

    /// <summary>
    /// 씬을 Map.json 에 등록한다(없으면 항목 생성). 마커 스캔 + 맵 크기 + NavMesh 복사/경로까지 한 번에 처리한다.
    /// 서버가 읽는 것은 GameData 쪽 사본이므로 복사까지 해야 실제로 반영된다.
    /// </summary>
    public static bool RegisterToMapJson(string sceneName, bool scanMarkers, bool copyNavMesh, out string summary, out string error)
    {
        summary = null;
        error = null;

        string mapJsonPath = MapJsonUpdater.FindMapJsonPath();
        if (mapJsonPath == null)
        {
            error = "Map.json 을 찾지 못했습니다(Assets/Resources/GameData/Map.json).";
            return false;
        }

        List<MapJsonUpdater.MapData> maps = MapJsonUpdater.LoadMapList(mapJsonPath);
        MapJsonUpdater.MapData map = MapJsonUpdater.FindMapForScene(maps, sceneName);

        bool created = false;
        if (map == null)
        {
            map = NewMapForScene(maps, sceneName);
            maps.Add(map);
            created = true;
        }

        if (scanMarkers)
            MapJsonUpdater.ScanSceneIntoMap(map, maps, sceneName);

        ApplySceneBounds(map);

        bool copied = false;
        if (copyNavMesh)
        {
            string src = NavMeshPathOf(sceneName);
            string dst = GameDataNavMeshPathOf(sceneName);
            if (File.Exists(src) && dst != null)
            {
                File.Copy(src, dst, true);
                map.navmesh_path = NavMeshFileNameOf(sceneName);
                copied = true;
            }
        }

        MapJsonUpdater.BackupBeforeWrite(mapJsonPath);
        MapJsonUpdater.SaveMapList(mapJsonPath, maps);
        AssetDatabase.Refresh();

        summary = $"map id {map.id} ({(created ? "생성" : "갱신")})" +
                  $", gates {map.gates.Count}, spawns {map.spawn_points.player_spawn.Count + map.spawn_points.monster_spawn.Count + map.spawn_points.boss_spawn.Count}" +
                  (copied ? ", navmesh 복사됨" : "");
        return true;
    }

    /// <summary>현재 씬과 연결된 맵 항목(없으면 null). 상태 표시에 쓴다.</summary>
    public static MapJsonUpdater.MapData FindMapEntry(string sceneName)
    {
        return MapJsonUpdater.FindMapForScene(LoadAllMaps(), sceneName);
    }

    /// <summary>Map.json 의 모든 맵. 파일이 없으면 빈 목록.</summary>
    public static List<MapJsonUpdater.MapData> LoadAllMaps()
    {
        string mapJsonPath = MapJsonUpdater.FindMapJsonPath();
        return mapJsonPath == null
            ? new List<MapJsonUpdater.MapData>()
            : MapJsonUpdater.LoadMapList(mapJsonPath);
    }

    /// <summary>맵 항목이 가리키는 씬 이름(scene 이 비어 있으면 name 을 쓴다).</summary>
    public static string SceneNameOf(MapJsonUpdater.MapData map)
    {
        return string.IsNullOrEmpty(map.scene) ? map.name : map.scene;
    }

    /// <summary>맵의 씬 에셋 경로. 프로젝트 어디에 있든 이름으로 찾는다(없으면 null).</summary>
    public static string FindSceneAssetPath(MapJsonUpdater.MapData map)
    {
        string sceneName = SceneNameOf(map);
        if (string.IsNullOrEmpty(sceneName))
            return null;

        return AssetDatabase.FindAssets($"t:Scene {sceneName}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p)
                .Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>맵의 씬을 연다(저장 확인 포함). 씬 에셋이 없으면 false.</summary>
    public static bool OpenSceneOf(MapJsonUpdater.MapData map, out string error)
    {
        error = null;
        string scenePath = FindSceneAssetPath(map);
        if (scenePath == null)
        {
            error = $"씬 에셋을 찾지 못했습니다: '{SceneNameOf(map)}' (map id {map.id})";
            return false;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        EditorSceneManager.OpenScene(scenePath);
        return true;
    }

    /// <summary>Map.json 의 맵 메타데이터(마커를 제외한 컬럼들). 창의 편집 버퍼로 쓴다.</summary>
    public class MapMetadata
    {
        public string name = "";
        public string scene = "";
        public string name_id = "";
        public string desc_id = "";
        public int game_mode_id = 1;
        public double width;
        public double height;
        public string navmesh_path = "";
        public int aoi_radius;

        public static MapMetadata From(MapJsonUpdater.MapData map)
        {
            return new MapMetadata
            {
                name = map.name ?? "",
                scene = map.scene ?? "",
                name_id = map.name_id ?? "",
                desc_id = map.desc_id ?? "",
                game_mode_id = map.game_mode_id,
                width = map.size != null ? map.size.width : 0,
                height = map.size != null ? map.size.height : 0,
                navmesh_path = map.navmesh_path ?? "",
                aoi_radius = ReadAoIRadius(map),
            };
        }
    }

    /// <summary>
    /// 맵 메타데이터를 Map.json 에 반영한다.
    /// 창이 들고 있던 목록을 통째로 덮어쓰지 않고 **디스크를 다시 읽어 해당 맵의 필드만 적용**한다 —
    /// 자동 동기화(MapJsonAutoSync)가 그 사이 마커 변경을 저장했을 수 있기 때문이다.
    /// </summary>
    public static bool UpdateMapMetadata(int mapId, MapMetadata edit, out string error)
    {
        error = null;

        string mapJsonPath = MapJsonUpdater.FindMapJsonPath();
        if (mapJsonPath == null)
        {
            error = "Map.json 을 찾지 못했습니다.";
            return false;
        }

        var maps = MapJsonUpdater.LoadMapList(mapJsonPath);
        var map = maps.FirstOrDefault(m => m.id == mapId);
        if (map == null)
        {
            error = $"map id {mapId} 항목이 Map.json 에 없습니다(다른 곳에서 삭제되었을 수 있습니다).";
            return false;
        }

        map.name = edit.name;
        map.scene = edit.scene;
        map.name_id = edit.name_id;
        map.desc_id = edit.desc_id;
        map.game_mode_id = edit.game_mode_id;
        if (map.size == null)
            map.size = new MapJsonUpdater.MapSize();
        map.size.width = edit.width;
        map.size.height = edit.height;
        map.navmesh_path = edit.navmesh_path;
        WriteAoIRadius(map, edit.aoi_radius);

        MapJsonUpdater.BackupBeforeWrite(mapJsonPath);
        MapJsonUpdater.SaveMapList(mapJsonPath, maps);
        AssetDatabase.Refresh();
        return true;
    }

    // aoi_radius 는 MapData 에 필드가 없고 JsonExtensionData(extra)로 보존되는 값이다.
    // 서버 관심영역 반경(0 이면 전체 브로드캐스트)이라 맵 제작 중에 볼 수 있어야 한다.
    private const string AoIRadiusKey = "aoi_radius";

    public static int ReadAoIRadius(MapJsonUpdater.MapData map)
    {
        if (map.extra != null && map.extra.TryGetValue(AoIRadiusKey, out var token) &&
            token.Type == Newtonsoft.Json.Linq.JTokenType.Integer)
        {
            return token.ToObject<int>();
        }
        return 0;
    }

    private static void WriteAoIRadius(MapJsonUpdater.MapData map, int value)
    {
        if (map.extra == null)
            map.extra = new Dictionary<string, Newtonsoft.Json.Linq.JToken>();
        map.extra[AoIRadiusKey] = value;
    }

    private static MapJsonUpdater.MapData NewMapForScene(List<MapJsonUpdater.MapData> maps, string sceneName)
    {
        string key = sceneName.ToLower().Replace(" ", "_");
        return new MapJsonUpdater.MapData
        {
            id = maps.Count == 0 ? 1 : maps.Max(m => m.id) + 1,
            name = sceneName,
            scene = sceneName,
            name_id = $"map_{key}_name",
            desc_id = $"map_{key}_desc",
            game_mode_id = 1, // 기본값: Field
            size = new MapJsonUpdater.MapSize { width = 1000, height = 1000 },
        };
    }

    /// <summary>씬의 모든 Renderer 를 감싸는 경계(맵 크기 계산용). 없으면 null.</summary>
    public static Bounds? SceneBounds()
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
        if (renderers.Length == 0)
            return null;

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);
        return bounds;
    }

    private static void ApplySceneBounds(MapJsonUpdater.MapData map)
    {
        var bounds = SceneBounds();
        if (!bounds.HasValue)
            return;

        map.size.width = System.Math.Round(bounds.Value.size.x, 2);
        map.size.height = System.Math.Round(bounds.Value.size.z, 2);
    }

    // ── 스폰 지점 자동 배치 ──
    //
    // 큰 맵에 스폰을 손으로 찍는 것은 현실적이지 않다(500x500 맵에 몬스터 수백 마리).
    // 배치는 반드시 navmesh 안이어야 한다 — 서버는 스폰 좌표를 navmesh 로 검증하고
    // (Map::ValidateMapDataOnNavMesh) 벗어난 지점은 몬스터 스폰이 실패한다.

    /// <summary>흩뿌리기가 만든 컨테이너 이름의 접두사. 다시 뿌릴 때 통째로 지우는 표식이다.</summary>
    public const string ScatterContainerPrefix = "Scattered_";

    // 창이 들고 있는 편집 버퍼라 리컴파일 후에도 값이 남도록 직렬화 대상으로 둔다
    // (NavMeshBakeSettings 와 같은 취급).
    [System.Serializable]
    public class ScatterSettings
    {
        public SpawnType type = SpawnType.Monster;
        public int count = 200;
        public float minSpacing = 5f;     // 점 사이 최소 거리(m). 0 이면 검사하지 않는다.
        public float edgeMargin = 1f;     // navmesh 가장자리/장애물에서 띄울 거리(m).
        public int seed = 0;              // 0 이면 매번 다른 배치.
        public bool replaceExisting = true;

        public int monsterId;
        public int bossId;
        public int spawnInterval = 30;
        public int spawnDelay;

        // 비어 있지 않으면 지점마다 여기서 무작위로 고른다(monsterId 대신).
        public int[] monsterIdPool;
    }

    /// <summary>
    /// navmesh 위에 스폰 지점을 무작위로 배치한다. 실제로 놓인 개수는 요청보다 적을 수 있다
    /// (최소 간격이 빡빡하면 자리가 없다) — 그 사실을 summary 에 담아 돌려준다.
    /// </summary>
    public static bool ScatterSpawnPoints(string sceneName, ScatterSettings settings, out string summary, out string error)
    {
        summary = null;

        NavMeshSampler sampler = NavMeshSampler.Load(NavMeshPathOf(sceneName), out error);
        if (sampler == null)
            return false;

        List<Vector3> points;
        try
        {
            points = sampler.Scatter(settings.count, settings.minSpacing, settings.edgeMargin, settings.seed,
                progress => !EditorUtility.DisplayCancelableProgressBar(
                    "Scatter spawns", $"Placing {settings.type} spawns on the navmesh…", progress));
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        if (points.Count == 0)
        {
            error = "navmesh 위에 놓을 자리를 찾지 못했습니다. Edge margin 을 줄여 보세요.";
            return false;
        }

        // 배치 전체를 되돌리기 한 번으로 묶는다.
        Undo.IncrementCurrentGroup();
        Undo.SetCurrentGroupName($"Scatter {settings.type} spawns");
        int undoGroup = Undo.GetCurrentGroup();

        int removed = settings.replaceExisting ? RemoveSpawnsOfType(settings.type) : 0;

        // 마커는 컨테이너 하나 밑에 모은다. 컨테이너만 Undo 에 등록하면 자식 수천 개를
        // 하나씩 등록하지 않아도 되고, 다시 뿌릴 때도 통째로 지울 수 있다.
        var container = new GameObject($"{ScatterContainerPrefix}{settings.type}");
        Undo.RegisterCreatedObjectUndo(container, "Scatter spawns");
        container.transform.SetParent(MapJsonUpdater.GetOrCreateGroup(MapJsonUpdater.SpawnGroupName), false);

        var others = Object.FindObjectsOfType<SpawnPoint>(true);
        int existing = others.Count(s => s.spawnType == settings.type);

        // id 를 미리 발급해 둔다. 0 으로 두면 스캔(MapJsonUpdater.UpdateSpawns)이 마커마다
        // Undo.RecordObject + SetDirty 로 id 를 써 넣는데, 수천 개면 그것만으로 몇 초가 걸린다.
        // 스캔이 그대로 유지하도록 '기존 id 최대값'과 '전체 개수'를 모두 넘겨서 시작한다
        // (id 가 없는 기존 마커는 스캔이 1부터 채우므로 개수 위에서 시작해야 겹치지 않는다).
        int nextId = Mathf.Max(others.Length + points.Count,
                               others.Select(s => s.id).DefaultIfEmpty(0).Max()) + 1;

        var rng = settings.seed == 0 ? new System.Random() : new System.Random(settings.seed);

        for (int i = 0; i < points.Count; i++)
        {
            var comp = MapJsonUpdater.NewSpawnMarker(settings.type, points[i],
                $"{settings.type}Spawn_{existing + i + 1}", container.transform);

            comp.id = nextId + i;

            // 타입에 안 쓰이는 필드는 0 으로 둔다(SpawnPoint 의 기본값 spawnInterval=30 이
            // 플레이어 스폰까지 따라 들어가면 Map.json 이 지저분해진다).
            comp.monsterId = 0;
            comp.bossId = 0;
            comp.spawnInterval = 0;
            comp.spawnDelay = 0;

            switch (settings.type)
            {
                case SpawnType.Monster:
                    comp.monsterId = PickMonsterId(settings, rng);
                    comp.spawnInterval = settings.spawnInterval;
                    break;
                case SpawnType.Boss:
                    comp.bossId = settings.bossId;
                    comp.spawnDelay = settings.spawnDelay;
                    break;
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        summary = $"{settings.type} 스폰 {points.Count}개 배치" +
                  (removed > 0 ? $" (기존 {removed}개 제거)" : "") +
                  (points.Count < settings.count
                      ? $" — {settings.count}개를 요청했지만 최소 간격 {settings.minSpacing}m 로는 자리가 부족합니다"
                      : "") +
                  // Auto sync 가 꺼져 있으면 마커는 씬에만 남는다. 수천 개를 뿌려 놓고
                  // Map.json 이 그대로인 것을 나중에 알게 되는 일이 없도록 여기서 말해 준다.
                  (MapJsonAutoSync.Enabled ? "" : " — Auto sync 가 꺼져 있습니다. Register 를 눌러야 Map.json 에 들어갑니다");
        return true;
    }

    private static int PickMonsterId(ScatterSettings settings, System.Random rng)
    {
        if (settings.monsterIdPool != null && settings.monsterIdPool.Length > 0)
            return settings.monsterIdPool[rng.Next(settings.monsterIdPool.Length)];
        return settings.monsterId;
    }

    private static int RemoveSpawnsOfType(SpawnType type)
    {
        var targets = Object.FindObjectsOfType<SpawnPoint>(true).Where(s => s.spawnType == type).ToArray();
        if (targets.Length == 0)
            return 0;

        var toDestroy = new List<GameObject>();
        var covered = new HashSet<SpawnPoint>();

        // 흩뿌리기 컨테이너는 통째로 지운다(수천 개를 하나씩 지우면 눈에 띄게 느리다).
        // 다른 타입 마커가 섞여 들어간 컨테이너는 건드리지 않는다.
        foreach (Transform parent in targets
            .Select(s => s.transform.parent)
            .Where(p => p != null && p.name.StartsWith(ScatterContainerPrefix))
            .Distinct())
        {
            var children = parent.GetComponentsInChildren<SpawnPoint>(true);
            if (children.Any(s => s.spawnType != type))
                continue;

            foreach (var child in children)
                covered.Add(child);
            toDestroy.Add(parent.gameObject);
        }

        foreach (var spawn in targets)
        {
            if (!covered.Contains(spawn))
                toDestroy.Add(spawn.gameObject);
        }

        foreach (var go in toDestroy)
            Undo.DestroyObjectImmediate(go);

        return targets.Length;
    }

    /// <summary>monster.json 의 몬스터 목록(자동 배치에서 id 를 고르는 데 쓴다).</summary>
    public struct MonsterEntry
    {
        public int id;
        public string name;
        public int level;
    }

    public static List<MonsterEntry> LoadMonsterCatalog()
    {
        var list = new List<MonsterEntry>();

        string dir = GameDataDir();
        if (dir == null)
            return list;

        string path = Path.Combine(dir, "monster.json");
        if (!File.Exists(path))
            return list;

        try
        {
            var array = Newtonsoft.Json.Linq.JArray.Parse(File.ReadAllText(path));
            foreach (var entry in array)
            {
                list.Add(new MonsterEntry
                {
                    id = entry.Value<int>("id"),
                    name = entry.Value<string>("name"),
                    level = entry.Value<int>("level"),
                });
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MapTool] monster.json 을 읽지 못했습니다: {e.Message}");
        }

        return list;
    }
}

/// <summary>NavMesh 굽기 파라미터. 서버 이동/충돌 판정이 이 값에 직접 좌우된다.</summary>
[System.Serializable]
public class NavMeshBakeSettings
{
    public float cellSize = 0.3f;
    public float cellHeight = 0.2f;
    public float walkableSlopeAngle = 45.0f;
    public float walkableHeight = 2.0f;
    public float walkableRadius = 0.6f;
    public float walkableClimb = 0.9f;
    public float minRegionArea = 8.0f;
    public float mergeRegionArea = 20.0f;
    public float maxSimplificationError = 1.3f;
    public float maxEdgeLen = 12.0f;
    public float detailSampleDistance = 6.0f;
    public float detailSampleMaxError = 1.0f;

    public void CopyFrom(NavMeshBakeSettings other)
    {
        cellSize = other.cellSize;
        cellHeight = other.cellHeight;
        walkableSlopeAngle = other.walkableSlopeAngle;
        walkableHeight = other.walkableHeight;
        walkableRadius = other.walkableRadius;
        walkableClimb = other.walkableClimb;
        minRegionArea = other.minRegionArea;
        mergeRegionArea = other.mergeRegionArea;
        maxSimplificationError = other.maxSimplificationError;
        maxEdgeLen = other.maxEdgeLen;
        detailSampleDistance = other.detailSampleDistance;
        detailSampleMaxError = other.detailSampleMaxError;
    }
}
