using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// 씬과 Map.json을 양방향으로 연동하는 맵 디자인 툴.
// - 씬 스캔: 씬에 배치된 Gate / SpawnPoint / MapObjectMarker 컴포넌트를 찾아 Map.json에 기록
// - 씬 빌드: Map.json의 게이트/스폰/오브젝트 데이터를 씬에 마커 오브젝트로 생성
// - 배치 툴박스: 게이트/스폰/오브젝트 마커를 씬 뷰 위치에 바로 생성
// - NavMesh 연동: GeneratedNavMeshes의 bin 파일을 GameData로 복사하고 navmesh_path를 갱신
public class MapJsonUpdater : EditorWindow
{
    private const string MarkerRootName = "MapDesign";
    public const string GateGroupName = "Gates";
    public const string SpawnGroupName = "SpawnPoints";
    public const string ObjectGroupName = "Objects";

    private string mapJsonPath = "";
    private List<MapData> mapDataList = new List<MapData>();

    // AutoSync가 열려 있는 창의 미저장 편집(맵 메타데이터 등)을 덮어쓰지 않도록 상태를 공유한다.
    public string SharedJsonPath => mapJsonPath;
    public List<MapData> SharedMapList => mapDataList;
    private Vector2 scrollPosition;
    private bool showAllMaps = false;
    private readonly HashSet<int> expandedMaps = new HashSet<int>();

    // ---------------------------------------------------------------------
    // 데이터 모델 (Map.json 스키마와 동일한 필드 순서 유지)
    // 알 수 없는 필드는 JsonExtensionData로 보존하여 저장 시 유실되지 않게 한다.
    // ---------------------------------------------------------------------

    [System.Serializable]
    public class Vec3
    {
        public double x;
        public double y;
        public double z;

        public Vec3() { }

        public Vec3(Vector3 vector)
        {
            x = System.Math.Round(vector.x, 2);
            y = System.Math.Round(vector.y, 2);
            z = System.Math.Round(vector.z, 2);
        }

        public Vector3 ToVector3()
        {
            return new Vector3((float)x, (float)y, (float)z);
        }
    }

    [System.Serializable]
    public class GateInfo
    {
        public int id;
        public string name;
        public Vec3 position;
        public int target_map_id;
        public int target_gate_id;
        public int required_level;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    [System.Serializable]
    public class SpawnPointInfo
    {
        public Vec3 position;
        public int monster_id;
        public int spawn_interval;
        public int boss_id;
        public int spawn_delay;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    [System.Serializable]
    public class MapSpawnPoints
    {
        public List<SpawnPointInfo> player_spawn = new List<SpawnPointInfo>();
        public List<SpawnPointInfo> monster_spawn = new List<SpawnPointInfo>();
        public List<SpawnPointInfo> boss_spawn = new List<SpawnPointInfo>();
    }

    [System.Serializable]
    public class StaticObjectInfo
    {
        public int id;
        public string type;
        public string name;
        public Vec3 position;
        public Vec3 size;
        public bool collision;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? damage;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? loot_table_id;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    [System.Serializable]
    public class MovableObjectInfo
    {
        public int id;
        public string type;
        public string name;
        public Vec3 position;
        public double movement_range;
        public double movement_speed;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public List<Vec3> patrol_path;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    [System.Serializable]
    public class MapObjects
    {
        public List<StaticObjectInfo> static_objects = new List<StaticObjectInfo>();
        public List<MovableObjectInfo> movable_objects = new List<MovableObjectInfo>();
    }

    [System.Serializable]
    public class MapSize
    {
        public double width;
        public double height;
    }

    [System.Serializable]
    public class MapData
    {
        public int id;
        public string name;
        public string scene;
        public string name_id;
        public string desc_id;
        public int game_mode_id;
        public MapSize size = new MapSize();
        public List<GateInfo> gates = new List<GateInfo>();
        public MapSpawnPoints spawn_points = new MapSpawnPoints();
        public MapObjects objects = new MapObjects();
        public string navmesh_path;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    // ---------------------------------------------------------------------
    // 윈도우 / 로드 / 저장
    // ---------------------------------------------------------------------

    [MenuItem("Tools/Map JSON Updater")]
    public static void ShowWindow()
    {
        GetWindow<MapJsonUpdater>("Map JSON Updater");
    }

    private void OnEnable()
    {
        LoadMapJson();
    }

    private string GameDataDir => string.IsNullOrEmpty(mapJsonPath) ? null : Path.GetDirectoryName(mapJsonPath);

    private void LoadMapJson()
    {
        string found = FindMapJsonPath();
        if (found == null)
        {
            mapJsonPath = "";
            Debug.LogError("Map.json not found. 프로젝트 루트의 GameData/Map.json 경로를 확인하세요.");
            return;
        }

        mapJsonPath = found;
        mapDataList = LoadMapList(mapJsonPath);
        Debug.Log($"Map.json loaded from: {mapJsonPath} (maps: {mapDataList.Count})");
    }

