using UnityEngine;
using RecastNavigation.Unity;
using System.IO;

public class NavMeshGenerator : MonoBehaviour
{
    [Header("NavMesh Generation Settings")]
    public string objFileName = "nav_test.obj";
    public string outputFileName = "test_navmesh.bin";
    
    [Header("Build Parameters")]
    public float cellSize = 0.3f;
    public float cellHeight = 0.2f;
    public float walkableSlopeAngle = 45f;
    public float walkableHeight = 2.0f;
    public float walkableRadius = 0.6f;
    public float walkableClimb = 0.9f;
    public float minRegionArea = 8f;
    public float mergeRegionArea = 20f;
    public float maxSimplificationError = 1.3f;
    public float maxEdgeLen = 12f;
    public float detailSampleDistance = 6f;
    public float detailSampleMaxError = 1f;

    [Header("Test Results")]
    public NavMeshData loadedNavMesh;
    public bool generationSuccess = false;
    public bool loadingSuccess = false;

    void Start()
    {
        GenerateAndLoadNavMesh();
    }

    [ContextMenu("Generate NavMesh")]
    public void GenerateAndLoadNavMesh()
    {
        // Get paths
        string objPath = Path.Combine(Application.dataPath, "..", "RecastDemo", "Bin", "Meshes", objFileName);
        string outputPath = Path.Combine(Application.dataPath, "GeneratedNavMeshes", outputFileName);
        
        Debug.Log($"OBJ Path: {objPath}");
        Debug.Log($"Output Path: {outputPath}");
        
        // Ensure output directory exists
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        
        // Generate NavMesh
        Debug.Log("Starting NavMesh generation...");
        generationSuccess = RecastNavigationWrapper.GenerateNavMesh(
            objPath,
            outputPath,
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
        
        if (generationSuccess)
        {
            Debug.Log("NavMesh generation completed successfully!");
            
            // Load the generated NavMesh
            Debug.Log("Loading generated NavMesh...");
            loadedNavMesh = RecastNavigationWrapper.LoadNavMesh(outputPath);
            
            if (loadedNavMesh != null)
            {
                loadingSuccess = true;
                Debug.Log("NavMesh loaded successfully!");
                
                // Log NavMesh info
                RecastNavigationWrapper.LogNavMeshInfo(loadedNavMesh);
            }
            else
            {
                Debug.LogError("Failed to load generated NavMesh!");
            }
        }
        else
        {
            Debug.LogError("NavMesh generation failed!");
        }
    }

    [ContextMenu("Load Existing NavMesh")]
    public void LoadExistingNavMesh()
    {
        string navMeshPath = Path.Combine(Application.dataPath, "GeneratedNavMeshes", outputFileName);
        
        if (File.Exists(navMeshPath))
        {
            Debug.Log($"Loading existing NavMesh from: {navMeshPath}");
            loadedNavMesh = RecastNavigationWrapper.LoadNavMesh(navMeshPath);
            
            if (loadedNavMesh != null)
            {
                loadingSuccess = true;
                Debug.Log("Existing NavMesh loaded successfully!");
                RecastNavigationWrapper.LogNavMeshInfo(loadedNavMesh);
            }
            else
            {
                Debug.LogError("Failed to load existing NavMesh!");
            }
        }
        else
        {
            Debug.LogError($"NavMesh file not found: {navMeshPath}");
        }
    }

    void OnDestroy()
    {
        // Cleanup
        if (loadedNavMesh != null)
        {
            RecastNavigationWrapper.UnloadNavMesh(loadedNavMesh);
        }
    }

    void OnDrawGizmos()
    {
        if (loadedNavMesh != null && loadedNavMesh.polygons != null)
        {
            // Draw NavMesh bounds
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(loadedNavMesh.bounds.center, loadedNavMesh.bounds.size);
            
            // Draw polygons
            Gizmos.color = Color.blue;
            foreach (var polygon in loadedNavMesh.polygons)
            {
                if (polygon.vertices != null && polygon.vertices.Length >= 3)
                {
                    for (int i = 0; i < polygon.vertices.Length; i++)
                    {
                        Vector3 current = polygon.vertices[i];
                        Vector3 next = polygon.vertices[(i + 1) % polygon.vertices.Length];
                        Gizmos.DrawLine(current, next);
                    }
                }
            }
        }
    }
} 