using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// Map.json 데이터 모델과 씬 ⇔ JSON 변환 로직.
// - 씬 스캔: 씬의 Gate / SpawnPoint / MapObjectMarker 컴포넌트를 찾아 Map.json 에 기록
// - 씬 빌드: Map.json 의 게이트/스폰/오브젝트를 씬 마커로 생성
// - 마커 배치: 씬 뷰 위치에 게이트/스폰/오브젝트 마커 생성
// 예전에는 이 클래스가 독립 창(Tools > Map JSON Updater)이었지만, 맵 제작 흐름을
// Map Tool 하나로 합치면서 UI 를 걷어내고 라이브러리만 남겼다.
// 호출자: MapPipeline(Map Tool 파이프라인), MapJsonAutoSync(씬 열기/편집 시 자동 동기화).
public static class MapJsonUpdater
{
    private const string MarkerRootName = "MapDesign";
    public const string GateGroupName = "Gates";
    public const string SpawnGroupName = "SpawnPoints";
    public const string ObjectGroupName = "Objects";

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
        public string type;   // "two_way"(짝 필수) | "one_way"(인스턴스 입구, target_gate_id 0 = player_spawn 도착)
        public Vec3 position;
        public int target_map_id;
        public int target_gate_id;
        public int required_level;

        [JsonExtensionData]
        public IDictionary<string, JToken> extra;
    }

    // Gate.gateType(enum) ↔ Map.json gates[].type(문자열) 변환.
    public const string GateTypeTwoWay = "two_way";
    public const string GateTypeOneWay = "one_way";

    public static string GateTypeToJson(GateType t)
        => t == GateType.OneWay ? GateTypeOneWay : GateTypeTwoWay;

    public static GateType GateTypeFromJson(string s)
        => s == GateTypeOneWay ? GateType.OneWay : GateType.TwoWay;

    [System.Serializable]
    public class SpawnPointInfo
    {
        public int id;
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
    // Map.json 파일 접근 (창이 아니라 Map Tool 파이프라인이 호출한다)
    // ---------------------------------------------------------------------

    public static string FindMapJsonPath()
    {
        string path = Path.Combine(Application.dataPath, "Resources", "GameData", "Map.json");
        return File.Exists(path) ? Path.GetFullPath(path) : null;
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
    // 문자열은 null 대신 "" 로 저장한다 — 클라이언트 JsonUtility 는 null 값에서 파싱 전체가 실패한다.
    private static void EnsureDefaults(MapData map)
    {
        if (map.navmesh_path == null) map.navmesh_path = "";
        if (map.size == null) map.size = new MapSize();
        if (map.gates == null) map.gates = new List<GateInfo>();
        foreach (var gate in map.gates)
        {
            // 타입 미지정 게이트는 기본 양방향으로 저장한다(데이터 검증은 유효한 타입을 요구).
            if (string.IsNullOrEmpty(gate.type)) gate.type = GateTypeTwoWay;
        }
        if (map.spawn_points == null) map.spawn_points = new MapSpawnPoints();
        if (map.spawn_points.player_spawn == null) map.spawn_points.player_spawn = new List<SpawnPointInfo>();
        if (map.spawn_points.monster_spawn == null) map.spawn_points.monster_spawn = new List<SpawnPointInfo>();
        if (map.spawn_points.boss_spawn == null) map.spawn_points.boss_spawn = new List<SpawnPointInfo>();
        if (map.objects == null) map.objects = new MapObjects();
        if (map.objects.static_objects == null) map.objects.static_objects = new List<StaticObjectInfo>();
        if (map.objects.movable_objects == null) map.objects.movable_objects = new List<MovableObjectInfo>();
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

                string gateLabel = $"맵 '{map.name}'({map.id}) 게이트 '{gate.name}'(id:{gate.id})";

                if (gate.type != GateTypeTwoWay && gate.type != GateTypeOneWay)
                {
                    Debug.LogWarning($"[MapValidate] {gateLabel} → type 은 '{GateTypeTwoWay}' 또는 '{GateTypeOneWay}' 여야 합니다 (현재: '{gate.type}')");
                    problems++;
                }

                if (!mapById.TryGetValue(gate.target_map_id, out var targetMap))
                {
                    Debug.LogWarning($"[MapValidate] {gateLabel} → 존재하지 않는 맵 {gate.target_map_id} 참조");
                    problems++;
                }
                else if (gate.type == GateTypeOneWay && gate.target_gate_id == 0)
                {
                    // one_way + target_gate_id 0 = 목적지 맵의 player_spawn 지점 도착(레이드 등 인스턴스 입구).
                    if (targetMap.spawn_points.player_spawn.Count == 0)
                    {
                        Debug.LogWarning($"[MapValidate] {gateLabel} → target_gate_id 0(스폰 지점 도착)인데 맵 '{targetMap.name}'({targetMap.id})에 player_spawn 이 없습니다");
                        problems++;
                    }
                }
                else if (!targetMap.gates.Any(g => g.id == gate.target_gate_id))
                {
                    Debug.LogWarning($"[MapValidate] {gateLabel} → 맵 '{targetMap.name}'({targetMap.id})에 없는 게이트 {gate.target_gate_id} 참조");
                    problems++;
                }
                else if (gate.type == GateTypeTwoWay)
                {
                    // 양방향 게이트는 짝이어야 한다: 상대도 two_way 이고 이 게이트를 되가리켜야 한다.
                    var back = targetMap.gates.First(g => g.id == gate.target_gate_id);
                    if (back.type != GateTypeTwoWay || back.target_map_id != map.id || back.target_gate_id != gate.id)
                    {
                        Debug.LogWarning($"[MapValidate] {gateLabel} → two_way 짝 불일치: 맵 '{targetMap.name}'({targetMap.id}) 게이트 {gate.target_gate_id} 가 two_way 로 이 게이트를 되가리켜야 합니다");
                        problems++;
                    }
                }
            }

            // 스폰 id는 맵 내에서 타입 구분 없이 유일해야 한다 (0은 미발급 상태라 검사 제외).
            var seenSpawnIds = new HashSet<int>();
            var allSpawns = map.spawn_points.player_spawn
                .Concat(map.spawn_points.monster_spawn)
                .Concat(map.spawn_points.boss_spawn);
            foreach (var spawn in allSpawns)
            {
                if (spawn.id > 0 && !seenSpawnIds.Add(spawn.id))
                {
                    Debug.LogWarning($"[MapValidate] 맵 '{map.name}'({map.id}) 안에 스폰 id {spawn.id} 중복");
                    problems++;
                }
            }
        }

        if (problems > 0)
            Debug.LogWarning($"[MapValidate] 게이트 참조 문제 {problems}건 발견 (위 경고 참조). 저장은 진행됩니다.");
        return problems;
    }

    // ---------------------------------------------------------------------
    // 씬 ↔ 맵 항목 매칭
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

    // ---------------------------------------------------------------------
    // 씬 스캔 → JSON
    // ---------------------------------------------------------------------

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
                type = GateTypeToJson(comp.gateType),
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
        // 스폰 id는 게이트와 동일하게 컴포넌트가 보유한다(이름 변경 시 페어링이 밀리지 않도록).
        // id는 맵 내에서 스폰 타입 구분 없이 유일하다. 0(또는 중복)이면 새 id를 발급해 컴포넌트에 기록한다.
        var oldAll = map.spawn_points.player_spawn
            .Concat(map.spawn_points.monster_spawn)
            .Concat(map.spawn_points.boss_spawn).ToList();

        map.spawn_points.player_spawn.Clear();
        map.spawn_points.monster_spawn.Clear();
        map.spawn_points.boss_spawn.Clear();

        var claimed = new HashSet<int>();
        int nextId = 1;

        foreach (var comp in spawnComponents)
        {
            int id = comp.id;
            if (id <= 0 || claimed.Contains(id))
            {
                while (claimed.Contains(nextId)) nextId++;
                id = nextId;
            }
            claimed.Add(id);
            AssignSpawnId(comp, id);

            var old = oldAll.Find(s => s.id == id);

            var info = new SpawnPointInfo
            {
                id = id,
                position = new Vec3(comp.transform.position),
                monster_id = comp.monsterId,
                spawn_interval = comp.spawnInterval,
                boss_id = comp.bossId,
                spawn_delay = comp.spawnDelay,
                extra = old?.extra
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

    // 다음 스캔에서도 id가 유지되도록 스폰 컴포넌트에 id를 기록해 둔다.
    private static void AssignSpawnId(SpawnPoint spawn, int id)
    {
        if (spawn.id == id)
            return;
        Undo.RecordObject(spawn, "Assign Spawn Id");
        spawn.id = id;
        EditorUtility.SetDirty(spawn);
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

    public static void CalcSizeFromSceneBounds(MapData map)
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
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
    // JSON → 씬 동기화 / 빌드
    // ---------------------------------------------------------------------

    // 씬을 연 채 Map.json을 외부에서 편집했을 때 재오픈 없이 반영한다.
    // Build와 달리 id 매칭 기반 reconcile이라 기존 오브젝트(프리팹 인스턴스 포함)를 보존한다.
    public static void SyncSceneFromJson(string sceneName)
    {
        if (!EditorUtility.DisplayDialog("Sync Scene from JSON",
                "Rebuild scene markers from Map.json on disk.\n" +
                "(matched by id: values are updated, markers missing from JSON are deleted)\n\n" +
                "Unsaved marker edits in the scene will be lost. Continue?",
                "Sync", "Cancel"))
            return;

        int changes = MapJsonAutoSync.ApplyJsonToScene(sceneName);
        if (changes == 0)
            Debug.Log($"[MapTool] Scene '{sceneName}' already matches Map.json (0 changes).");
    }

    public static void BuildSceneFromJson(MapData map)
    {
        // 기존 마커가 있으면 교체 여부 확인
        var existing = new List<GameObject>();
        existing.AddRange(Object.FindObjectsOfType<Gate>(true).Select(c => c.gameObject));
        existing.AddRange(Object.FindObjectsOfType<SpawnPoint>(true).Select(c => c.gameObject));
        existing.AddRange(Object.FindObjectsOfType<MapObjectMarker>(true).Select(c => c.gameObject));
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
            comp.gateType = GateTypeFromJson(gate.type);
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

    private static int BuildSpawns(List<SpawnPointInfo> spawns, SpawnType type, string baseName)
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
            comp.id = info.id;
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

    public static void CreateGateMarker()
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

    public static void CreateSpawnMarker(SpawnType type)
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

    public static void CreateObjectMarker(MapObjectKind kind)
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