    // GameData 디렉토리에서 Map.json 찾기 (에디터 실행 위치에 따라 여러 경로 시도). 없으면 null.
    public static string FindMapJsonPath()
    {
        string[] possiblePaths = {
            Path.Combine(Application.dataPath, "..", "..", "..", "GameData", "Map.json"), // Client/Assets -> GameData
            Path.Combine(Application.dataPath, "..", "..", "GameData", "Map.json"),      // Assets -> GameData
            Path.Combine(Application.dataPath, "..", "GameData", "Map.json"),            // Assets -> GameData
            Path.Combine(Directory.GetCurrentDirectory(), "GameData", "Map.json")       // 현재 디렉토리 -> GameData
        };

        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
                return Path.GetFullPath(path);
        }
        return null;
    }

    public static List<MapData> LoadMapList(string path)
    {
        string jsonContent = File.ReadAllText(path);
        var list = JsonConvert.DeserializeObject<List<MapData>>(jsonContent) ?? new List<MapData>();
        foreach (var map in list)
            EnsureDefaults(map);
        return list;
    }

    // JSON에 필드가 null/누락으로 들어있어도 툴이 NRE 없이 동작하도록 기본값을 채운다.
    private static void EnsureDefaults(MapData map)
    {
        if (map.size == null) map.size = new MapSize();
        if (map.gates == null) map.gates = new List<GateInfo>();
        if (map.spawn_points == null) map.spawn_points = new MapSpawnPoints();
        if (map.spawn_points.player_spawn == null) map.spawn_points.player_spawn = new List<SpawnPointInfo>();
        if (map.spawn_points.monster_spawn == null) map.spawn_points.monster_spawn = new List<SpawnPointInfo>();
        if (map.spawn_points.boss_spawn == null) map.spawn_points.boss_spawn = new List<SpawnPointInfo>();
        if (map.objects == null) map.objects = new MapObjects();
        if (map.objects.static_objects == null) map.objects.static_objects = new List<StaticObjectInfo>();
        if (map.objects.movable_objects == null) map.objects.movable_objects = new List<MovableObjectInfo>();
    }

    private void SaveMapJson()
    {
        if (string.IsNullOrEmpty(mapJsonPath))
        {
            Debug.LogError("Map JSON path is not set!");
            return;
        }

        try
        {
            SaveMapList(mapJsonPath, mapDataList);
            DeployMapJsonCopies(mapJsonPath);
            Debug.Log($"Map JSON saved to: {mapJsonPath} (+ Client/Game/UnitTest 사본 복사)\n" +
                      "스키마(필드 구조)가 바뀐 경우에는 GameDataFlow/GameDataFlow.py 로 코드젠도 갱신하세요.");
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save Map JSON: {e.Message}");
        }
    }

    public static void SaveMapList(string path, List<MapData> list)
    {
        ValidateMapReferences(list);
        string jsonContent = JsonConvert.SerializeObject(list, Formatting.Indented);
        BackupBeforeWrite(path);
        File.WriteAllText(path, jsonContent);
    }

    // 저장 직전 기존 Map.json을 .bak 1벌로 보존한다.
    // 자동 저장은 undo 결과도 즉시 기록하므로, 실수로 덮어써도 직전 상태로 되돌릴 수 있게 한다.
    public static void BackupBeforeWrite(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Copy(path, path + ".bak", true);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Map.json 백업(.bak) 실패: {e.Message}");
        }
    }

    // 게이트 참조 무결성 검사: dangling target_map_id/target_gate_id, 맵 내 게이트 id 중복을 경고한다.
    // 저장을 막지는 않는다(작업 중간 상태 저장 허용). 발견한 문제 수를 반환한다.
    public static int ValidateMapReferences(List<MapData> maps)
    {
        int problems = 0;
        var mapById = new Dictionary<int, MapData>();
        foreach (var map in maps)
        {
            if (mapById.ContainsKey(map.id))
            {
                Debug.LogWarning($"[MapValidate] 맵 id {map.id} 중복: '{mapById[map.id].name}' / '{map.name}'");
                problems++;
            }
            else
            {
                mapById[map.id] = map;
            }
        }

        foreach (var map in maps)
        {
            var seenGateIds = new HashSet<int>();
            foreach (var gate in map.gates)
            {
                if (!seenGateIds.Add(gate.id))
                {
                    Debug.LogWarning($"[MapValidate] 맵 '{map.name}'({map.id}) 안에 게이트 id {gate.id} 중복 (게이트 '{gate.name}')");
                    problems++;
                }

                if (!mapById.TryGetValue(gate.target_map_id, out var targetMap))
                {
                    Debug.LogWarning($"[MapValidate] 맵 '{map.name}'({map.id}) 게이트 '{gate.name}'(id:{gate.id}) → 존재하지 않는 맵 {gate.target_map_id} 참조");
                    problems++;
                }
                else if (!targetMap.gates.Any(g => g.id == gate.target_gate_id))
                {
                    Debug.LogWarning($"[MapValidate] 맵 '{map.name}'({map.id}) 게이트 '{gate.name}'(id:{gate.id}) → 맵 '{targetMap.name}'({targetMap.id})에 없는 게이트 {gate.target_gate_id} 참조");
                    problems++;
                }
            }
        }

        if (problems > 0)
            Debug.LogWarning($"[MapValidate] 게이트 참조 문제 {problems}건 발견 (위 경고 참조). 저장은 진행됩니다.");
        return problems;
    }

