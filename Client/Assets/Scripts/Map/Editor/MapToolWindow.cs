using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// The map authoring window: scene -> terrain -> navmesh -> Map.json, in one place.
///
/// This replaces three separate tools (Terrain Generator, RecastNavigation NavMesh Generator,
/// Map JSON Updater) that had to be opened one by one, with file paths matched by hand and
/// no indication of what was still missing.
///
/// Every step derives its files from the active scene name (see MapPipeline), so the stages
/// chain automatically, and each step reports its own state from files on disk.
/// </summary>
public class MapToolWindow : EditorWindow
{
    private enum StepState { Todo, Done, Stale }

    private string newSceneName = "New Map";
    private Vector2 scroll;

    private readonly TerrainBuilder.Settings terrain = new TerrainBuilder.Settings();
    private readonly NavMeshBakeSettings navMesh = new NavMeshBakeSettings();

    // Details stay folded by default: opening the window should show what to build and what is
    // missing, not every knob. Fold state is remembered per user.
    private const string PrefPrefix = "MapTool.";
    private bool showTerrainDetails;
    private bool showNavMeshSettings;
    private bool showMarkers;
    private bool showMapFields;
    private bool scanMarkers = true;

    // Map.json 필드 편집 버퍼. 화면에서 고치는 값은 여기 담아 두었다가 저장할 때만 파일에 반영한다
    // (저장은 디스크를 다시 읽어 해당 맵의 필드만 덮어쓰므로, 그 사이 자동 저장된 마커 변경을 지우지 않는다).
    private MapPipeline.MapMetadata mapFields;
    private int mapFieldsLoadedFor = -1;
    private int setupReferenceIndex;

    private string lastResult = "";

    // Map.json 의 맵 목록 캐시. OnGUI 는 초당 여러 번 도는데 그때마다 파일을 읽고 역직렬화할 수는 없다.
    // 창을 열/포커스할 때와 등록 직후에만 새로 읽는다.
    private List<MapJsonUpdater.MapData> mapEntries = new List<MapJsonUpdater.MapData>();
    private string[] mapLabels = new string[0];

    [MenuItem("Tools/Map Tool")]
    public static void ShowWindow()
    {
        GetWindow<MapToolWindow>("Map Tool").minSize = new Vector2(420, 520);
    }

    private void OnEnable()
    {
        showTerrainDetails = EditorPrefs.GetBool(PrefPrefix + "TerrainDetails", false);
        showNavMeshSettings = EditorPrefs.GetBool(PrefPrefix + "NavMeshSettings", false);
        showMarkers = EditorPrefs.GetBool(PrefPrefix + "Markers", false);
        showMapFields = EditorPrefs.GetBool(PrefPrefix + "MapFields", false);
        RefreshMapEntries();
    }

    // 다른 창에서 Map.json 을 고쳤을 수 있으므로 포커스를 받을 때 다시 읽는다.
    private void OnFocus()
    {
        RefreshMapEntries();
    }

    private void RefreshMapEntries()
    {
        mapEntries = MapPipeline.LoadAllMaps();
        mapLabels = mapEntries
            .Select(m =>
            {
                string sceneName = MapPipeline.SceneNameOf(m);
                bool hasScene = MapPipeline.FindSceneAssetPath(m) != null;
                return $"{m.id}. {sceneName}{(hasScene ? "" : "  (scene asset missing)")}";
            })
            .ToArray();
    }

    private void OnDisable()
    {
        EditorPrefs.SetBool(PrefPrefix + "TerrainDetails", showTerrainDetails);
        EditorPrefs.SetBool(PrefPrefix + "NavMeshSettings", showNavMeshSettings);
        EditorPrefs.SetBool(PrefPrefix + "Markers", showMarkers);
        EditorPrefs.SetBool(PrefPrefix + "MapFields", showMapFields);
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);

        string sceneName = EditorSceneManager.GetActiveScene().name;

