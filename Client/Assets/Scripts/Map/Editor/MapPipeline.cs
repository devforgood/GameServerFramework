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
            error = "NavMesh 생성에 실패했습니다. 콘솔 로그를 확인하세요.";
            return false;
        }

        AssetDatabase.Refresh();
        return true;
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
