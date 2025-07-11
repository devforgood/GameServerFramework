using UnityEngine;
using UnityEditor;
using System.IO;
using System.Runtime.InteropServices;

namespace RecastNavigation.Unity
{
    public class RecastNavigationEditor : EditorWindow
    {
        private string objFilePath = "";
        private string outputPath = "Assets/GeneratedNavMeshes";
        private bool showAdvancedSettings = false;
        
        // NavMesh generation parameters
        private float cellSize = 0.3f;
        private float cellHeight = 0.2f;
        private float walkableSlopeAngle = 45.0f;
        private float walkableHeight = 2.0f;
        private float walkableRadius = 0.6f;
        private float walkableClimb = 0.9f;
        private float minRegionArea = 8.0f;
        private float mergeRegionArea = 20.0f;
                        private float maxSimplificationError = 1.3f;
                private float maxEdgeLen = 12.0f;
        private float detailSampleDistance = 6.0f;
        private float detailSampleMaxError = 1.0f;
        
        // Visualization settings
        private bool showNavMesh = true;
        private bool showWalkableAreas = true;
        private bool showUnwalkableAreas = true;
        private Color navMeshColor = Color.green;
        private Color walkableColor = Color.blue;
        private Color unwalkableColor = Color.red;
        
        private Vector2 scrollPosition;
        private NavMeshData currentNavMeshData;
        
        // OBJ Export settings
        private int selectedExportMode = 1; // 0: Separate, 1: Whole Single (default), 2: Each Single
        private readonly string[] exportModeOptions = {
            "Export Selection To Separate Files",
            "Export Whole Selection To Single File", 
            "Export Each Selection To Single File"
        };
        
        [MenuItem("Tools/RecastNavigation/Open NavMesh Generator")]
        public static void ShowWindow()
        {
            GetWindow<RecastNavigationEditor>("RecastNavigation Generator");
        }
        
