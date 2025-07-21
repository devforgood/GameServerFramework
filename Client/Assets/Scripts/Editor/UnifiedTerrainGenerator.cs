using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace GameServerFramework.Editor
{
    public class UnifiedTerrainGenerator : EditorWindow
    {
        private enum TerrainType
        {
            SimplePlane,
            Maze,
            City,
            Forest,
            Mountain,
            Battlefield,
            Dungeon,
            Custom
        }

        private enum GeneratorMode
        {
            Simple,
            Advanced
        }

        private enum PathfindingTestType
        {
            SimplePathfinding,
            ComplexMaze,
            MultiLevel,
            DynamicObstacles,
            PerformanceTest
        }

        // Mode selection
        private GeneratorMode currentMode = GeneratorMode.Simple;
        
        // Terrain settings
        private TerrainType selectedTerrainType = TerrainType.SimplePlane;
        private PathfindingTestType selectedTestType = PathfindingTestType.SimplePathfinding;
        private Vector3 terrainSize = new Vector3(50f, 10f, 50f);
        private int gridSize = 10;
        private float wallHeight = 3f;
        private float wallThickness = 0.2f;
        
        // Basic settings (Simple mode)
        private bool generateObstacles = true;
        private int obstacleCount = 20;
        private float obstacleSize = 2f;
        private bool generateRamps = true;
        private int rampCount = 5;
        private float rampHeight = 2f;
        private bool generateWater = false;
        private float waterLevel = 0f;
        
        // Advanced settings (Advanced mode)
        private bool generateElevation = true;
        private float maxElevation = 10f;
        private bool generateTunnels = false;
        private int tunnelCount = 3;
        private bool generateBridges = false;
        private int bridgeCount = 2;
        
        // Material settings
        private Material terrainMaterial;
        private Material obstacleMaterial;
        private Material waterMaterial;
        private Material bridgeMaterial;
        private Material tunnelMaterial;
        
        // Performance settings (Advanced mode)
        private bool optimizeForPerformance = true;
        private bool generateLOD = true;
        private bool useStaticBatching = true;
        
        // Visualization settings (Advanced mode)
        private bool showPathfindingNodes = false;
        private bool showGrid = false;
        private Color gridColor = Color.white;
        private Color nodeColor = Color.yellow;

        // UI state
        private Vector2 scrollPosition;

        [MenuItem("Tools/Terrain Generator")]
        public static void ShowWindow()
        {
            GetWindow<UnifiedTerrainGenerator>("Terrain Generator");
        }

        private void OnEnable()
        {
            // Load default materials with error handling
            try
            {
                terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/TerrainMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load TerrainMaterial.mat: {e.Message}");
                terrainMaterial = null;
            }
            
            try
            {
                obstacleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ObstacleMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load ObstacleMaterial.mat: {e.Message}");
                obstacleMaterial = null;
            }
            
            try
            {
                waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/WaterMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load WaterMaterial.mat: {e.Message}");
                waterMaterial = null;
            }
            
            try
            {
                bridgeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ObstacleMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load bridge material: {e.Message}");
                bridgeMaterial = null;
            }
            
            try
            {
                tunnelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/TerrainMaterial.mat");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load tunnel material: {e.Message}");
                tunnelMaterial = null;
            }
            
            // Create default materials if they don't exist
            if (terrainMaterial == null)
            {
                terrainMaterial = CreateDefaultMaterial("TerrainMaterial", new Color(0.5f, 0.8f, 0.3f, 1f));
            }
            if (obstacleMaterial == null)
            {
                obstacleMaterial = CreateDefaultMaterial("ObstacleMaterial", new Color(0.6f, 0.4f, 0.2f, 1f));
            }
            if (waterMaterial == null)
            {
                waterMaterial = CreateDefaultMaterial("WaterMaterial", new Color(0.2f, 0.6f, 0.8f, 0.7f));
            }
            if (bridgeMaterial == null)
            {
                bridgeMaterial = CreateDefaultMaterial("BridgeMaterial", new Color(0.4f, 0.3f, 0.2f, 1f));
            }
            if (tunnelMaterial == null)
            {
                tunnelMaterial = CreateDefaultMaterial("TunnelMaterial", new Color(0.3f, 0.3f, 0.3f, 1f));
            }
        }

        private Material CreateDefaultMaterial(string name, Color color)
        {
            Material material = new Material(Shader.Find("Standard"));
            material.name = name;
            material.color = color;
            
            // Save material as asset
            string directory = "Assets/Resources/Materials";
            if (!AssetDatabase.IsValidFolder(directory))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Materials");
            }
            
            string assetPath = $"{directory}/{name}.mat";
            AssetDatabase.CreateAsset(material, assetPath);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Created default material: {assetPath}");
            return material;
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Unified Terrain Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Mode selection
            EditorGUILayout.LabelField("Generator Mode", EditorStyles.boldLabel);
            currentMode = (GeneratorMode)EditorGUILayout.EnumPopup("Mode:", currentMode);
            
            EditorGUILayout.Space();

            // Terrain type selection
            EditorGUILayout.LabelField("Terrain Type", EditorStyles.boldLabel);
            selectedTerrainType = (TerrainType)EditorGUILayout.EnumPopup("Type:", selectedTerrainType);
            
            if (currentMode == GeneratorMode.Advanced)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Pathfinding Test Type", EditorStyles.boldLabel);
                selectedTestType = (PathfindingTestType)EditorGUILayout.EnumPopup("Test Type:", selectedTestType);
            }
            
            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            terrainSize = EditorGUILayout.Vector3Field("Terrain Size:", terrainSize);
            gridSize = EditorGUILayout.IntField("Grid Size:", gridSize);
            wallHeight = EditorGUILayout.FloatField("Wall Height:", wallHeight);
            wallThickness = EditorGUILayout.FloatField("Wall Thickness:", wallThickness);
            
            EditorGUILayout.Space();

            // Simple mode settings
            if (currentMode == GeneratorMode.Simple)
            {
                EditorGUILayout.LabelField("Simple Mode Settings", EditorStyles.boldLabel);
                
                // Obstacle settings
                generateObstacles = EditorGUILayout.Toggle("Generate Obstacles:", generateObstacles);
                if (generateObstacles)
                {
                    obstacleCount = EditorGUILayout.IntField("Obstacle Count:", obstacleCount);
                    obstacleSize = EditorGUILayout.FloatField("Obstacle Size:", obstacleSize);
                }
                
                EditorGUILayout.Space();
                
                // Ramp settings
                generateRamps = EditorGUILayout.Toggle("Generate Ramps:", generateRamps);
                if (generateRamps)
                {
                    rampCount = EditorGUILayout.IntField("Ramp Count:", rampCount);
                    rampHeight = EditorGUILayout.FloatField("Ramp Height:", rampHeight);
                }
                
                EditorGUILayout.Space();
                
                // Water settings
                generateWater = EditorGUILayout.Toggle("Generate Water:", generateWater);
                if (generateWater)
                {
                    waterLevel = EditorGUILayout.FloatField("Water Level:", waterLevel);
                }
            }
            
            // Advanced mode settings
            if (currentMode == GeneratorMode.Advanced)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced Mode Settings", EditorStyles.boldLabel);
                
                // Advanced terrain features
                generateElevation = EditorGUILayout.Toggle("Generate Elevation:", generateElevation);
                if (generateElevation)
                {
                    maxElevation = EditorGUILayout.FloatField("Max Elevation:", maxElevation);
                }
                
                generateTunnels = EditorGUILayout.Toggle("Generate Tunnels:", generateTunnels);
                if (generateTunnels)
                {
                    tunnelCount = EditorGUILayout.IntField("Tunnel Count:", tunnelCount);
                }
                
                generateBridges = EditorGUILayout.Toggle("Generate Bridges:", generateBridges);
                if (generateBridges)
                {
                    bridgeCount = EditorGUILayout.IntField("Bridge Count:", bridgeCount);
                }
                
                EditorGUILayout.Space();
                
                // Performance settings
                EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
                optimizeForPerformance = EditorGUILayout.Toggle("Optimize for Performance:", optimizeForPerformance);
                generateLOD = EditorGUILayout.Toggle("Generate LOD:", generateLOD);
                useStaticBatching = EditorGUILayout.Toggle("Use Static Batching:", useStaticBatching);
                
                EditorGUILayout.Space();
                
                // Visualization settings
                EditorGUILayout.LabelField("Visualization Settings", EditorStyles.boldLabel);
                showPathfindingNodes = EditorGUILayout.Toggle("Show Pathfinding Nodes:", showPathfindingNodes);
                showGrid = EditorGUILayout.Toggle("Show Grid:", showGrid);
                if (showGrid)
                {
                    gridColor = EditorGUILayout.ColorField("Grid Color:", gridColor);
                }
                if (showPathfindingNodes)
                {
                    nodeColor = EditorGUILayout.ColorField("Node Color:", nodeColor);
                }
            }
            
            EditorGUILayout.Space();

            // Material settings
            EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);
            terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material:", terrainMaterial, typeof(Material), false);
            obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacle Material:", obstacleMaterial, typeof(Material), false);
            waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material:", waterMaterial, typeof(Material), false);
            
            if (currentMode == GeneratorMode.Advanced)
            {
                bridgeMaterial = (Material)EditorGUILayout.ObjectField("Bridge Material:", bridgeMaterial, typeof(Material), false);
                tunnelMaterial = (Material)EditorGUILayout.ObjectField("Tunnel Material:", tunnelMaterial, typeof(Material), false);
            }
            
            EditorGUILayout.Space();

            // Generate button
            if (GUILayout.Button($"Generate {currentMode} Terrain", GUILayout.Height(30)))
            {
                GenerateTerrain();
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void GenerateTerrain()
        {
            // Create parent object
            GameObject terrainParent = new GameObject($"Generated_{currentMode}_{selectedTerrainType}_Terrain");
            
            try
            {
                if (currentMode == GeneratorMode.Simple)
                {
                    GenerateSimpleTerrain(terrainParent);
                }
                else
                {
                    GenerateAdvancedTerrain(terrainParent);
                }
                
                // Select the generated terrain
                Selection.activeGameObject = terrainParent;
                EditorGUIUtility.PingObject(terrainParent);
                
                Debug.Log($"Successfully generated {currentMode} terrain: {terrainParent.name}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to generate terrain: {e.Message}");
                if (terrainParent != null)
                {
                    DestroyImmediate(terrainParent);
                }
            }
        }

        private void GenerateSimpleTerrain(GameObject parent)
        {
            // Generate base terrain
            switch (selectedTerrainType)
            {
                case TerrainType.SimplePlane:
                    GenerateSimplePlane(parent);
                    break;
                case TerrainType.Maze:
                    GenerateMaze(parent);
                    break;
                case TerrainType.City:
                    GenerateCity(parent);
                    break;
                case TerrainType.Forest:
                    GenerateForest(parent);
                    break;
                case TerrainType.Mountain:
                    GenerateMountain(parent);
                    break;
                case TerrainType.Battlefield:
                    GenerateBattlefield(parent);
                    break;
                case TerrainType.Dungeon:
                    GenerateDungeon(parent);
                    break;
                case TerrainType.Custom:
                    GenerateCustom(parent);
                    break;
            }
            
            // Generate additional features
            if (generateObstacles)
            {
                GenerateRandomObstacles(parent);
            }
            
            if (generateRamps)
            {
                GenerateRandomRamps(parent);
            }
            
            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private void GenerateAdvancedTerrain(GameObject parent)
        {
            // Generate base terrain
            switch (selectedTerrainType)
            {
                case TerrainType.SimplePlane:
                    GenerateSimplePlane(parent);
                    break;
                case TerrainType.Maze:
                    GenerateMaze(parent);
                    break;
                case TerrainType.City:
                    GenerateCity(parent);
                    break;
                case TerrainType.Forest:
                    GenerateForest(parent);
                    break;
                case TerrainType.Mountain:
                    GenerateMountain(parent);
                    break;
                case TerrainType.Battlefield:
                    GenerateBattlefield(parent);
                    break;
                case TerrainType.Dungeon:
                    GenerateDungeon(parent);
                    break;
                case TerrainType.Custom:
                    GenerateCustom(parent);
                    break;
            }
            
            // Configure for pathfinding test
            ConfigureTerrainForTestType(parent);
            
            // Generate advanced features
            if (generateElevation)
            {
                GenerateElevation(parent);
            }
            
            if (generateTunnels)
            {
                GenerateTunnels(parent);
            }
            
            if (generateBridges)
            {
                GenerateBridges(parent);
            }
            
            if (generateWater)
            {
                GenerateWater(parent);
            }
            
            // Apply performance optimizations
            if (optimizeForPerformance)
            {
                ApplyPerformanceOptimizations(parent);
            }
        }

        // Terrain generation methods (implemented from both generators)
        private void GenerateSimplePlane(GameObject parent)
        {
            GameObject plane = CreatePlane("BasePlane", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);
        }

        private void GenerateMaze(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateMazeWalls(parent);
        }

        private void GenerateCity(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateCityBuildings(parent);
        }

        private void GenerateForest(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateForestTrees(parent);
        }

        private void GenerateMountain(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateMountainTerrain(parent);
        }

        private void GenerateBattlefield(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateBattlefieldFeatures(parent);
        }

        private void GenerateDungeon(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateDungeonFeatures(parent);
        }

        private void GenerateCustom(GameObject parent)
        {
            GenerateSimplePlane(parent);
            GenerateCustomFeatures(parent);
        }

        // Helper methods (simplified implementations)
        private GameObject CreatePlane(string name, float width, float length, Vector3 position, Material material)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.position = position;
            plane.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);
            
            Renderer renderer = plane.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }
            
            return plane;
        }

        private void GenerateRandomObstacles(GameObject parent)
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    obstacleSize / 2f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );
                
                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i}";
                obstacle.transform.position = randomPos;
                obstacle.transform.localScale = Vector3.one * obstacleSize;
                obstacle.transform.SetParent(parent.transform);
                
                Renderer renderer = obstacle.GetComponent<Renderer>();
                if (renderer != null && obstacleMaterial != null)
                {
                    renderer.material = obstacleMaterial;
                }
            }
        }

        private void GenerateRandomRamps(GameObject parent)
        {
            for (int i = 0; i < rampCount; i++)
            {
                Vector3 randomPos = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    0f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );
                
                CreateRamp($"Ramp_{i}", randomPos, rampHeight).transform.SetParent(parent.transform);
            }
        }

        private GameObject CreateRamp(string name, Vector3 position, float height)
        {
            GameObject ramp = new GameObject(name);
            ramp.transform.position = position;
            
            // Create ramp mesh
            MeshFilter meshFilter = ramp.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = ramp.AddComponent<MeshRenderer>();
            
            Mesh mesh = new Mesh();
            mesh.vertices = new Vector3[]
            {
                new Vector3(-2f, 0f, -2f),
                new Vector3(2f, 0f, -2f),
                new Vector3(-2f, height, 2f),
                new Vector3(2f, height, 2f)
            };
            
            mesh.triangles = new int[]
            {
                0, 2, 1,
                1, 2, 3
            };
            
            mesh.RecalculateNormals();
            meshFilter.mesh = mesh;
            
            if (terrainMaterial != null)
            {
                meshRenderer.material = terrainMaterial;
            }
            
            return ramp;
        }

        // Placeholder methods for advanced features
        private void ConfigureTerrainForTestType(GameObject parent)
        {
            // Configure terrain based on selected test type
            Debug.Log($"Configuring terrain for {selectedTestType}");
        }

        private void GenerateMazeWalls(GameObject parent)
        {
            // Create walls container
            GameObject wallsContainer = new GameObject("MazeWalls");
            wallsContainer.transform.SetParent(parent.transform);
            
            // Calculate wall dimensions - make walls more rectangular
            float wallWidth = terrainSize.x / gridSize;
            float wallLength = terrainSize.z / gridSize;
            
            // Create vertical walls (along Z-axis) - thin and tall
            for (int x = 0; x < gridSize; x++)
            {
                for (int z = 0; z < gridSize - 1; z++)
                {
                    // Randomly decide if we should place a wall
                    if (Random.Range(0f, 1f) < 0.7f) // 70% chance to place a wall
                    {
                        Vector3 wallPosition = new Vector3(
                            (x - gridSize / 2f) * wallWidth + wallWidth / 2f,
                            wallHeight / 2f,
                            (z - gridSize / 2f) * wallLength + wallLength
                        );
                        
                        // Vertical wall: wide in X, thin in Z, tall in Y
                        CreateWall($"Wall_V_{x}_{z}", wallPosition, wallWidth, wallHeight, wallThickness, wallsContainer);
                    }
                }
            }
            
            // Create horizontal walls (along X-axis) - thin and tall
            for (int x = 0; x < gridSize - 1; x++)
            {
                for (int z = 0; z < gridSize; z++)
                {
                    // Randomly decide if we should place a wall
                    if (Random.Range(0f, 1f) < 0.7f) // 70% chance to place a wall
                    {
                        Vector3 wallPosition = new Vector3(
                            (x - gridSize / 2f) * wallWidth + wallWidth,
                            wallHeight / 2f,
                            (z - gridSize / 2f) * wallLength + wallLength / 2f
                        );
                        
                        // Horizontal wall: thin in X, wide in Z, tall in Y
                        CreateWall($"Wall_H_{x}_{z}", wallPosition, wallThickness, wallHeight, wallLength, wallsContainer);
                    }
                }
            }
            
            Debug.Log($"Generated maze walls with {wallsContainer.transform.childCount} walls");
            Debug.Log($"Wall dimensions - Width: {wallWidth}, Length: {wallLength}, Height: {wallHeight}, Thickness: {wallThickness}");
        }
        
        private void CreateWall(string name, Vector3 position, float width, float height, float depth, GameObject parent)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            
            // Set scale to create rectangular walls
            // Unity's default cube is 1x1x1, so we scale it to our desired dimensions
            wall.transform.localScale = new Vector3(width, height, depth);
            wall.transform.SetParent(parent.transform);
            
            // Apply material
            Renderer renderer = wall.GetComponent<Renderer>();
            if (renderer != null && obstacleMaterial != null)
            {
                renderer.material = obstacleMaterial;
            }
            
            // Add collider if not present
            if (wall.GetComponent<Collider>() == null)
            {
                wall.AddComponent<BoxCollider>();
            }
            
            // Debug log to verify wall dimensions
            Debug.Log($"Created wall '{name}' at {position} with scale {wall.transform.localScale}");
        }

        private void GenerateCityBuildings(GameObject parent)
        {
            // Generate city buildings
            Debug.Log("Generating city buildings");
        }

        private void GenerateForestTrees(GameObject parent)
        {
            // Generate forest trees
            Debug.Log("Generating forest trees");
        }

        private void GenerateMountainTerrain(GameObject parent)
        {
            // Generate mountain terrain
            Debug.Log("Generating mountain terrain");
        }

        private void GenerateBattlefieldFeatures(GameObject parent)
        {
            // Generate battlefield features
            Debug.Log("Generating battlefield features");
        }

        private void GenerateDungeonFeatures(GameObject parent)
        {
            // Generate dungeon features
            Debug.Log("Generating dungeon features");
        }

        private void GenerateCustomFeatures(GameObject parent)
        {
            // Generate custom features
            Debug.Log("Generating custom features");
        }

        private void GenerateElevation(GameObject parent)
        {
            // Generate elevation
            Debug.Log("Generating elevation");
        }

        private void GenerateTunnels(GameObject parent)
        {
            // Generate tunnels
            Debug.Log("Generating tunnels");
        }

        private void GenerateBridges(GameObject parent)
        {
            // Generate bridges
            Debug.Log("Generating bridges");
        }

        private void GenerateWater(GameObject parent)
        {
            if (waterMaterial == null) return;
            
            GameObject water = CreatePlane("Water", terrainSize.x, terrainSize.z, 
                new Vector3(0f, waterLevel, 0f), waterMaterial);
            water.transform.SetParent(parent.transform);
            
            // Make water transparent
            Renderer renderer = water.GetComponent<Renderer>();
            if (renderer != null && renderer.material != null)
            {
                renderer.material.SetFloat("_Mode", 3); // Transparent mode
                renderer.material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                renderer.material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                renderer.material.SetInt("_ZWrite", 0);
                renderer.material.DisableKeyword("_ALPHATEST_ON");
                renderer.material.EnableKeyword("_ALPHABLEND_ON");
                renderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                renderer.material.renderQueue = 3000;
            }
        }

        private void ApplyPerformanceOptimizations(GameObject parent)
        {
            // Apply performance optimizations
            Debug.Log("Applying performance optimizations");
        }
    }
} 