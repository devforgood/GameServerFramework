using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

public class MapJsonUpdater : EditorWindow
{
    private string mapJsonPath = "";
    private List<MapData> mapDataList = new List<MapData>();
    private Vector2 scrollPosition;
    private bool showDebugInfo = false;

    [System.Serializable]
    public class Vec3
    {
        public double x;
        public double y;
        public double z;

        public Vec3(Vector3 vector)
        {
            x = System.Math.Round(vector.x, 2);
            y = System.Math.Round(vector.y, 2);
            z = System.Math.Round(vector.z, 2);
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
    }

    [System.Serializable]
    public class SpawnPointInfo
    {
        public Vec3 position;
        public int monster_id;
        public int spawn_interval;
        public int boss_id;
        public int spawn_delay;
    }

    [System.Serializable]
    public class MapSpawnPoints
    {
        public List<SpawnPointInfo> player_spawn = new List<SpawnPointInfo>();
        public List<SpawnPointInfo> monster_spawn = new List<SpawnPointInfo>();
        public List<SpawnPointInfo> boss_spawn = new List<SpawnPointInfo>();
    }

    [System.Serializable]
    public class MapData
    {
        public int id;
        public string name;
        public string name_id;
        public string desc_id;
        public int game_mode_id;
        public MapSize size;
        public List<GateInfo> gates = new List<GateInfo>();
        public MapSpawnPoints spawn_points = new MapSpawnPoints();
        public MapObjects objects = new MapObjects();
        public string navmesh_path; // NavMesh 파일 경로
    }

    [System.Serializable]
    public class MapSize
    {
        public double width;
        public double height;
    }

    [System.Serializable]
    public class MapObjects
    {
        public List<object> static_objects = new List<object>();
        public List<object> movable_objects = new List<object>();
    }

    [MenuItem("Tools/Map JSON Updater")]
    public static void ShowWindow()
    {
        GetWindow<MapJsonUpdater>("Map JSON Updater");
    }

    private void OnEnable()
    {
        LoadMapJson();
    }