    // Map.json을 소비처 런타임 GameData 폴더로 그대로 복사한다.
    // (GameDataFlow.py의 JSON verbatim 복사와 동일 대상. 코드젠/navmesh 배포는 GameDataFlow 몫)
    public static void DeployMapJsonCopies(string sourcePath)
    {
        string gameDataDir = Path.GetDirectoryName(sourcePath);
        string root = Path.GetDirectoryName(gameDataDir); // 리포지토리 루트
        string[] targetDirs = {
            Path.Combine(root, "Client", "Assets", "Resources", "GameData"),
            Path.Combine(root, "Game", "GameData"),
            Path.Combine(root, "UnitTest", "GameData"),
        };

        foreach (string dir in targetDirs)
        {
            if (!Directory.Exists(dir))
                continue; // 대상 폴더가 없으면 배포 대상이 아니다

            try
            {
                File.Copy(sourcePath, Path.Combine(dir, Path.GetFileName(sourcePath)), true);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Map.json 사본 복사 실패 ({dir}): {e.Message}");
            }
        }
    }

    // ---------------------------------------------------------------------
    // GUI
    // ---------------------------------------------------------------------

    private void OnGUI()
    {
        GUILayout.Label("Map JSON Updater", EditorStyles.boldLabel);

        if (string.IsNullOrEmpty(mapJsonPath))
        {
            EditorGUILayout.HelpBox("Map.json not found. Please check GameData directory.", MessageType.Error);
            if (GUILayout.Button("Retry"))
                LoadMapJson();
            return;
        }

        EditorGUILayout.LabelField("Map JSON Path:", mapJsonPath);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Reload Map JSON"))
            LoadMapJson();
        if (GUILayout.Button("Save Map JSON"))
            SaveMapJson();
        EditorGUILayout.EndHorizontal();

        // 자동 동기화 토글 (씬 열 때 JSON→씬 배치, 마커 변경 시 Map.json 자동 저장)
        bool auto = MapJsonAutoSync.Enabled;
        bool newAuto = EditorGUILayout.ToggleLeft(
            "Auto Sync — 씬을 열면 Map.json 기준으로 마커 배치, 마커 변경 시 Map.json 자동 저장", auto);
        if (newAuto != auto)
            MapJsonAutoSync.Enabled = newAuto;

        EditorGUILayout.HelpBox("저장 시 Map.json 사본은 Client/Game/UnitTest에 자동 복사됩니다.\n" +
                                "코드젠(Gamedata.cs/gamedata.h)과 navmesh 배포는 스키마 변경 시 GameDataFlow.py를 실행하세요.", MessageType.Info);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        DrawCurrentSceneSection();

        EditorGUILayout.Space();

        DrawAllMapsSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawCurrentSceneSection()
    {
        string sceneName = EditorSceneManager.GetActiveScene().name;

        EditorGUILayout.BeginVertical("box");
        GUI.backgroundColor = Color.green;
        EditorGUILayout.LabelField($"Current Scene: {sceneName}", EditorStyles.boldLabel);
        GUI.backgroundColor = Color.white;

        MapData map = FindMapForScene(sceneName);

        if (map == null)
        {
            EditorGUILayout.HelpBox($"'{sceneName}' 씬과 연결된 맵이 Map.json에 없습니다.", MessageType.Warning);
            if (GUILayout.Button("Create Map Entry for This Scene"))
            {
                map = CreateMapForScene(sceneName);
            }
            EditorGUILayout.EndVertical();
            return;
        }

        // --- 맵 메타데이터 편집 ---
        EditorGUILayout.LabelField($"Map ID: {map.id}");
        map.name = EditorGUILayout.TextField("Name", map.name);
        map.scene = EditorGUILayout.TextField("Scene", map.scene);
        map.name_id = EditorGUILayout.TextField("Name ID", map.name_id);
        map.desc_id = EditorGUILayout.TextField("Desc ID", map.desc_id);
        map.game_mode_id = EditorGUILayout.IntField("Game Mode ID", map.game_mode_id);

        EditorGUILayout.BeginHorizontal();
        map.size.width = EditorGUILayout.DoubleField("Size (W x H)", map.size.width);
        map.size.height = EditorGUILayout.DoubleField(map.size.height);
        if (GUILayout.Button("From Scene Bounds", GUILayout.Width(130)))
            CalcSizeFromSceneBounds(map);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- 씬 <-> JSON 동기화 ---
        EditorGUILayout.LabelField("Scene ⇔ JSON", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Scan Scene → JSON"))
            ScanScene(sceneName, map);
        if (GUILayout.Button("Build Scene ← JSON"))
            BuildSceneFromJson(map);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField($"Gates: {map.gates.Count}   " +
                                   $"Player: {map.spawn_points.player_spawn.Count}   " +
                                   $"Monster: {map.spawn_points.monster_spawn.Count}   " +
                                   $"Boss: {map.spawn_points.boss_spawn.Count}   " +
                                   $"Static: {map.objects.static_objects.Count}   " +
                                   $"Movable: {map.objects.movable_objects.Count}");

        EditorGUILayout.Space();

        // --- 배치 툴박스 ---
        EditorGUILayout.LabelField("Place Markers (씬 뷰 중심에 생성)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Gate"))
            CreateGateMarker();
        if (GUILayout.Button("+ Player Spawn"))
            CreateSpawnMarker(SpawnType.Player);
        if (GUILayout.Button("+ Monster Spawn"))
            CreateSpawnMarker(SpawnType.Monster);
        if (GUILayout.Button("+ Boss Spawn"))
            CreateSpawnMarker(SpawnType.Boss);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Static Object"))
            CreateObjectMarker(MapObjectKind.Static);
        if (GUILayout.Button("+ Movable Object"))
            CreateObjectMarker(MapObjectKind.Movable);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // --- NavMesh 상태 ---
        DrawNavMeshSection(map, sceneName);

        EditorGUILayout.EndVertical();
    }

    private void DrawNavMeshSection(MapData map, string sceneName)
    {
        EditorGUILayout.LabelField("NavMesh", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"navmesh_path: {(string.IsNullOrEmpty(map.navmesh_path) ? "(none)" : map.navmesh_path)}");

        string fileName = $"{sceneName}_navmesh.bin";
        string genPath = Path.Combine(Application.dataPath, "GeneratedNavMeshes", fileName);
        string gameDataPath = GameDataDir != null ? Path.Combine(GameDataDir, fileName) : null;

        bool genExists = File.Exists(genPath);
        bool gameDataExists = gameDataPath != null && File.Exists(gameDataPath);

        if (!genExists)
        {
            EditorGUILayout.HelpBox($"Assets/GeneratedNavMeshes/{fileName} 이 없습니다. NavMesh를 먼저 생성하세요.\n" +
                                    "(서버는 navmesh가 없으면 solo_navmesh로 폴백하여 씬과 불일치가 발생합니다)",
                                    MessageType.Warning);
            return;
        }

        if (!gameDataExists)
        {
            EditorGUILayout.HelpBox($"GameData/{fileName} 이 없습니다. 서버가 이 맵의 NavMesh를 로드하지 못합니다.", MessageType.Warning);
        }
        else
        {
            var genTime = File.GetLastWriteTimeUtc(genPath);
            var dstTime = File.GetLastWriteTimeUtc(gameDataPath);
            if (genTime > dstTime)
                EditorGUILayout.HelpBox("GeneratedNavMeshes의 NavMesh가 GameData보다 최신입니다. 복사가 필요합니다.", MessageType.Warning);
            else
                EditorGUILayout.HelpBox("GameData의 NavMesh가 최신 상태입니다.", MessageType.Info);
        }

        if (GUILayout.Button("Copy NavMesh → GameData & Update navmesh_path"))
        {
            File.Copy(genPath, gameDataPath, true);
            map.navmesh_path = fileName;
            Debug.Log($"NavMesh copied: {genPath} -> {gameDataPath}. navmesh_path='{fileName}' (Save Map JSON 필요)");
        }
    }

    private void DrawAllMapsSection()
    {
        showAllMaps = EditorGUILayout.Foldout(showAllMaps, $"All Maps ({mapDataList.Count})", true);
        if (!showAllMaps)
            return;

        string currentSceneName = EditorSceneManager.GetActiveScene().name;
        MapData toRemove = null;

        foreach (var map in mapDataList)
        {
            EditorGUILayout.BeginVertical("box");

            bool isCurrent = IsMapForScene(map, currentSceneName);
            string title = $"{map.name} (ID: {map.id})" + (isCurrent ? " [CURRENT]" : "");

            bool expanded = expandedMaps.Contains(map.id);
            bool newExpanded = EditorGUILayout.Foldout(expanded, title, true);
            if (newExpanded != expanded)
            {
                if (newExpanded) expandedMaps.Add(map.id);
                else expandedMaps.Remove(map.id);
            }

            if (newExpanded)
            {
                EditorGUILayout.LabelField($"Scene: {map.scene}   Game Mode: {map.game_mode_id}   Size: {map.size.width} x {map.size.height}");
                EditorGUILayout.LabelField($"Gates: {map.gates.Count}   " +
                                           $"Player: {map.spawn_points.player_spawn.Count}   " +
                                           $"Monster: {map.spawn_points.monster_spawn.Count}   " +
                                           $"Boss: {map.spawn_points.boss_spawn.Count}   " +
                                           $"Objects: {map.objects.static_objects.Count + map.objects.movable_objects.Count}");
                EditorGUILayout.LabelField($"NavMesh: {(string.IsNullOrEmpty(map.navmesh_path) ? "(none)" : map.navmesh_path)}");

                foreach (var gate in map.gates)
                {
                    EditorGUILayout.LabelField($"  Gate {gate.id}: {gate.name} → map {gate.target_map_id}, gate {gate.target_gate_id} (Lv.{gate.required_level})");
                }

                EditorGUILayout.BeginHorizontal();
                if (!isCurrent && GUILayout.Button("Open Scene", GUILayout.Width(100)))
                    OpenSceneForMap(map);
                if (GUILayout.Button("Delete Map", GUILayout.Width(100)))
                {
                    if (EditorUtility.DisplayDialog("Delete Map",
                            $"맵 '{map.name}' (ID: {map.id}) 항목을 Map.json에서 삭제할까요?\n" +
                            "다른 맵의 게이트가 이 맵을 참조 중이면 참조가 깨집니다.", "Delete", "Cancel"))
                    {
                        toRemove = map;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }

        if (toRemove != null)
        {
            // 이 맵을 참조하는 게이트가 있으면 경고 로그를 남긴다.
            foreach (var other in mapDataList.Where(m => m != toRemove))
            {
                foreach (var gate in other.gates.Where(g => g.target_map_id == toRemove.id))
                    Debug.LogWarning($"Map '{other.name}' gate '{gate.name}' references deleted map id {toRemove.id}.");
            }
            mapDataList.Remove(toRemove);
            Debug.Log($"Map '{toRemove.name}' removed. (Save Map JSON 필요)");
        }
    }

    // ---------------------------------------------------------------------
    // 씬 ⇔ 맵 매칭
    // ---------------------------------------------------------------------

    public static bool IsMapForScene(MapData map, string sceneName)
    {
        if (!string.IsNullOrEmpty(map.scene))
            return map.scene.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase);
        return map.name != null && map.name.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    public static MapData FindMapForScene(List<MapData> maps, string sceneName)
    {
        return maps.FirstOrDefault(m => IsMapForScene(m, sceneName));
    }

    private MapData FindMapForScene(string sceneName)
    {
        return FindMapForScene(mapDataList, sceneName);
    }

    private MapData CreateMapForScene(string sceneName)
    {
        var map = new MapData
        {
            id = GetNextMapId(),
            name = sceneName,
            scene = sceneName,
            name_id = $"map_{sceneName.ToLower().Replace(" ", "_")}_name",
            desc_id = $"map_{sceneName.ToLower().Replace(" ", "_")}_desc",
            game_mode_id = 1, // 기본값: Field
            size = new MapSize { width = 1000, height = 1000 },
        };
        mapDataList.Add(map);
        Debug.Log($"Created new map entry: {sceneName} (ID: {map.id})");
        return map;
    }

    private int GetNextMapId()
    {
        return mapDataList.Count == 0 ? 1 : mapDataList.Max(m => m.id) + 1;
    }

    private void OpenSceneForMap(MapData map)
    {
        string sceneName = string.IsNullOrEmpty(map.scene) ? map.name : map.scene;
        string[] guids = AssetDatabase.FindAssets($"t:Scene {sceneName}");
        string scenePath = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => Path.GetFileNameWithoutExtension(p)
                .Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));

        if (scenePath == null)
        {
            Debug.LogWarning($"Scene asset '{sceneName}' not found for map '{map.name}'.");
            return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            EditorSceneManager.OpenScene(scenePath);
    }

    // ---------------------------------------------------------------------
    // 씬 스캔 → JSON
    // ---------------------------------------------------------------------

    private void ScanScene(string sceneName, MapData map)
    {
        if (map == null)
            map = CreateMapForScene(sceneName);

        ScanSceneIntoMap(map, mapDataList, sceneName);
        ValidateMapReferences(mapDataList);
        Debug.Log($"Scan complete for '{sceneName}': {map.gates.Count} gates. (Save Map JSON 필요)");
    }

    // 씬의 마커를 스캔하여 map에 기록한다 (MapJsonAutoSync에서도 사용).
    public static void ScanSceneIntoMap(MapData map, List<MapData> allMaps, string sceneName)
    {
        // 컴포넌트 기반 스캔 (태그 불필요). 이름순 정렬로 저장 결과를 결정적으로 만든다.
        var gates = Object.FindObjectsOfType<Gate>(true)
            .OrderBy(g => g.name, System.StringComparer.OrdinalIgnoreCase).ToArray();
        var spawns = Object.FindObjectsOfType<SpawnPoint>(true)
            .OrderBy(s => s.name, System.StringComparer.OrdinalIgnoreCase).ToArray();
        var objects = Object.FindObjectsOfType<MapObjectMarker>(true)
            .OrderBy(o => o.name, System.StringComparer.OrdinalIgnoreCase).ToArray();

        UpdateGates(map, gates);
        UpdateSpawns(map, spawns);
        UpdateObjects(map, objects, allMaps);
        DetectNavMesh(map, sceneName);
    }

    private static void UpdateGates(MapData map, Gate[] gateComponents)
    {
        var oldGates = map.gates;
        map.gates = new List<GateInfo>();

        // 게이트 id는 컴포넌트가 직접 보유한다(MapObjectMarker.objectId와 동일 방식).
        // id는 맵 내부에서 지역적으로 유일하면 된다(target_gate_id는 (target_map_id, gate.id) 쌍으로 해석됨).
        // 0(또는 중복)이면 새 id를 발급하고 컴포넌트에 다시 써서 고정한다.
        var claimed = new HashSet<int>();
        int nextId = 1;

        foreach (var comp in gateComponents)
        {
            string gateName = string.IsNullOrEmpty(comp.gateName) ? comp.gameObject.name : comp.gateName;

            int id = comp.id;
            if (id <= 0 || claimed.Contains(id))
            {
                while (claimed.Contains(nextId)) nextId++;
                id = nextId;
            }
            claimed.Add(id);
            AssignGateId(comp, id);

            var old = oldGates.Find(g => g.id == id);

            map.gates.Add(new GateInfo
            {
                id = id,
                name = gateName,
                position = new Vec3(comp.transform.position),
                target_map_id = comp.targetMapId,
                target_gate_id = comp.targetGateId,
                required_level = comp.requiredLevel,
                extra = old?.extra
            });
        }
    }

    // 다음 스캔에서도 id가 유지되도록 컴포넌트에 id를 기록해 둔다.
    private static void AssignGateId(Gate gate, int id)
    {
        if (gate.id == id)
            return;
        Undo.RecordObject(gate, "Assign Gate Id");
        gate.id = id;
        EditorUtility.SetDirty(gate);
    }

    private static void UpdateSpawns(MapData map, SpawnPoint[] spawnComponents)
    {
        map.spawn_points.player_spawn.Clear();
        map.spawn_points.monster_spawn.Clear();
        map.spawn_points.boss_spawn.Clear();

        foreach (var comp in spawnComponents)
        {
            var info = new SpawnPointInfo
            {
                position = new Vec3(comp.transform.position),
                monster_id = comp.monsterId,
                spawn_interval = comp.spawnInterval,
                boss_id = comp.bossId,
                spawn_delay = comp.spawnDelay
            };

            switch (comp.spawnType)
            {
                case SpawnType.Player:
                    map.spawn_points.player_spawn.Add(info);
                    break;
                case SpawnType.Monster:
                    if (comp.monsterId == 0)
                        Debug.LogWarning($"Monster spawn '{comp.name}' has monsterId=0.");
                    map.spawn_points.monster_spawn.Add(info);
                    break;
                case SpawnType.Boss:
                    if (comp.bossId == 0)
                        Debug.LogWarning($"Boss spawn '{comp.name}' has bossId=0.");
                    map.spawn_points.boss_spawn.Add(info);
                    break;
            }
        }
    }

    private static void UpdateObjects(MapData map, MapObjectMarker[] markers, List<MapData> allMaps)
    {
        var oldStatics = map.objects.static_objects;
        var oldMovables = map.objects.movable_objects;
        map.objects = new MapObjects();

        // 오브젝트 id는 전체 맵에서 유일하게 유지한다.
        int nextId = allMaps
            .SelectMany(m => m.objects.static_objects.Select(o => o.id)
                .Concat(m.objects.movable_objects.Select(o => o.id)))
            .DefaultIfEmpty(0).Max() + 1;

        foreach (var marker in markers)
        {
            string objName = marker.gameObject.name;

            if (marker.kind == MapObjectKind.Static)
            {
                var old = oldStatics.Find(o => o.name != null && o.name.Equals(objName, System.StringComparison.OrdinalIgnoreCase));
                int id = marker.objectId != 0 ? marker.objectId : (old != null ? old.id : nextId++);
                AssignMarkerId(marker, id);

                map.objects.static_objects.Add(new StaticObjectInfo
                {
                    id = id,
                    type = marker.objectType,
                    name = objName,
                    position = new Vec3(marker.transform.position),
                    size = new Vec3(marker.transform.localScale),
                    collision = marker.collision,
                    damage = marker.damage != 0 ? marker.damage : (int?)null,
                    loot_table_id = marker.lootTableId != 0 ? marker.lootTableId : (int?)null,
                    extra = old?.extra
                });
            }
            else
            {
                var old = oldMovables.Find(o => o.name != null && o.name.Equals(objName, System.StringComparison.OrdinalIgnoreCase));
                int id = marker.objectId != 0 ? marker.objectId : (old != null ? old.id : nextId++);
                AssignMarkerId(marker, id);

                map.objects.movable_objects.Add(new MovableObjectInfo
                {
                    id = id,
                    type = marker.objectType,
                    name = objName,
                    position = new Vec3(marker.transform.position),
                    movement_range = System.Math.Round(marker.movementRange, 2),
                    movement_speed = System.Math.Round(marker.movementSpeed, 2),
                    patrol_path = (marker.patrolPath != null && marker.patrolPath.Count > 0)
                        ? marker.patrolPath.Select(p => new Vec3(p)).ToList()
                        : null,
                    extra = old?.extra
                });
            }
        }
    }

    // 다음 스캔에서 이름이 바뀌어도 id가 유지되도록 마커에 id를 기록해 둔다.
    private static void AssignMarkerId(MapObjectMarker marker, int id)
    {
        if (marker.objectId == id)
            return;
        Undo.RecordObject(marker, "Assign Map Object Id");
        marker.objectId = id;
        EditorUtility.SetDirty(marker);
    }

    private static void DetectNavMesh(MapData map, string sceneName)
    {
        string fileName = $"{sceneName}_navmesh.bin";
        string genPath = Path.Combine(Application.dataPath, "GeneratedNavMeshes", fileName);
        if (File.Exists(genPath))
            map.navmesh_path = fileName;
    }

    private void CalcSizeFromSceneBounds(MapData map)
    {
        var renderers = FindObjectsOfType<Renderer>();
        if (renderers.Length == 0)
        {
            Debug.LogWarning("씬에 Renderer가 없어 크기를 계산할 수 없습니다.");
            return;
        }

        Bounds bounds = renderers[0].bounds;
        foreach (var r in renderers)
            bounds.Encapsulate(r.bounds);

        map.size.width = System.Math.Round(bounds.size.x, 2);
        map.size.height = System.Math.Round(bounds.size.z, 2);
        Debug.Log($"Map size set from scene bounds: {map.size.width} x {map.size.height}");
    }

    // ---------------------------------------------------------------------
    // JSON → 씬 빌드
    // ---------------------------------------------------------------------

    private void BuildSceneFromJson(MapData map)
    {
        // 기존 마커가 있으면 교체 여부 확인
        var existing = new List<GameObject>();
        existing.AddRange(FindObjectsOfType<Gate>(true).Select(c => c.gameObject));
        existing.AddRange(FindObjectsOfType<SpawnPoint>(true).Select(c => c.gameObject));
        existing.AddRange(FindObjectsOfType<MapObjectMarker>(true).Select(c => c.gameObject));
        existing = existing.Distinct().ToList();

        if (existing.Count > 0)
        {
            if (!EditorUtility.DisplayDialog("Build Scene From JSON",
                    $"씬에 이미 {existing.Count}개의 마커(Gate/SpawnPoint/MapObjectMarker)가 있습니다.\n" +
                    "모두 삭제하고 JSON 데이터로 다시 생성할까요?", "Replace", "Cancel"))
                return;

            foreach (var go in existing)
                Undo.DestroyObjectImmediate(go);
        }

        int created = 0;

        // 게이트 생성
        foreach (var gate in map.gates)
        {
            var go = new GameObject(gate.name);
            Undo.RegisterCreatedObjectUndo(go, "Build Map From JSON");
            go.transform.SetParent(GetOrCreateGroup(GateGroupName), true);
            go.transform.position = gate.position != null ? gate.position.ToVector3() : Vector3.zero;
            TrySetTag(go, "Gate");

            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 3f, 2f);
            box.center = new Vector3(0f, 1.5f, 0f);

            var comp = go.AddComponent<Gate>();
            comp.id = gate.id;
            comp.gateName = gate.name;
            comp.targetMapId = gate.target_map_id;
            comp.targetGateId = gate.target_gate_id;
            comp.requiredLevel = gate.required_level;
            created++;
        }

        // 스폰 포인트 생성
        created += BuildSpawns(map.spawn_points.player_spawn, SpawnType.Player, "PlayerSpawn");
        created += BuildSpawns(map.spawn_points.monster_spawn, SpawnType.Monster, "MonsterSpawn");
        created += BuildSpawns(map.spawn_points.boss_spawn, SpawnType.Boss, "BossSpawn");

        // 맵 오브젝트 생성
        foreach (var obj in map.objects.static_objects)
        {
            var go = new GameObject(string.IsNullOrEmpty(obj.name) ? $"StaticObject_{obj.id}" : obj.name);
            Undo.RegisterCreatedObjectUndo(go, "Build Map From JSON");
            go.transform.SetParent(GetOrCreateGroup(ObjectGroupName), true);
            go.transform.position = obj.position != null ? obj.position.ToVector3() : Vector3.zero;
            if (obj.size != null)
                go.transform.localScale = obj.size.ToVector3();

            var marker = go.AddComponent<MapObjectMarker>();
            marker.kind = MapObjectKind.Static;
            marker.objectId = obj.id;
            marker.objectType = obj.type;
            marker.collision = obj.collision;
            marker.damage = obj.damage ?? 0;
            marker.lootTableId = obj.loot_table_id ?? 0;
            created++;
        }

        foreach (var obj in map.objects.movable_objects)
        {
            var go = new GameObject(string.IsNullOrEmpty(obj.name) ? $"MovableObject_{obj.id}" : obj.name);
            Undo.RegisterCreatedObjectUndo(go, "Build Map From JSON");
            go.transform.SetParent(GetOrCreateGroup(ObjectGroupName), true);
            go.transform.position = obj.position != null ? obj.position.ToVector3() : Vector3.zero;

            var marker = go.AddComponent<MapObjectMarker>();
            marker.kind = MapObjectKind.Movable;
            marker.objectId = obj.id;
            marker.objectType = obj.type;
            marker.movementRange = (float)obj.movement_range;
            marker.movementSpeed = (float)obj.movement_speed;
            if (obj.patrol_path != null)
                marker.patrolPath = obj.patrol_path.Select(p => p.ToVector3()).ToList();
            created++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Build complete: {created} marker objects created from map '{map.name}'. (씬 저장 필요)");
    }

    private int BuildSpawns(List<SpawnPointInfo> spawns, SpawnType type, string baseName)
    {
        for (int i = 0; i < spawns.Count; i++)
        {
            var info = spawns[i];
            var go = new GameObject($"{baseName}_{i + 1}");
            Undo.RegisterCreatedObjectUndo(go, "Build Map From JSON");
            go.transform.SetParent(GetOrCreateGroup(SpawnGroupName), true);
            go.transform.position = info.position != null ? info.position.ToVector3() : Vector3.zero;
            TrySetTag(go, "Spawn");

            var comp = go.AddComponent<SpawnPoint>();
            comp.spawnType = type;
            comp.monsterId = info.monster_id;
            comp.spawnInterval = info.spawn_interval;
            comp.bossId = info.boss_id;
            comp.spawnDelay = info.spawn_delay;
        }
        return spawns.Count;
    }

    // ---------------------------------------------------------------------
    // 배치 툴박스
    // ---------------------------------------------------------------------

    private void CreateGateMarker()
    {
        var go = new GameObject("NewGate");
        Undo.RegisterCreatedObjectUndo(go, "Create Gate Marker");
        go.transform.SetParent(GetOrCreateGroup(GateGroupName), true);
        go.transform.position = GetPlacementPosition();
        TrySetTag(go, "Gate");

        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(2f, 3f, 2f);
        box.center = new Vector3(0f, 1.5f, 0f);

        // id/gateName은 비워 둔다. Scan 시 id가 자동 발급되고, 이름은 GameObject 이름으로 폴백한다.
        // 목적지는 인스펙터에서 Target Map Id / Target Gate Id(Map.json 스키마)를 채운다.
        go.AddComponent<Gate>();

        FinishMarkerCreation(go);
    }

    private void CreateSpawnMarker(SpawnType type)
    {
        var go = new GameObject($"New{type}Spawn");
        Undo.RegisterCreatedObjectUndo(go, "Create Spawn Marker");
        go.transform.SetParent(GetOrCreateGroup(SpawnGroupName), true);
        go.transform.position = GetPlacementPosition();
        TrySetTag(go, "Spawn");

        var comp = go.AddComponent<SpawnPoint>();
        comp.spawnType = type;

        FinishMarkerCreation(go);
    }

    private void CreateObjectMarker(MapObjectKind kind)
    {
        var go = new GameObject(kind == MapObjectKind.Static ? "NewStaticObject" : "NewMovableObject");
        Undo.RegisterCreatedObjectUndo(go, "Create Map Object Marker");
        go.transform.SetParent(GetOrCreateGroup(ObjectGroupName), true);
        go.transform.position = GetPlacementPosition();

        var marker = go.AddComponent<MapObjectMarker>();
        marker.kind = kind;

        FinishMarkerCreation(go);
    }

    private static void FinishMarkerCreation(GameObject go)
    {
        Selection.activeGameObject = go;
        EditorGUIUtility.PingObject(go);
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
    }

    private static Vector3 GetPlacementPosition()
    {
        var view = SceneView.lastActiveSceneView;
        return view != null ? view.pivot : Vector3.zero;
    }

    public static Transform GetOrCreateGroup(string groupName)
    {
        var root = GameObject.Find(MarkerRootName);
        if (root == null)
        {
            root = new GameObject(MarkerRootName);
            Undo.RegisterCreatedObjectUndo(root, "Create Map Design Root");
        }

        var group = root.transform.Find(groupName);
        if (group == null)
        {
            var go = new GameObject(groupName);
            Undo.RegisterCreatedObjectUndo(go, "Create Map Design Group");
            go.transform.SetParent(root.transform, false);
            group = go.transform;
        }
        return group;
    }

    public static void TrySetTag(GameObject go, string tag)
    {
        try
        {
            go.tag = tag;
        }
        catch (System.Exception)
        {
            // 프로젝트에 태그가 정의되어 있지 않으면 무시한다 (스캔은 컴포넌트 기반이라 태그가 필수는 아님).
        }
    }
}