        private void OnEnable()
        {
            if (string.IsNullOrEmpty(outputPath))
            {
                outputPath = "Assets/GeneratedNavMeshes";
            }
        }
        
        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("RecastNavigation NavMesh Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            
            // OBJ Export Tools (at the top)
            EditorGUILayout.LabelField("OBJ Export Tools", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Select objects in the scene to export them as OBJ files.", MessageType.Info);
            EditorGUILayout.Space();
            
            // Check if any objects are selected
            bool hasSelection = Selection.transforms.Length > 0;
            
            // Export mode selection
            selectedExportMode = EditorGUILayout.Popup("Export Mode:", selectedExportMode, exportModeOptions);
            
            GUI.enabled = hasSelection;
            if (GUILayout.Button("Export OBJ", GUILayout.Height(30)))
            {
                ExportOBJ();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            EditorGUILayout.Space();
            
            // File selection
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("OBJ File:", GUILayout.Width(100));
            objFilePath = EditorGUILayout.TextField(objFilePath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFilePanel("Select OBJ File", "", "obj");
                if (!string.IsNullOrEmpty(path))
                {
                    objFilePath = path;
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Output Path:", GUILayout.Width(100));
            outputPath = EditorGUILayout.TextField(outputPath, GUILayout.ExpandWidth(true));
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.SaveFolderPanel("Select Output Directory", Application.dataPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    // Assets 이하 상대경로로 변환
                    if (path.StartsWith(Application.dataPath))
                    {
                        outputPath = "Assets" + path.Substring(Application.dataPath.Length).Replace("\\", "/");
                    }
                    else
                    {
                        outputPath = path;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space();
            
            // Generate button
            GUI.enabled = !string.IsNullOrEmpty(objFilePath) && File.Exists(objFilePath);
            if (GUILayout.Button("Generate NavMesh", GUILayout.Height(30)))
            {
                GenerateNavMesh();
            }
            GUI.enabled = true;
            
            EditorGUILayout.Space();
            
            // Advanced settings
            showAdvancedSettings = EditorGUILayout.Foldout(showAdvancedSettings, "Advanced Settings");
            if (showAdvancedSettings)
            {
                EditorGUI.indentLevel++;
                
                EditorGUILayout.LabelField("Rasterization", EditorStyles.boldLabel);
                cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
                cellHeight = EditorGUILayout.FloatField("Cell Height", cellHeight);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Agent", EditorStyles.boldLabel);
                walkableSlopeAngle = EditorGUILayout.FloatField("Walkable Slope Angle", walkableSlopeAngle);
                walkableHeight = EditorGUILayout.FloatField("Walkable Height", walkableHeight);
                walkableRadius = EditorGUILayout.FloatField("Walkable Radius", walkableRadius);
                walkableClimb = EditorGUILayout.FloatField("Walkable Climb", walkableClimb);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Region", EditorStyles.boldLabel);
                minRegionArea = EditorGUILayout.FloatField("Min Region Area", minRegionArea);
                mergeRegionArea = EditorGUILayout.FloatField("Merge Region Area", mergeRegionArea);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Polygonization", EditorStyles.boldLabel);
                maxSimplificationError = EditorGUILayout.FloatField("Max Simplification Error", maxSimplificationError);
                maxEdgeLen = EditorGUILayout.FloatField("Max Edge Length", maxEdgeLen);
                
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Detail Mesh", EditorStyles.boldLabel);
                detailSampleDistance = EditorGUILayout.FloatField("Detail Sample Distance", detailSampleDistance);
                detailSampleMaxError = EditorGUILayout.FloatField("Detail Sample Max Error", detailSampleMaxError);
                
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
            
            // Visualization settings
            EditorGUILayout.LabelField("Visualization", EditorStyles.boldLabel);
            showNavMesh = EditorGUILayout.Toggle("Show NavMesh", showNavMesh);
            showWalkableAreas = EditorGUILayout.Toggle("Show Walkable Areas", showWalkableAreas);
            showUnwalkableAreas = EditorGUILayout.Toggle("Show Unwalkable Areas", showUnwalkableAreas);
            
            if (showNavMesh)
            {
                navMeshColor = EditorGUILayout.ColorField("NavMesh Color", navMeshColor);
            }
            if (showWalkableAreas)
            {
                walkableColor = EditorGUILayout.ColorField("Walkable Color", walkableColor);
            }
            if (showUnwalkableAreas)
            {
                unwalkableColor = EditorGUILayout.ColorField("Unwalkable Color", unwalkableColor);
            }
            
            EditorGUILayout.EndScrollView();
        }
        
        private void ExportOBJ()
        {
            string exportedFilePath = "";
            
            switch (selectedExportMode)
            {
                case 0:
                    exportedFilePath = ObjExportor.ExportSelectionToSeparate();
                    break;
                case 1:
                    exportedFilePath = ObjExportor.ExportWholeSelectionToSingle();
                    break;
                case 2:
                    exportedFilePath = ObjExportor.ExportEachSelectionToSingle();
                    break;
                default:
                    exportedFilePath = ObjExportor.ExportWholeSelectionToSingle();
                    break;
            }
            
            // Export가 성공적으로 완료되면 OBJ File 필드에 경로 설정
            if (!string.IsNullOrEmpty(exportedFilePath) && File.Exists(exportedFilePath))
            {
                // 절대경로라면 Assets/GeneratedObj/xxx.obj 형태로 변환
                if (exportedFilePath.StartsWith(Application.dataPath))
                {
                    string relPath = "Assets" + exportedFilePath.Substring(Application.dataPath.Length).Replace("\\", "/");
                    objFilePath = relPath;
                }
                else
                {
                    objFilePath = exportedFilePath;
                }
                Debug.Log($"Exported OBJ file path set to: {objFilePath}");
                Repaint();
            }
        }
        
        private void GenerateNavMesh()
        {
            if (string.IsNullOrEmpty(objFilePath) || !File.Exists(objFilePath))
            {
                EditorUtility.DisplayDialog("Error", "Please select a valid OBJ file.", "OK");
                return;
            }
            
            if (string.IsNullOrEmpty(outputPath))
            {
                EditorUtility.DisplayDialog("Error", "Please select an output directory.", "OK");
                return;
            }
            
            try
            {
                // Create output directory if it doesn't exist
                if (!Directory.Exists(outputPath))
                {
                    Directory.CreateDirectory(outputPath);
                }
                
                // Generate navmesh using RecastNavigation
                string outputFile = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(objFilePath) + "_navmesh.bin");
                
                bool success = RecastNavigationWrapper.GenerateNavMesh(
                    objFilePath,
                    outputFile,
                    cellSize,
                    cellHeight,
                    walkableSlopeAngle,
                    walkableHeight,
                    walkableRadius,
                    walkableClimb,
                    minRegionArea,
                    mergeRegionArea,
                    maxSimplificationError,
                    maxEdgeLen,
                    detailSampleDistance,
                    detailSampleMaxError
                );
                
                if (success)
                {
                    EditorUtility.DisplayDialog("Success", $"NavMesh generated successfully!\nOutput: {outputFile}", "OK");
                    
                    // Load and visualize the generated navmesh
                    LoadAndVisualizeNavMesh(outputFile);
                    
                    // Refresh the asset database
                    AssetDatabase.Refresh();
                }
                else
                {
                    EditorUtility.DisplayDialog("Error", "Failed to generate NavMesh. Check the console for details.", "OK");
                }
            }
            catch (System.Exception e)
            {
                EditorUtility.DisplayDialog("Error", $"An error occurred: {e.Message}", "OK");
                Debug.LogError($"RecastNavigation Error: {e}");
            }
        }
        
        private void LoadAndVisualizeNavMesh(string navMeshFile)
        {
            if (File.Exists(navMeshFile))
            {
                currentNavMeshData = RecastNavigationWrapper.LoadNavMesh(navMeshFile);
                if (currentNavMeshData != null)
                {
                    // Create a GameObject to hold the navmesh visualization
                    GameObject navMeshVisualizer = GameObject.Find("NavMeshVisualizer");
                    if (navMeshVisualizer == null)
                    {
                        navMeshVisualizer = new GameObject("NavMeshVisualizer");
                    }
                    
                    // Add or get the NavMeshVisualizer component
                    NavMeshVisualizer visualizer = navMeshVisualizer.GetComponent<NavMeshVisualizer>();
                    if (visualizer == null)
                    {
                        visualizer = navMeshVisualizer.AddComponent<NavMeshVisualizer>();
                    }
                    
                    visualizer.SetNavMeshData(currentNavMeshData);
                    visualizer.SetVisualizationSettings(showNavMesh, showWalkableAreas, showUnwalkableAreas, 
                                                       navMeshColor, walkableColor, unwalkableColor);
                    
                    // Select the visualizer in the hierarchy
                    Selection.activeGameObject = navMeshVisualizer;
                    
                    Debug.Log($"NavMesh loaded and visualized: {navMeshFile}");
                }
            }
        }
        
        private void OnSceneGUI(SceneView sceneView)
        {
            // This will be called when drawing in the Scene view
            if (currentNavMeshData != null && showNavMesh)
            {
                DrawNavMeshInScene();
            }
        }
        
        private void DrawNavMeshInScene()
        {
            // Draw navmesh polygons in the scene view
            if (currentNavMeshData.polygons != null)
            {
                Handles.color = navMeshColor;
                for (int i = 0; i < currentNavMeshData.polygons.Length; i++)
                {
                    var polygon = currentNavMeshData.polygons[i];
                    if (polygon.vertices != null && polygon.vertices.Length >= 3)
                    {
                        Vector3[] vertices = new Vector3[polygon.vertices.Length];
                        for (int j = 0; j < polygon.vertices.Length; j++)
                        {
                            vertices[j] = new Vector3(polygon.vertices[j].x, polygon.vertices[j].y, polygon.vertices[j].z);
                        }
                        
                        Handles.DrawPolyLine(vertices);
                        Handles.DrawLine(vertices[vertices.Length - 1], vertices[0]);
                    }
                }
            }
        }
    }
} 