using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

// Map.json을 원본(source of truth)으로 씬과 자동 동기화하는 에디터 상주 모듈.
//
// 방향 규칙:
// - 씬을 여는 순간에만 JSON이 이긴다: Map.json 기준으로 씬 마커를 생성/갱신/삭제한다.
//   (게이트는 id — id 미발급 시 이름 폴백, 오브젝트는 objectId로 매칭하여
//    기존 오브젝트(프리팹 인스턴스 포함)를 보존한 채 값만 맞춘다)
// - 그 이후에는 씬 편집이 이긴다: 마커 추가/삭제/이동/필드 변경을 감지해
//   디바운스(1초) 후 Map.json을 자동 저장한다.
//
// Map.json에 항목이 없는 씬은 어느 방향으로도 건드리지 않는다 (맵 항목 생성은 창에서 명시적으로).
[InitializeOnLoad]
public static class MapJsonAutoSync
{
    private const string EnabledPrefKey = "MapJsonAutoSync.Enabled";
    private const double SaveDebounceSeconds = 1.0;
    private const float PositionTolerance = 0.006f; // JSON 좌표가 소수 2자리로 반올림되므로 그 이하 차이는 무시

    private static bool savePending;
    private static double lastChangeTime;
    private static bool applying; // JSON→씬 반영/자동 저장 중 재진입 방지

    public static bool Enabled
    {
        get { return EditorPrefs.GetBool(EnabledPrefKey, true); }
        set { EditorPrefs.SetBool(EnabledPrefKey, value); }
    }

    static MapJsonAutoSync()
    {
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosing += OnSceneClosing;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        Undo.postprocessModifications += OnPostprocessModifications;
        EditorApplication.update += OnUpdate;
        EditorApplication.quitting += FlushPendingSave;
    }

    private static bool CanSync
    {
        get
        {
            return Enabled
                && !EditorApplication.isPlayingOrWillChangePlaymode
                && UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null;
        }
    }

    // -------------------------------------------------------------
    // 데이터 소스: Map.json 파일을 직접 읽는다.
    // (예전에는 Map JSON Updater 창이 열려 있으면 그 창의 리스트를 공유했지만,
    //  창이 Map Tool 로 통합되면서 항상 파일이 유일한 출처가 됐다.)
    // -------------------------------------------------------------

    private static bool TryGetMapList(out string path, out List<MapJsonUpdater.MapData> maps)
    {
        path = MapJsonUpdater.FindMapJsonPath();
        if (path == null)
        {
            maps = null;
            return false;
        }
        maps = MapJsonUpdater.LoadMapList(path);
        return true;
    }

    // -------------------------------------------------------------
    // JSON → 씬 (씬을 열 때)
    // -------------------------------------------------------------

    private static void OnSceneOpened(UnityEngine.SceneManagement.Scene scene, OpenSceneMode mode)
    {
        if (!CanSync || mode != OpenSceneMode.Single)
            return;
        ApplyJsonToScene(scene.name);
    }

    // JSON 기준으로 씬 마커를 맞추고 변경 건수를 반환한다. 대상 씬/맵이 없으면 -1.
    public static int ApplyJsonToScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return -1;
        if (!TryGetMapList(out _, out var maps))
            return -1;

        var map = MapJsonUpdater.FindMapForScene(maps, sceneName);
        if (map == null)
            return -1; // 연결된 맵이 없는 씬은 건드리지 않는다