    private void LoadMapJson()
    {
        // GameData 디렉토리에서 Map.json 찾기 (여러 경로 시도)
        string[] possiblePaths = {
            Path.Combine(Application.dataPath, "..", "..", "..", "GameData", "Map.json"), // Client/Assets -> GameData
            Path.Combine(Application.dataPath, "..", "..", "GameData", "Map.json"),      // Assets -> GameData
            Path.Combine(Application.dataPath, "..", "GameData", "Map.json"),            // Assets -> GameData
            Path.Combine(Directory.GetCurrentDirectory(), "GameData", "Map.json")       // 현재 디렉토리 -> GameData
        };
        
        foreach (string path in possiblePaths)
        {
            if (File.Exists(path))
            {
                mapJsonPath = path;
                string jsonContent = File.ReadAllText(path);
                mapDataList = JsonConvert.DeserializeObject<List<MapData>>(jsonContent);
                Debug.Log($"Map.json loaded from: {path}");
                return;
            }
        }
        
        Debug.LogError($"Map.json not found in any of these paths:");
        foreach (string path in possiblePaths)
        {
            Debug.LogError($"  - {path}");
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("Map JSON Updater", EditorStyles.boldLabel);
        
        if (string.IsNullOrEmpty(mapJsonPath))
        {
            EditorGUILayout.HelpBox("Map.json not found. Please check GameData directory.", MessageType.Error);
            return;
        }

        EditorGUILayout.LabelField("Map JSON Path:", mapJsonPath);
        
        if (GUILayout.Button("Reload Map JSON"))
        {
            LoadMapJson();
        }

        EditorGUILayout.Space();

        // 현재 씬 정보
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        EditorGUILayout.LabelField("Current Scene:", currentSceneName);

        // 씬에서 태그 찾기
        if (GUILayout.Button("Scan Scene for Tags"))
        {
            ScanSceneForTags(currentSceneName);
        }

        EditorGUILayout.Space();

        // 맵 데이터 표시
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        for (int i = 0; i < mapDataList.Count; i++)
        {
            var map = mapDataList[i];
            bool isCurrentMap = map.name.Equals(currentSceneName, System.StringComparison.OrdinalIgnoreCase);
            
            EditorGUILayout.BeginVertical("box");
            
            string mapTitle = $"{map.name} (ID: {map.id})";
            if (isCurrentMap)
            {
                mapTitle += " [CURRENT]";
                GUI.backgroundColor = Color.green;
            }
            
            EditorGUILayout.LabelField(mapTitle, EditorStyles.boldLabel);
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.LabelField($"Gates: {map.gates.Count}");
            EditorGUILayout.LabelField($"Player Spawns: {map.spawn_points.player_spawn.Count}");
            EditorGUILayout.LabelField($"Monster Spawns: {map.spawn_points.monster_spawn.Count}");
            EditorGUILayout.LabelField($"Boss Spawns: {map.spawn_points.boss_spawn.Count}");
            
            if (showDebugInfo)
            {
                EditorGUILayout.LabelField($"Game Mode ID: {map.game_mode_id}");
                EditorGUILayout.LabelField($"Size: {map.size.width} x {map.size.height}");
            }

            EditorGUILayout.LabelField($"NavMesh: {map.navmesh_path}");
            
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        
        showDebugInfo = EditorGUILayout.Toggle("Show Debug Info", showDebugInfo);
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Save Map JSON"))
        {
            SaveMapJson();
        }
    }

    private void ScanSceneForTags(string sceneName)
    {
        // 현재 맵 찾기 또는 새로 생성
        MapData currentMap = mapDataList.FirstOrDefault(m => m.name.Equals(sceneName, System.StringComparison.OrdinalIgnoreCase));
        
        if (currentMap == null)
        {
            // 새 맵 생성
            currentMap = new MapData
            {
                id = GetNextMapId(),
                name = sceneName,
                name_id = $"map_{sceneName.ToLower()}_name",
                desc_id = $"map_{sceneName.ToLower()}_desc",
                game_mode_id = 1, // 기본값: Field
                size = new MapSize { width = 1000, height = 1000 },
                gates = new List<GateInfo>(),
                spawn_points = new MapSpawnPoints(),
                objects = new MapObjects()
            };
            mapDataList.Add(currentMap);
            Debug.Log($"Created new map: {sceneName} (ID: {currentMap.id})");
        }

        // 씬에서 모든 GameObject 찾기
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        
        // Gate 태그 찾기
        var gateObjects = allObjects.Where(obj => obj.CompareTag("Gate")).ToArray();
        UpdateGates(currentMap, gateObjects);
        
        // Spawn 태그 찾기
        var spawnObjects = allObjects.Where(obj => obj.CompareTag("Spawn")).ToArray();
        UpdateSpawns(currentMap, spawnObjects);

        // 현재 씬의 NavMesh 데이터 찾기
        string navmesh_file_name = $"{sceneName}_navmesh.bin";
        var path = Path.Combine(Application.dataPath, "GeneratedNavMeshes", navmesh_file_name);
        if (File.Exists(path))
        {
            currentMap.navmesh_path = navmesh_file_name;
        }

        Debug.Log($"Scan complete. Found {gateObjects.Length} gates and {spawnObjects.Length} spawns in scene '{sceneName}'");
    }

    private void UpdateGates(MapData map, GameObject[] gateObjects)
    {
        map.gates.Clear();
        
        for (int i = 0; i < gateObjects.Length; i++)
        {
            var gateObj = gateObjects[i];
            var gateComponent = gateObj.GetComponent<Gate>();

            GateInfo foundGate = null;
            var foundMap = mapDataList.Find(m => m.name.Equals(gateComponent.destinationMapName, System.StringComparison.OrdinalIgnoreCase));
            if (foundMap == null)
            {
                Debug.LogWarning($"Destination map '{gateComponent.destinationMapName}' not found for gate '{gateObj.name}'. Using default values.");
            }
            else
            {
                foundGate = foundMap.gates.Find(g => g.name.Equals(gateComponent.destinationGateName, System.StringComparison.OrdinalIgnoreCase));
                if (foundGate == null)
                {
                    Debug.LogWarning($"Destination gate '{gateComponent.destinationGateName}' not found in map '{foundMap.name}' for gate '{gateObj.name}'. Using default values.");
                }
            }

            GateInfo gate = new GateInfo
            {
                id = i + 1,
                name = gateComponent.gateName,
                position = new Vec3(gateObj.transform.position),
                target_map_id = foundMap != null ? foundMap.id : 1,
                target_gate_id = foundGate != null ? foundGate.id : 1,
                required_level = 1
            };
            
            map.gates.Add(gate);
        }
    }

    private void UpdateSpawns(MapData map, GameObject[] spawnObjects)
    {
        map.spawn_points.player_spawn.Clear();
        map.spawn_points.monster_spawn.Clear();
        map.spawn_points.boss_spawn.Clear();
        
        for (int i = 0; i < spawnObjects.Length; i++)
        {
            var spawnObj = spawnObjects[i];
            var spawnComponent = spawnObj.GetComponent<SpawnPoint>();
            
            SpawnPointInfo spawnPoint = new SpawnPointInfo
            {
                position = new Vec3(spawnObj.transform.position),
                monster_id = spawnComponent != null ? spawnComponent.monsterId : 0,
                spawn_interval = spawnComponent != null ? spawnComponent.spawnInterval : 30,
                boss_id = spawnComponent != null ? spawnComponent.bossId : 0,
                spawn_delay = spawnComponent != null ? spawnComponent.spawnDelay : 0
            };
            
            // 스폰 타입에 따라 분류
            if (spawnComponent != null)
            {
                switch (spawnComponent.spawnType)
                {
                    case SpawnType.Player:
                        map.spawn_points.player_spawn.Add(spawnPoint);
                        break;
                    case SpawnType.Monster:
                        map.spawn_points.monster_spawn.Add(spawnPoint);
                        break;
                    case SpawnType.Boss:
                        map.spawn_points.boss_spawn.Add(spawnPoint);
                        break;
                }
            }
            else
            {
                // 기본값: 플레이어 스폰
                map.spawn_points.player_spawn.Add(spawnPoint);
            }
        }
    }

    private int GetNextMapId()
    {
        if (mapDataList.Count == 0)
            return 1;
        
        return mapDataList.Max(m => m.id) + 1;
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
            string jsonContent = JsonConvert.SerializeObject(mapDataList, Formatting.Indented);
            File.WriteAllText(mapJsonPath, jsonContent);
            Debug.Log($"Map JSON saved successfully to: {mapJsonPath}");
            
            // Unity 에디터 새로고침
            AssetDatabase.Refresh();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save Map JSON: {e.Message}");
        }
    }
}