        EditorGUILayout.LabelField("Map Tool", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Builds one scene all the way to the data the server reads.", EditorStyles.miniLabel);
        EditorGUILayout.Space();

        DrawSceneStep(sceneName);
        EditorGUILayout.Space();
        DrawTerrainStep();
        EditorGUILayout.Space();
        DrawNavMeshStep(sceneName);
        EditorGUILayout.Space();
        DrawMapJsonStep(sceneName);
        EditorGUILayout.Space();

        DrawRunAll(sceneName);

        if (!string.IsNullOrEmpty(lastResult))
        {
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(lastResult, MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    // -- Step 1 --
    private void DrawSceneStep(string sceneName)
    {
        bool saved = !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path);
        BeginStep(1, "Scene", saved ? StepState.Done : StepState.Todo);

        DrawActiveSceneSelector(sceneName);

        if (!saved)
            EditorGUILayout.HelpBox("Save the scene first — every later step is named after it.", MessageType.Warning);

        EditorGUILayout.BeginHorizontal();
        newSceneName = EditorGUILayout.TextField("New map scene", newSceneName);
        if (GUILayout.Button("Create / Open", GUILayout.Width(110)))
        {
            if (MapPipeline.CreateOrOpenScene(newSceneName, out string error))
            {
                MapPipeline.AddSceneToBuildSettings(newSceneName);
                lastResult = $"Scene ready: {newSceneName}";
            }
            else if (!string.IsNullOrEmpty(error))
            {
                lastResult = error;
            }
            GUIUtility.ExitGUI();
        }
        EditorGUILayout.EndHorizontal();

        if (saved && !MapPipeline.IsSceneInBuildSettings(sceneName))
        {
            EditorGUILayout.HelpBox("Scene is not in Build Settings — gates cannot load this map.", MessageType.Warning);
            if (GUILayout.Button("Add to Build Settings"))
                MapPipeline.AddSceneToBuildSettings(sceneName);
        }

        DrawSetupCopy(sceneName, saved);

        EndStep();
    }

    // 새 씬은 기본 카메라/조명뿐이라 기존 맵과 룩이 어긋난다. 이미 만들어 둔 맵 씬을 본떠 맞춘다.
    private void DrawSetupCopy(string sceneName, bool saved)
    {
        var references = mapEntries
            .Where(m => !MapJsonUpdater.IsMapForScene(m, sceneName) && MapPipeline.FindSceneAssetPath(m) != null)
            .ToList();

        if (references.Count == 0)
            return;

        EditorGUILayout.BeginHorizontal();

        var labels = references.Select(m => MapPipeline.SceneNameOf(m)).ToArray();
        setupReferenceIndex = Mathf.Clamp(setupReferenceIndex, 0, labels.Length - 1);
        setupReferenceIndex = EditorGUILayout.Popup("Copy setup from", setupReferenceIndex, labels);

        GUI.enabled = saved;
        if (GUILayout.Button("Copy", GUILayout.Width(70)))
        {
            var reference = references[setupReferenceIndex];
            string path = MapPipeline.FindSceneAssetPath(reference);

            bool replace = EditorUtility.DisplayDialog("Copy scene setup",
                $"Copy cameras, lights and render settings from '{MapPipeline.SceneNameOf(reference)}'.\n\n" +
                "Replace the cameras/lights already in this scene?",
                "Replace", "Keep both");

            if (MapPipeline.CopySceneSetupFrom(path, replace, out string summary, out string error))
                lastResult = summary;
            else
                lastResult = error;

            GUIUtility.ExitGUI();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Cameras, lights and render settings only — terrain and markers are not copied.", EditorStyles.miniLabel);
    }

    // 활성 씬을 Map.json 에 등록된 맵 중에서 골라 연다.
    // 맵을 오가며 게이트를 잇는 작업이 잦아서, 씬 창을 뒤지지 않고 여기서 바로 전환할 수 있어야 한다.
    private void DrawActiveSceneSelector(string sceneName)
    {
        int current = mapEntries.FindIndex(m =>
            MapJsonUpdater.IsMapForScene(m, sceneName));

        EditorGUILayout.BeginHorizontal();

        if (mapEntries.Count == 0)
        {
            EditorGUILayout.LabelField("Active scene", string.IsNullOrEmpty(sceneName) ? "(unsaved)" : sceneName);
        }
        else
        {
            // 현재 씬이 Map.json 에 없으면 그 사실이 보이도록 맨 앞에 항목을 하나 끼운다.
            bool unregistered = current < 0;
            string[] options = unregistered
                ? new[] { $"(not in Map.json) {(string.IsNullOrEmpty(sceneName) ? "unsaved" : sceneName)}" }
                    .Concat(mapLabels).ToArray()
                : mapLabels;

            int selected = unregistered ? 0 : current;
            int picked = EditorGUILayout.Popup("Active scene", selected, options);

            if (picked != selected)
            {
                int mapIndex = unregistered ? picked - 1 : picked;
                if (mapIndex >= 0 && mapIndex < mapEntries.Count)
                {
                    if (MapPipeline.OpenSceneOf(mapEntries[mapIndex], out string error))
                    {
                        lastResult = $"Opened scene: {MapPipeline.SceneNameOf(mapEntries[mapIndex])}";
                        GUIUtility.ExitGUI();
                    }
                    else if (!string.IsNullOrEmpty(error))
                    {
                        lastResult = error;
                    }
                }
            }
        }

        if (GUILayout.Button("↻", GUILayout.Width(24)))
            RefreshMapEntries();

        EditorGUILayout.EndHorizontal();
    }

    // -- Step 2 --
    private void DrawTerrainStep()
    {
        int meshCount = MapPipeline.CountSceneMeshes();
        BeginStep(2, "Terrain", meshCount > 0 ? StepState.Done : StepState.Todo);

        EditorGUILayout.LabelField("Scene meshes", $"{meshCount}");

        // What to build stays visible; the tuning values fold away.
        terrain.preset = (TerrainBuilder.Preset)EditorGUILayout.EnumPopup("Preset", terrain.preset);
        terrain.size = EditorGUILayout.Vector3Field("Size", terrain.size);

        showTerrainDetails = EditorGUILayout.Foldout(showTerrainDetails, "Details", true);
        if (showTerrainDetails)
        {
            EditorGUI.indentLevel++;
            terrain.borderWalls = EditorGUILayout.Toggle("Border walls", terrain.borderWalls);

            if (terrain.preset == TerrainBuilder.Preset.Obstacles)
            {
                terrain.obstacleCount = EditorGUILayout.IntField("Obstacle count", terrain.obstacleCount);
                if (terrain.obstacleCount > MapPipeline.SuggestedMaxMeshCount)
                {
                    EditorGUILayout.HelpBox(
                        $"More than about {MapPipeline.SuggestedMaxMeshCount} obstacles will not fit in a single-tile navmesh (step 3).",
                        MessageType.Warning);
                }
                terrain.obstacleSize = EditorGUILayout.FloatField("Obstacle size", terrain.obstacleSize);
                terrain.rampCount = EditorGUILayout.IntField("Ramp count", terrain.rampCount);
                terrain.rampHeight = EditorGUILayout.FloatField("Ramp height", terrain.rampHeight);
            }
            else if (terrain.preset == TerrainBuilder.Preset.Maze)
            {
                terrain.mazeGridSize = EditorGUILayout.IntField("Maze grid", terrain.mazeGridSize);
                terrain.wallHeight = EditorGUILayout.FloatField("Wall height", terrain.wallHeight);
                terrain.wallThickness = EditorGUILayout.FloatField("Wall thickness", terrain.wallThickness);
            }

            terrain.randomSeed = EditorGUILayout.IntField("Random seed (0 = random)", terrain.randomSeed);

            terrain.groundColor = EditorGUILayout.ColorField("Ground color", terrain.groundColor);
            terrain.obstacleColor = EditorGUILayout.ColorField("Obstacle color", terrain.obstacleColor);
            EditorGUILayout.LabelField("Colors become materials under Assets/Materials/MapTool.", EditorStyles.miniLabel);

            terrain.groundMaterial = (Material)EditorGUILayout.ObjectField("Ground material (optional)", terrain.groundMaterial, typeof(Material), false);
            terrain.obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacle material (optional)", terrain.obstacleMaterial, typeof(Material), false);
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button($"Generate terrain (replaces '{TerrainBuilder.RootName}' root)"))
        {
            var root = TerrainBuilder.Build(terrain);
            Selection.activeGameObject = root;
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            lastResult = $"Terrain generated: {terrain.preset}, {MapPipeline.CountSceneMeshes()} meshes";
        }

        EditorGUILayout.LabelField("Changing terrain means the navmesh must be baked again.", EditorStyles.miniLabel);
        EndStep();
    }

    // -- Step 3 --
    private void DrawNavMeshStep(string sceneName)
    {
        string navMeshPath = MapPipeline.NavMeshPathOf(sceneName);
        bool exists = File.Exists(navMeshPath);
        bool stale = MapPipeline.IsNavMeshStale(sceneName);
        BeginStep(3, "NavMesh", !exists ? StepState.Todo : (stale ? StepState.Stale : StepState.Done));

        EditorGUILayout.LabelField("Output", navMeshPath);
        if (!exists)
            EditorGUILayout.HelpBox("No navmesh yet — the server cannot load this map.", MessageType.Warning);
        else if (stale)
            EditorGUILayout.HelpBox("Scene is newer than the navmesh. Bake again.", MessageType.Warning);

        DrawNavMeshBudget();

        showNavMeshSettings = EditorGUILayout.Foldout(showNavMeshSettings, "Details (bake parameters)", true);
        if (showNavMeshSettings)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.LabelField("These values drive server movement and collision directly.", EditorStyles.miniLabel);
            navMesh.cellSize = EditorGUILayout.FloatField("Cell size", navMesh.cellSize);
            navMesh.cellHeight = EditorGUILayout.FloatField("Cell height", navMesh.cellHeight);
            navMesh.walkableSlopeAngle = EditorGUILayout.FloatField("Walkable slope", navMesh.walkableSlopeAngle);
            navMesh.walkableHeight = EditorGUILayout.FloatField("Agent height", navMesh.walkableHeight);
            navMesh.walkableRadius = EditorGUILayout.FloatField("Agent radius", navMesh.walkableRadius);
            navMesh.walkableClimb = EditorGUILayout.FloatField("Agent climb", navMesh.walkableClimb);
            if (GUILayout.Button("Reset to defaults"))
                navMesh.CopyFrom(new NavMeshBakeSettings());
            EditorGUI.indentLevel--;
        }

        if (GUILayout.Button("Bake navmesh (scene -> OBJ -> navmesh)"))
            BakeNavMesh(sceneName);

        EndStep();
    }

    // Recast 는 맵 하나를 타일 하나로 굽는다. 장애물이 많으면 정점 한계에 걸려 실패하는데,
    // 굽는 데 수십 초가 걸린 뒤에야 알 수 있으므로 미리 보여 준다.
    private void DrawNavMeshBudget()
    {
        var budget = MapPipeline.EstimateBudget(navMesh);
        if (budget.meshCount == 0)
            return;

        EditorGUILayout.LabelField("Estimated cost",
            $"{budget.gridWidth} x {budget.gridHeight} cells · ~{budget.estimatedVerts:N0} / {MapPipeline.MaxContourVerts:N0} verts");

        if (budget.OverBudget)
        {
            EditorGUILayout.HelpBox(
                $"Too many objects ({budget.meshCount}) for a single-tile navmesh — the bake will fail with " +
                $"\"Could not triangulate contours.\"\nReduce to about {budget.SuggestedMeshCount} objects, or raise Cell size.",
                MessageType.Error);
        }
        else if (budget.NearBudget)
        {
            EditorGUILayout.HelpBox(
                $"Close to Recast's {MapPipeline.MaxContourVerts:N0} vertex limit for a single-tile navmesh. " +
                "Adding more obstacles may make the bake fail.",
                MessageType.Warning);
        }

        if (budget.HeavyGrid)
        {
            EditorGUILayout.HelpBox(
                $"{budget.cellCount:N0} heightfield cells — the bake will take a while and use a lot of memory. " +
                "A larger Cell size makes it much cheaper.",
                MessageType.Info);
        }
    }

    private bool BakeNavMesh(string sceneName)
    {
        if (!MapPipeline.ExportSceneObj(sceneName, out string objPath, out string error))
        {
            lastResult = $"OBJ export failed: {error}";
            return false;
        }

        if (!MapPipeline.GenerateNavMesh(sceneName, objPath, navMesh, out error))
        {
            lastResult = $"NavMesh bake failed: {error}";
            return false;
        }

        lastResult = $"NavMesh baked: {MapPipeline.NavMeshPathOf(sceneName)}";
        return true;
    }

    // -- Step 4 --
    private void DrawMapJsonStep(string sceneName)
    {
        // 캐시된 목록에서 찾는다(OnGUI 마다 Map.json 을 다시 읽지 않기 위해).
        var map = mapEntries.FirstOrDefault(m => MapJsonUpdater.IsMapForScene(m, sceneName));
        bool navMeshStale = MapPipeline.IsGameDataNavMeshStale(sceneName);
        BeginStep(4, "Map.json", map == null ? StepState.Todo : (navMeshStale ? StepState.Stale : StepState.Done));

        if (map == null)
        {
            EditorGUILayout.HelpBox("No map entry for this scene yet — registering creates one.", MessageType.Warning);
        }
        else
        {
            EditorGUILayout.LabelField("Map", $"id {map.id} · {map.name}");
            EditorGUILayout.LabelField("navmesh_path", string.IsNullOrEmpty(map.navmesh_path) ? "(none)" : map.navmesh_path);
            EditorGUILayout.LabelField("Gates / spawns",
                $"{map.gates.Count} / {map.spawn_points.player_spawn.Count + map.spawn_points.monster_spawn.Count + map.spawn_points.boss_spawn.Count}");

            if (navMeshStale)
                EditorGUILayout.HelpBox("GameData navmesh copy is missing or outdated — that is the copy the server reads.", MessageType.Warning);
        }

        scanMarkers = EditorGUILayout.Toggle("Scan markers (gates / spawns / objects)", scanMarkers);

        if (GUILayout.Button("Register to Map.json + copy navmesh"))
            RegisterMap(sceneName);

        DrawMapFields(map);
        DrawMarkerTools(sceneName);

        EditorGUILayout.LabelField("Deploying to server/client still needs GameDataFlow.", EditorStyles.miniLabel);
        EndStep();
    }

    // Map.json 의 맵 컬럼 편집. 마커(게이트/스폰/오브젝트)는 씬에서 편집하고 스캔으로 반영하므로
    // 여기서는 씬으로 표현되지 않는 값들만 다룬다.
    private void DrawMapFields(MapJsonUpdater.MapData map)
    {
        if (map == null)
            return;

        showMapFields = EditorGUILayout.Foldout(showMapFields, "Map fields", true);
        if (!showMapFields)
            return;

        // 선택된 맵이 바뀌면 편집 버퍼를 다시 채운다.
        if (mapFields == null || mapFieldsLoadedFor != map.id)
        {
            mapFields = MapPipeline.MapMetadata.From(map);
            mapFieldsLoadedFor = map.id;
        }

        EditorGUI.indentLevel++;

        EditorGUILayout.LabelField("id", map.id.ToString());
        mapFields.name = EditorGUILayout.TextField("name", mapFields.name);
        mapFields.scene = EditorGUILayout.TextField("scene", mapFields.scene);
        mapFields.name_id = EditorGUILayout.TextField("name_id", mapFields.name_id);
        mapFields.desc_id = EditorGUILayout.TextField("desc_id", mapFields.desc_id);
        mapFields.game_mode_id = EditorGUILayout.IntField("game_mode_id", mapFields.game_mode_id);

        EditorGUILayout.BeginHorizontal();
        mapFields.width = EditorGUILayout.DoubleField("size (w x h)", mapFields.width);
        mapFields.height = EditorGUILayout.DoubleField(mapFields.height);
        if (GUILayout.Button("From bounds", GUILayout.Width(90)))
        {
            var bounds = MapPipeline.SceneBounds();
            if (bounds.HasValue)
            {
                mapFields.width = System.Math.Round(bounds.Value.size.x, 2);
                mapFields.height = System.Math.Round(bounds.Value.size.z, 2);
            }
        }
        EditorGUILayout.EndHorizontal();

        mapFields.navmesh_path = EditorGUILayout.TextField("navmesh_path", mapFields.navmesh_path);
        mapFields.aoi_radius = EditorGUILayout.IntField("aoi_radius (0 = off)", mapFields.aoi_radius);
        EditorGUILayout.LabelField("aoi_radius: server interest radius; 0 broadcasts the whole map.", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Save fields to Map.json"))
        {
            if (MapPipeline.UpdateMapMetadata(map.id, mapFields, out string error))
            {
                RefreshMapEntries();
                lastResult = $"Map fields saved (id {map.id}).";
            }
            else
            {
                lastResult = error;
            }
        }
        if (GUILayout.Button("Revert", GUILayout.Width(80)))
        {
            mapFields = MapPipeline.MapMetadata.From(map);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.LabelField("Markers are not edited here — edit them in the scene and press Register.", EditorStyles.miniLabel);
        EditorGUI.indentLevel--;
    }

    // Marker placement and JSON->scene rebuild used to live in the Map JSON Updater window.
    private void DrawMarkerTools(string sceneName)
    {
        showMarkers = EditorGUILayout.Foldout(showMarkers, "Markers", true);
        if (!showMarkers)
            return;

        EditorGUI.indentLevel++;
        EditorGUILayout.LabelField("Placed at the scene view pivot, under the MapDesign root.", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Gate"))
            MapJsonUpdater.CreateGateMarker();
        if (GUILayout.Button("Player spawn"))
            MapJsonUpdater.CreateSpawnMarker(SpawnType.Player);
        if (GUILayout.Button("Monster spawn"))
            MapJsonUpdater.CreateSpawnMarker(SpawnType.Monster);
        if (GUILayout.Button("Boss spawn"))
            MapJsonUpdater.CreateSpawnMarker(SpawnType.Boss);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Static object"))
            MapJsonUpdater.CreateObjectMarker(MapObjectKind.Static);
        if (GUILayout.Button("Movable object"))
            MapJsonUpdater.CreateObjectMarker(MapObjectKind.Movable);
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Rebuild scene markers from Map.json"))
            MapJsonUpdater.SyncSceneFromJson(sceneName);

        bool auto = MapJsonAutoSync.Enabled;
        bool newAuto = EditorGUILayout.ToggleLeft("Auto sync scene and Map.json", auto);
        if (newAuto != auto)
            MapJsonAutoSync.Enabled = newAuto;

        EditorGUI.indentLevel--;
    }

    private bool RegisterMap(string sceneName)
    {
        if (!MapPipeline.RegisterToMapJson(sceneName, scanMarkers, true, out string summary, out string error))
        {
            lastResult = $"Register failed: {error}";
            return false;
        }

        RefreshMapEntries();
        lastResult = $"Registered to Map.json — {summary}";
        return true;
    }

    // -- Run everything --
    private void DrawRunAll(string sceneName)
    {
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("Run", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Leaves terrain alone; re-syncs navmesh and Map.json.", EditorStyles.miniLabel);

        GUI.enabled = !string.IsNullOrEmpty(EditorSceneManager.GetActiveScene().path);
        if (GUILayout.Button("Bake navmesh -> register to Map.json", GUILayout.Height(28)))
        {
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            if (BakeNavMesh(sceneName))
                RegisterMap(sceneName);
        }
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    // -- Shared UI --
    private void BeginStep(int index, string title, StepState state)
    {
        EditorGUILayout.BeginVertical("box");

        Color previous = GUI.color;
        GUI.color = state == StepState.Done ? new Color(0.6f, 1f, 0.6f)
                  : state == StepState.Stale ? new Color(1f, 0.9f, 0.5f)
                  : new Color(1f, 0.7f, 0.7f);

        string mark = state == StepState.Done ? "done" : state == StepState.Stale ? "needs update" : "todo";
        EditorGUILayout.LabelField($"{index}. {title} — {mark}", EditorStyles.boldLabel);
        GUI.color = previous;
    }

    private void EndStep()
    {
        EditorGUILayout.EndVertical();
    }
}