        applying = true;
        try
        {
            int changes = 0;
            changes += ReconcileGates(map);
            changes += ReconcileSpawns(map);
            changes += ReconcileObjects(map);

            if (changes > 0)
            {
                EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                Debug.Log($"[MapAutoSync] '{sceneName}' 씬을 Map.json 기준으로 동기화: {changes}건 변경 (씬 저장 필요)");
            }
            return changes;
        }
        finally
        {
            applying = false;
            savePending = false; // 방금 JSON 기준으로 맞췄으므로 자동 저장 불필요
        }
    }

    // --- 게이트: Gate.id 로 매칭, id 미발급(0)/불일치 시 이름으로 폴백 매칭 ---

    private static int ReconcileGates(MapJsonUpdater.MapData map)
    {
        int changes = 0;
        var comps = Object.FindObjectsOfType<Gate>(true).ToList();
        var used = new HashSet<Gate>();

        // 1차: id 매칭 (이름 변경에 안전)
        var pending = new List<MapJsonUpdater.GateInfo>();
        foreach (var info in map.gates)
        {
            var comp = comps.FirstOrDefault(c => c.id == info.id && !used.Contains(c));
            if (comp != null)
            {
                used.Add(comp);
                changes += ApplyGate(comp, info);
            }
            else
            {
                pending.Add(info);
            }
        }

        // 2차: id로 짝을 못 찾은 항목은 이름(gateName, 없으면 GameObject 이름)으로 짝짓는다.
        // 씬에 id가 저장되지 않은 게이트(프리팹 기본값 0)의 마이그레이션 경로 —
        // 삭제/재생성 대신 값만 갱신하며, ApplyGate가 id를 컴포넌트에 기록해 다음부터는 id로 매칭된다.
        foreach (var info in pending)
        {
            var comp = comps.FirstOrDefault(c => !used.Contains(c) && GateNameMatches(c, info.name));
            if (comp == null)
            {
                comp = CreateGate(info);
                changes++;
            }
            used.Add(comp);
            changes += ApplyGate(comp, info);
        }

        foreach (var extra in comps.Where(c => !used.Contains(c)).ToList())
        {
            Debug.LogWarning($"[MapAutoSync] Map.json에 없는 게이트 '{extra.gameObject.name}' (id:{extra.id}) 를 씬에서 제거합니다.");
            Undo.DestroyObjectImmediate(extra.gameObject);
            changes++;
        }
        return changes;
    }

    private static bool GateNameMatches(Gate comp, string jsonName)
    {
        if (string.IsNullOrEmpty(jsonName))
            return false;
        string compName = string.IsNullOrEmpty(comp.gateName) ? comp.gameObject.name : comp.gateName;
        return jsonName.Equals(compName, System.StringComparison.OrdinalIgnoreCase);
    }

    private static Gate CreateGate(MapJsonUpdater.GateInfo info)
    {
        // Gate 프리팹이 있으면 프리팹으로 생성(시각물 유지), 없으면 트리거 마커로 생성한다.
        GameObject go;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gate.prefab");
        if (prefab != null)
        {
            go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        }
        else
        {
            go = new GameObject();
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(2f, 3f, 2f);
            box.center = new Vector3(0f, 1.5f, 0f);
            go.AddComponent<Gate>();
        }

        Undo.RegisterCreatedObjectUndo(go, "Map Auto Sync");
        go.name = string.IsNullOrEmpty(info.name) ? $"Gate_{info.id}" : info.name;
        go.transform.SetParent(MapJsonUpdater.GetOrCreateGroup(MapJsonUpdater.GateGroupName), true);
        MapJsonUpdater.TrySetTag(go, "Gate");
        Debug.Log($"[MapAutoSync] 게이트 '{go.name}' (id:{info.id}) 생성");
        return go.GetComponent<Gate>();
    }

    private static int ApplyGate(Gate comp, MapJsonUpdater.GateInfo info)
    {
        Vector3 pos = info.position != null ? info.position.ToVector3() : Vector3.zero;
        string gateName = string.IsNullOrEmpty(info.name) ? comp.gateName : info.name;
        GateType gateType = MapJsonUpdater.GateTypeFromJson(info.type);

        int changed = 0;
        if (comp.id != info.id
            || comp.gateName != gateName
            || comp.gateType != gateType
            || comp.targetId != info.target_id
            || comp.requiredLevel != info.required_level)
        {
            Undo.RecordObject(comp, "Map Auto Sync");
            comp.id = info.id;
            comp.gateName = gateName;
            comp.gateType = gateType;
            comp.targetId = info.target_id;
            comp.requiredLevel = info.required_level;
            EditorUtility.SetDirty(comp);
            changed = 1;
        }
        if (!Approximately(comp.transform.position, pos))
        {
            Undo.RecordObject(comp.transform, "Map Auto Sync");
            comp.transform.position = pos;
            EditorUtility.SetDirty(comp.transform);
            changed = 1;
        }
        return changed;
    }

    // --- 스폰: SpawnPoint.id 로 매칭. id가 없는(0) 항목은 종전처럼 이름순 인덱스 매칭(마이그레이션 경로) ---

    private static int ReconcileSpawns(MapJsonUpdater.MapData map)
    {
        var all = Object.FindObjectsOfType<SpawnPoint>(true)
            .OrderBy(s => s.name, System.StringComparer.OrdinalIgnoreCase).ToList();

        int changes = 0;
        changes += ReconcileSpawnList(map.spawn_points.player_spawn, SpawnType.Player, "PlayerSpawn",
            all.Where(s => s.spawnType == SpawnType.Player).ToList());
        changes += ReconcileSpawnList(map.spawn_points.monster_spawn, SpawnType.Monster, "MonsterSpawn",
            all.Where(s => s.spawnType == SpawnType.Monster).ToList());
        changes += ReconcileSpawnList(map.spawn_points.boss_spawn, SpawnType.Boss, "BossSpawn",
            all.Where(s => s.spawnType == SpawnType.Boss).ToList());
        return changes;
    }

    private static int ReconcileSpawnList(List<MapJsonUpdater.SpawnPointInfo> infos, SpawnType type,
        string baseName, List<SpawnPoint> comps)
    {
        int changes = 0;
        var used = new HashSet<SpawnPoint>();

        // 1차: id 매칭 (이름 변경에 안전)
        var pending = new List<MapJsonUpdater.SpawnPointInfo>();
        foreach (var info in infos)
        {
            SpawnPoint comp = info.id > 0
                ? comps.FirstOrDefault(c => c.id == info.id && !used.Contains(c))
                : null;
            if (comp != null)
            {
                used.Add(comp);
                changes += ApplySpawn(comp, info);
            }
            else
            {
                pending.Add(info);
            }
        }

        // 2차: id로 짝을 찾지 못한 항목은 남은 컴포넌트와 이름순으로 짝짓는다.
        // (id 도입 전 데이터/씬의 마이그레이션 경로. 짝지어지면 ApplySpawn이 id를 컴포넌트에 기록한다)
        var freeComps = comps.Where(c => !used.Contains(c)).ToList();
        int next = 0;
        foreach (var info in pending)
        {
            SpawnPoint comp;
            if (next < freeComps.Count)
            {
                comp = freeComps[next++];
            }
            else
            {
                var go = new GameObject(info.id > 0 ? $"{baseName}_{info.id}" : $"{baseName}_{next + 1}");
                Undo.RegisterCreatedObjectUndo(go, "Map Auto Sync");
                go.transform.SetParent(MapJsonUpdater.GetOrCreateGroup(MapJsonUpdater.SpawnGroupName), true);
                MapJsonUpdater.TrySetTag(go, "Spawn");
                comp = go.AddComponent<SpawnPoint>();
                comp.spawnType = type;
                changes++;
            }
            used.Add(comp);
            changes += ApplySpawn(comp, info);
        }

        foreach (var extra in comps.Where(c => !used.Contains(c)).ToList())
        {
            Debug.LogWarning($"[MapAutoSync] Map.json에 없는 스폰 '{extra.gameObject.name}' ({type}) 를 씬에서 제거합니다.");
            Undo.DestroyObjectImmediate(extra.gameObject);
            changes++;
        }
        return changes;
    }

    private static int ApplySpawn(SpawnPoint comp, MapJsonUpdater.SpawnPointInfo info)
    {
        Vector3 pos = info.position != null ? info.position.ToVector3() : Vector3.zero;

        int changed = 0;
        if ((info.id > 0 && comp.id != info.id) // id 0(미발급)인 JSON이 컴포넌트의 id를 지우지 않도록 info.id > 0 일 때만 동기화
            || comp.monsterId != info.monster_id
            || comp.spawnInterval != info.spawn_interval
            || comp.bossId != info.boss_id
            || comp.spawnDelay != info.spawn_delay)
        {
            Undo.RecordObject(comp, "Map Auto Sync");
            if (info.id > 0)
                comp.id = info.id;
            comp.monsterId = info.monster_id;
            comp.spawnInterval = info.spawn_interval;
            comp.bossId = info.boss_id;
            comp.spawnDelay = info.spawn_delay;
            EditorUtility.SetDirty(comp);
            changed = 1;
        }
        if (!Approximately(comp.transform.position, pos))
        {
            Undo.RecordObject(comp.transform, "Map Auto Sync");
            comp.transform.position = pos;
            EditorUtility.SetDirty(comp.transform);
            changed = 1;
        }
        return changed;
    }

    // --- 오브젝트: MapObjectMarker.objectId 로 매칭 ---

    private static int ReconcileObjects(MapJsonUpdater.MapData map)
    {
        int changes = 0;
        var markers = Object.FindObjectsOfType<MapObjectMarker>(true).ToList();
        var used = new HashSet<MapObjectMarker>();

        foreach (var info in map.objects.static_objects)
        {
            var m = markers.FirstOrDefault(x => x.objectId == info.id && !used.Contains(x));
            if (m == null)
            {
                m = CreateObjectMarker(MapObjectKind.Static, info.id, info.name);
                changes++;
            }
            used.Add(m);
            changes += ApplyStaticObject(m, info);
        }

        foreach (var info in map.objects.movable_objects)
        {
            var m = markers.FirstOrDefault(x => x.objectId == info.id && !used.Contains(x));
            if (m == null)
            {
                m = CreateObjectMarker(MapObjectKind.Movable, info.id, info.name);
                changes++;
            }
            used.Add(m);
            changes += ApplyMovableObject(m, info);
        }

        foreach (var extra in markers.Where(x => !used.Contains(x)).ToList())
        {
            Debug.LogWarning($"[MapAutoSync] Map.json에 없는 오브젝트 '{extra.gameObject.name}' (id:{extra.objectId}) 를 씬에서 제거합니다.");
            Undo.DestroyObjectImmediate(extra.gameObject);
            changes++;
        }
        return changes;
    }

    private static MapObjectMarker CreateObjectMarker(MapObjectKind kind, int id, string name)
    {
        var go = new GameObject(string.IsNullOrEmpty(name)
            ? (kind == MapObjectKind.Static ? $"StaticObject_{id}" : $"MovableObject_{id}")
            : name);
        Undo.RegisterCreatedObjectUndo(go, "Map Auto Sync");
        go.transform.SetParent(MapJsonUpdater.GetOrCreateGroup(MapJsonUpdater.ObjectGroupName), true);

        var marker = go.AddComponent<MapObjectMarker>();
        marker.kind = kind;
        marker.objectId = id;
        Debug.Log($"[MapAutoSync] 오브젝트 '{go.name}' (id:{id}, {kind}) 생성");
        return marker;
    }

    private static int ApplyStaticObject(MapObjectMarker m, MapJsonUpdater.StaticObjectInfo info)
    {
        Vector3 pos = info.position != null ? info.position.ToVector3() : Vector3.zero;
        Vector3 size = info.size != null ? info.size.ToVector3() : m.transform.localScale;

        int changed = 0;
        if (m.kind != MapObjectKind.Static
            || m.objectId != info.id
            || m.objectType != info.type
            || m.collision != info.collision
            || m.damage != (info.damage ?? 0)
            || m.lootTableId != (info.loot_table_id ?? 0))
        {
            Undo.RecordObject(m, "Map Auto Sync");
            m.kind = MapObjectKind.Static;
            m.objectId = info.id;
            m.objectType = info.type;
            m.collision = info.collision;
            m.damage = info.damage ?? 0;
            m.lootTableId = info.loot_table_id ?? 0;
            EditorUtility.SetDirty(m);
            changed = 1;
        }
        changed = System.Math.Max(changed, ApplyObjectName(m, info.name));
        if (!Approximately(m.transform.position, pos) || !Approximately(m.transform.localScale, size))
        {
            Undo.RecordObject(m.transform, "Map Auto Sync");
            m.transform.position = pos;
            m.transform.localScale = size;
            EditorUtility.SetDirty(m.transform);
            changed = 1;
        }
        return changed;
    }

    private static int ApplyMovableObject(MapObjectMarker m, MapJsonUpdater.MovableObjectInfo info)
    {
        Vector3 pos = info.position != null ? info.position.ToVector3() : Vector3.zero;

        int changed = 0;
        if (m.kind != MapObjectKind.Movable
            || m.objectId != info.id
            || m.objectType != info.type
            || Mathf.Abs(m.movementRange - (float)info.movement_range) > PositionTolerance
            || Mathf.Abs(m.movementSpeed - (float)info.movement_speed) > PositionTolerance
            || !PatrolEquals(m.patrolPath, info.patrol_path))
        {
            Undo.RecordObject(m, "Map Auto Sync");
            m.kind = MapObjectKind.Movable;
            m.objectId = info.id;
            m.objectType = info.type;
            m.movementRange = (float)info.movement_range;
            m.movementSpeed = (float)info.movement_speed;
            m.patrolPath = info.patrol_path != null
                ? info.patrol_path.Select(p => p.ToVector3()).ToList()
                : new List<Vector3>();
            EditorUtility.SetDirty(m);
            changed = 1;
        }
        changed = System.Math.Max(changed, ApplyObjectName(m, info.name));
        if (!Approximately(m.transform.position, pos))
        {
            Undo.RecordObject(m.transform, "Map Auto Sync");
            m.transform.position = pos;
            EditorUtility.SetDirty(m.transform);
            changed = 1;
        }
        return changed;
    }

    // 오브젝트 이름은 GameObject 이름으로 기록되므로 JSON 이름과 맞춘다.
    private static int ApplyObjectName(MapObjectMarker m, string name)
    {
        if (string.IsNullOrEmpty(name) || m.gameObject.name == name)
            return 0;
        Undo.RecordObject(m.gameObject, "Map Auto Sync");
        m.gameObject.name = name;
        return 1;
    }

    private static bool PatrolEquals(List<Vector3> a, List<MapJsonUpdater.Vec3> b)
    {
        int na = a != null ? a.Count : 0;
        int nb = b != null ? b.Count : 0;
        if (na != nb)
            return false;
        for (int i = 0; i < na; i++)
        {
            if (!Approximately(a[i], b[i].ToVector3()))
                return false;
        }
        return true;
    }

    private static bool Approximately(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) < PositionTolerance
            && Mathf.Abs(a.y - b.y) < PositionTolerance
            && Mathf.Abs(a.z - b.z) < PositionTolerance;
    }

    // -------------------------------------------------------------
    // 씬 → JSON (마커 변경 감지 → 디바운스 자동 저장)
    // -------------------------------------------------------------

    private static void OnHierarchyChanged()
    {
        // 마커 외의 변경도 걸리지만, 저장 시 기존 파일과 비교하여 내용이 같으면 쓰지 않는다.
        if (applying || !CanSync)
            return;
        MarkPending();
    }

    private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
    {
        // 트랜스폼 이동/인스펙터 편집 감지 (hierarchyChanged로는 잡히지 않음)
        if (!applying && CanSync)
        {
            foreach (var mod in modifications)
            {
                var target = mod.currentValue != null ? mod.currentValue.target : null;
                if (IsMarkerRelated(target))
                {
                    MarkPending();
                    break;
                }
            }
        }
        return modifications;
    }

    private static bool IsMarkerRelated(Object target)
    {
        if (target == null)
            return false;
        if (target is Gate || target is SpawnPoint || target is MapObjectMarker)
            return true;
        var t = target as Transform;
        return t != null
            && (t.GetComponent<Gate>() != null
                || t.GetComponent<SpawnPoint>() != null
                || t.GetComponent<MapObjectMarker>() != null);
    }

    private static void MarkPending()
    {
        savePending = true;
        lastChangeTime = EditorApplication.timeSinceStartup;
    }

    private static void OnUpdate()
    {
        if (!savePending)
            return;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return; // 플레이 중에는 보류. 플레이 진입 시점에 이미 플러시된다.
        if (!CanSync)
            return;
        if (EditorApplication.timeSinceStartup - lastChangeTime < SaveDebounceSeconds)
            return;

        savePending = false;
        SaveActiveSceneToJson();
    }

    // 씬 전환/플레이 진입/에디터 종료 직전에 대기 중인 저장을 즉시 반영한다.
    private static void OnSceneClosing(UnityEngine.SceneManagement.Scene scene, bool removingScene)
    {
        FlushPendingSave();
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
            FlushPendingSave();
    }

    private static void FlushPendingSave()
    {
        if (!savePending || !Enabled)
            return;
        savePending = false;
        SaveActiveSceneToJson();
    }

    public static void SaveActiveSceneToJson()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.name))
            return;
        if (!TryGetMapList(out var path, out var maps))
            return;

        var map = MapJsonUpdater.FindMapForScene(maps, scene.name);
        if (map == null)
            return; // Map.json에 항목이 없는 씬은 자동 저장하지 않는다

        applying = true;
        try
        {
            MapJsonUpdater.ScanSceneIntoMap(map, maps, scene.name);

            // 내용이 실제로 바뀌었을 때만 파일에 쓴다 (불필요한 저장/로그 방지).
            string json = JsonConvert.SerializeObject(maps, Formatting.Indented);
            string current = File.Exists(path) ? File.ReadAllText(path) : null;
            if (json == current)
                return;

            MapJsonUpdater.ValidateMapReferences(maps);
            MapJsonUpdater.BackupBeforeWrite(path);
            File.WriteAllText(path, json);
            Debug.Log($"[MapAutoSync] '{scene.name}' 마커 변경 감지 → Map.json 자동 저장");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MapAutoSync] Map.json 자동 저장 실패: {e.Message}");
        }
        finally
        {
            applying = false;
        }
    }
}
