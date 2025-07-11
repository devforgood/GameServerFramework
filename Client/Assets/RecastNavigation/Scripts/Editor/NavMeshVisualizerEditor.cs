using UnityEngine;
using UnityEditor;

namespace RecastNavigation.Unity
{
    [CustomEditor(typeof(NavMeshVisualizer))]
    public class NavMeshVisualizerEditor : Editor
    {
        private NavMeshVisualizer visualizer;
        
        private void OnEnable()
        {
            visualizer = (NavMeshVisualizer)target;
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            
            if (GUILayout.Button("Refresh Visualization"))
            {
                visualizer.RefreshVisualization();
            }
            
            if (GUILayout.Button("Clear Visualization"))
            {
                visualizer.ClearVisualization();
            }
            
            EditorGUILayout.Space();
            
            // Add buttons for loading navmesh from file
            if (GUILayout.Button("Load NavMesh from File"))
            {
                string path = EditorUtility.OpenFilePanel("Select NavMesh File", "", "bin");
                if (!string.IsNullOrEmpty(path))
                {
                    NavMeshData data = RecastNavigationWrapper.LoadNavMesh(path);
                    if (data != null)
                    {
                        visualizer.SetNavMeshData(data);
                        EditorUtility.SetDirty(visualizer);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Error", "Failed to load NavMesh file", "OK");
                    }
                }
            }
        }
        
        private void OnSceneGUI()
        {
            if (visualizer == null) return;
            
            // Draw additional scene GUI elements here if needed
            Handles.BeginGUI();
            
            GUILayout.BeginArea(new Rect(Screen.width - 320, 10, 300, 200));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("NavMesh Visualizer", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Toggle Debug Info"))
            {
                visualizer.ToggleDebugInfo();
            }
            
            if (GUILayout.Button("Toggle Polygon Centers"))
            {
                visualizer.TogglePolygonCenters();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
            
            Handles.EndGUI();
        }
    }
} 