using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace GameServerFramework.Editor
{
    public class AdvancedTerrainGenerator : EditorWindow
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

        private enum PathfindingTestType
        {
            SimplePathfinding,
            ComplexMaze,
            MultiLevel,
            DynamicObstacles,
            PerformanceTest
        }

        private TerrainType selectedTerrainType = TerrainType.SimplePlane;
        private PathfindingTestType selectedTestType = PathfindingTestType.SimplePathfinding;
        private Vector3 terrainSize = new Vector3(100f, 20f, 100f);
        private int gridSize = 10;
        private float wallHeight = 3f;
        private float wallThickness = 0.5f;
        
        // Advanced settings
        private bool generateObstacles = true;
        private int obstacleCount = 30;
        private float obstacleSize = 2f;
        private bool generateRamps = true;
        private int rampCount = 8;
        private float rampHeight = 3f;
        private bool generateWater = false;
        private float waterLevel = 0f;
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
        
        // Performance settings
        private bool optimizeForPerformance = true;
        private bool generateLOD = true;
        private bool useStaticBatching = true;
        
        // Visualization settings
        private bool showPathfindingNodes = false;
        private bool showGrid = false;
        private Color gridColor = Color.white;
        private Color nodeColor = Color.yellow;

        [MenuItem("Tools/Terrain Generator (Legacy)")]
        public static void ShowWindow()
        {
            GetWindow<AdvancedTerrainGenerator>("Advanced Terrain Generator (Legacy)");
        }

        private void OnEnable()
        {
            // Load default materials
            terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/TerrainMaterial.mat");
            obstacleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ObstacleMaterial.mat");
            waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/WaterMaterial.mat");
            bridgeMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ObstacleMaterial.mat");
            tunnelMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/TerrainMaterial.mat");
            
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
            return material;
        }

        private void OnGUI()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced Terrain Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Terrain type selection
            EditorGUILayout.LabelField("Terrain Configuration", EditorStyles.boldLabel);
            selectedTerrainType = (TerrainType)EditorGUILayout.EnumPopup("Terrain Type:", selectedTerrainType);
            selectedTestType = (PathfindingTestType)EditorGUILayout.EnumPopup("Pathfinding Test Type:", selectedTestType);
            
            EditorGUILayout.Space();

            // Basic settings
            EditorGUILayout.LabelField("Basic Settings", EditorStyles.boldLabel);
            terrainSize = EditorGUILayout.Vector3Field("Terrain Size:", terrainSize);
            gridSize = EditorGUILayout.IntField("Grid Size:", gridSize);
            
            EditorGUILayout.Space();

            // Wall settings
            EditorGUILayout.LabelField("Wall Settings", EditorStyles.boldLabel);
            wallHeight = EditorGUILayout.FloatField("Wall Height:", wallHeight);
            wallThickness = EditorGUILayout.FloatField("Wall Thickness:", wallThickness);
            
            EditorGUILayout.Space();

            // Advanced features
            EditorGUILayout.LabelField("Advanced Features", EditorStyles.boldLabel);
            generateObstacles = EditorGUILayout.Toggle("Generate Obstacles:", generateObstacles);
            if (generateObstacles)
            {
                obstacleCount = EditorGUILayout.IntField("Obstacle Count:", obstacleCount);
                obstacleSize = EditorGUILayout.FloatField("Obstacle Size:", obstacleSize);
            }
            
            generateRamps = EditorGUILayout.Toggle("Generate Ramps:", generateRamps);
            if (generateRamps)
            {
                rampCount = EditorGUILayout.IntField("Ramp Count:", rampCount);
                rampHeight = EditorGUILayout.FloatField("Ramp Height:", rampHeight);
            }
            
            generateWater = EditorGUILayout.Toggle("Generate Water:", generateWater);
            if (generateWater)
            {
                waterLevel = EditorGUILayout.FloatField("Water Level:", waterLevel);
            }
            
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

            // Material settings
            EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);
            terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material:", terrainMaterial, typeof(Material), false);
            obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacle Material:", obstacleMaterial, typeof(Material), false);
            waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material:", waterMaterial, typeof(Material), false);
            bridgeMaterial = (Material)EditorGUILayout.ObjectField("Bridge Material:", bridgeMaterial, typeof(Material), false);
            tunnelMaterial = (Material)EditorGUILayout.ObjectField("Tunnel Material:", tunnelMaterial, typeof(Material), false);
            
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
            
            EditorGUILayout.Space();

            // Generate button
            if (GUILayout.Button("Generate Advanced Terrain", GUILayout.Height(30)))
            {
                GenerateAdvancedTerrain();
            }
        }

        private void GenerateAdvancedTerrain()
        {
            // Create parent GameObject for the terrain
            GameObject terrainParent = new GameObject($"AdvancedTerrain_{selectedTerrainType}_{selectedTestType}");
            
            // Configure terrain based on test type
            ConfigureTerrainForTestType(terrainParent);
            
            // Generate terrain based on type
            switch (selectedTerrainType)
            {
                case TerrainType.SimplePlane:
                    GenerateSimplePlane(terrainParent);
                    break;
                case TerrainType.Maze:
                    GenerateMaze(terrainParent);
                    break;
                case TerrainType.City:
                    GenerateCity(terrainParent);
                    break;
                case TerrainType.Forest:
                    GenerateForest(terrainParent);
                    break;
                case TerrainType.Mountain:
                    GenerateMountain(terrainParent);
                    break;
                case TerrainType.Battlefield:
                    GenerateBattlefield(terrainParent);
                    break;
                case TerrainType.Dungeon:
                    GenerateDungeon(terrainParent);
                    break;
                case TerrainType.Custom:
                    GenerateCustom(terrainParent);
                    break;
            }

            // Apply optimizations
            if (optimizeForPerformance)
            {
                ApplyPerformanceOptimizations(terrainParent);
            }

            // Select the generated terrain
            Selection.activeGameObject = terrainParent;
            EditorUtility.SetDirty(terrainParent);
            
            Debug.Log($"Generated {selectedTerrainType} terrain with {selectedTestType} test configuration successfully!");
        }

        private void ConfigureTerrainForTestType(GameObject parent)
        {
            switch (selectedTestType)
            {
                case PathfindingTestType.SimplePathfinding:
                    // Simple configuration for basic pathfinding tests
                    break;
                case PathfindingTestType.ComplexMaze:
                    // Increase complexity for maze testing
                    obstacleCount = Mathf.Max(obstacleCount, 50);
                    gridSize = Mathf.Max(gridSize, 5);
                    break;
                case PathfindingTestType.MultiLevel:
                    // Enable elevation and bridges for multi-level testing
                    generateElevation = true;
                    generateBridges = true;
                    maxElevation = Mathf.Max(maxElevation, 15f);
                    break;
                case PathfindingTestType.DynamicObstacles:
                    // Add more obstacles for dynamic testing
                    obstacleCount = Mathf.Max(obstacleCount, 100);
                    break;
                case PathfindingTestType.PerformanceTest:
                    // Large terrain for performance testing
                    terrainSize = new Vector3(200f, 30f, 200f);
                    obstacleCount = Mathf.Max(obstacleCount, 200);
                    break;
            }
        }

        private void GenerateSimplePlane(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("BasePlane", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

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

            if (generateElevation)
            {
                GenerateElevation(parent);
            }
        }

        private void GenerateMaze(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("MazeBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate maze walls
            GenerateMazeWalls(parent);

            if (generateWater)
            {
                GenerateWater(parent);
            }

            if (generateElevation)
            {
                GenerateElevation(parent);
            }
        }

        private void GenerateCity(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("CityBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate city buildings
            GenerateCityBuildings(parent);

            if (generateBridges)
            {
                GenerateBridges(parent);
            }

            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private void GenerateForest(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("ForestBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate forest trees
            GenerateForestTrees(parent);

            if (generateElevation)
            {
                GenerateElevation(parent);
            }

            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private void GenerateMountain(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("MountainBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate mountain terrain
            GenerateMountainTerrain(parent);

            if (generateTunnels)
            {
                GenerateTunnels(parent);
            }

            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private void GenerateBattlefield(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("BattlefieldBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate battlefield features
            GenerateBattlefieldFeatures(parent);

            if (generateElevation)
            {
                GenerateElevation(parent);
            }

            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private void GenerateDungeon(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("DungeonBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate dungeon features
            GenerateDungeonFeatures(parent);

            if (generateTunnels)
            {
                GenerateTunnels(parent);
            }
        }

        private void GenerateCustom(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("CustomBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate custom terrain features
            GenerateCustomFeatures(parent);

            if (generateWater)
            {
                GenerateWater(parent);
            }
        }

        private GameObject CreatePlane(string name, float width, float length, Vector3 position, Material material)
        {
            GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
            plane.name = name;
            plane.transform.position = position;
            plane.transform.localScale = new Vector3(width / 10f, 1f, length / 10f);

            if (material != null)
            {
                plane.GetComponent<Renderer>().material = material;
            }

            return plane;
        }

        private void GenerateRandomObstacles(GameObject parent)
        {
            for (int i = 0; i < obstacleCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    obstacleSize / 2f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = $"Obstacle_{i}";
                obstacle.transform.position = position;
                obstacle.transform.localScale = new Vector3(obstacleSize, obstacleSize, obstacleSize);

                if (obstacleMaterial != null)
                {
                    obstacle.GetComponent<Renderer>().material = obstacleMaterial;
                }

                obstacle.transform.SetParent(parent.transform);
            }
        }

        private void GenerateRandomRamps(GameObject parent)
        {
            for (int i = 0; i < rampCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    rampHeight / 2f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject ramp = CreateRamp($"Ramp_{i}", position, rampHeight);
                ramp.transform.SetParent(parent.transform);
            }
        }

        private GameObject CreateRamp(string name, Vector3 position, float height)
        {
            GameObject ramp = new GameObject(name);
            ramp.transform.position = position;

            // Create ramp mesh
            Mesh rampMesh = new Mesh();
            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-3f, 0f, -3f),
                new Vector3(3f, 0f, -3f),
                new Vector3(3f, height, 3f),
                new Vector3(-3f, height, 3f)
            };

            int[] triangles = new int[]
            {
                0, 1, 2,
                0, 2, 3
            };

            Vector2[] uvs = new Vector2[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            rampMesh.vertices = vertices;
            rampMesh.triangles = triangles;
            rampMesh.uv = uvs;
            rampMesh.RecalculateNormals();

            MeshFilter meshFilter = ramp.AddComponent<MeshFilter>();
            meshFilter.mesh = rampMesh;

            MeshRenderer meshRenderer = ramp.AddComponent<MeshRenderer>();
            if (terrainMaterial != null)
            {
                meshRenderer.material = terrainMaterial;
            }

            return ramp;
        }

        private void GenerateMazeWalls(GameObject parent)
        {
            int gridX = Mathf.FloorToInt(terrainSize.x / gridSize);
            int gridZ = Mathf.FloorToInt(terrainSize.z / gridSize);

            for (int x = 0; x < gridX; x++)
            {
                for (int z = 0; z < gridZ; z++)
                {
                    if (Random.Range(0f, 1f) < 0.4f) // 40% chance to place a wall
                    {
                        Vector3 position = new Vector3(
                            x * gridSize - terrainSize.x / 2f + gridSize / 2f,
                            wallHeight / 2f,
                            z * gridSize - terrainSize.z / 2f + gridSize / 2f
                        );

                        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        wall.name = $"MazeWall_{x}_{z}";
                        wall.transform.position = position;
                        wall.transform.localScale = new Vector3(gridSize - wallThickness, wallHeight, wallThickness);

                        if (obstacleMaterial != null)
                        {
                            wall.GetComponent<Renderer>().material = obstacleMaterial;
                        }

                        wall.transform.SetParent(parent.transform);
                    }
                }
            }
        }

        private void GenerateCityBuildings(GameObject parent)
        {
            int buildingCount = Mathf.FloorToInt((terrainSize.x * terrainSize.z) / 80f);

            for (int i = 0; i < buildingCount; i++)
            {
                float buildingHeight = Random.Range(8f, 25f);
                float buildingWidth = Random.Range(4f, 12f);

                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f + buildingWidth, terrainSize.x / 2f - buildingWidth),
                    buildingHeight / 2f,
                    Random.Range(-terrainSize.z / 2f + buildingWidth, terrainSize.z / 2f - buildingWidth)
                );

                GameObject building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                building.name = $"Building_{i}";
                building.transform.position = position;
                building.transform.localScale = new Vector3(buildingWidth, buildingHeight, buildingWidth);

                if (obstacleMaterial != null)
                {
                    building.GetComponent<Renderer>().material = obstacleMaterial;
                }

                building.transform.SetParent(parent.transform);
            }
        }

        private void GenerateForestTrees(GameObject parent)
        {
            int treeCount = Mathf.FloorToInt((terrainSize.x * terrainSize.z) / 40f);

            for (int i = 0; i < treeCount; i++)
            {
                float treeHeight = Random.Range(4f, 12f);
                float treeRadius = Random.Range(1.5f, 4f);

                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f + treeRadius, terrainSize.x / 2f - treeRadius),
                    treeHeight / 2f,
                    Random.Range(-terrainSize.z / 2f + treeRadius, terrainSize.z / 2f - treeRadius)
                );

                GameObject tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                tree.name = $"Tree_{i}";
                tree.transform.position = position;
                tree.transform.localScale = new Vector3(treeRadius, treeHeight, treeRadius);

                if (obstacleMaterial != null)
                {
                    tree.GetComponent<Renderer>().material = obstacleMaterial;
                }

                tree.transform.SetParent(parent.transform);
            }
        }

        private void GenerateMountainTerrain(GameObject parent)
        {
            int mountainCount = 8;

            for (int i = 0; i < mountainCount; i++)
            {
                float mountainHeight = Random.Range(12f, 30f);
                float mountainRadius = Random.Range(10f, 20f);

                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f + mountainRadius, terrainSize.x / 2f - mountainRadius),
                    mountainHeight / 2f,
                    Random.Range(-terrainSize.z / 2f + mountainRadius, terrainSize.z / 2f - mountainRadius)
                );

                GameObject mountain = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                mountain.name = $"Mountain_{i}";
                mountain.transform.position = position;
                mountain.transform.localScale = new Vector3(mountainRadius, mountainHeight, mountainRadius);

                if (terrainMaterial != null)
                {
                    mountain.GetComponent<Renderer>().material = terrainMaterial;
                }

                mountain.transform.SetParent(parent.transform);
            }
        }

        private void GenerateBattlefieldFeatures(GameObject parent)
        {
            // Generate trenches
            for (int i = 0; i < 10; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    1f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject trench = GameObject.CreatePrimitive(PrimitiveType.Cube);
                trench.name = $"Trench_{i}";
                trench.transform.position = position;
                trench.transform.localScale = new Vector3(8f, 2f, 1f);

                if (obstacleMaterial != null)
                {
                    trench.GetComponent<Renderer>().material = obstacleMaterial;
                }

                trench.transform.SetParent(parent.transform);
            }

            // Generate bunkers
            for (int i = 0; i < 5; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    3f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject bunker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bunker.name = $"Bunker_{i}";
                bunker.transform.position = position;
                bunker.transform.localScale = new Vector3(6f, 6f, 6f);

                if (obstacleMaterial != null)
                {
                    bunker.GetComponent<Renderer>().material = obstacleMaterial;
                }

                bunker.transform.SetParent(parent.transform);
            }
        }

        private void GenerateDungeonFeatures(GameObject parent)
        {
            // Generate dungeon walls
            for (int i = 0; i < 20; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    wallHeight / 2f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"DungeonWall_{i}";
                wall.transform.position = position;
                wall.transform.localScale = new Vector3(5f, wallHeight, 1f);

                if (obstacleMaterial != null)
                {
                    wall.GetComponent<Renderer>().material = obstacleMaterial;
                }

                wall.transform.SetParent(parent.transform);
            }

            // Generate pillars
            for (int i = 0; i < 15; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    8f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i}";
                pillar.transform.position = position;
                pillar.transform.localScale = new Vector3(2f, 16f, 2f);

                if (obstacleMaterial != null)
                {
                    pillar.GetComponent<Renderer>().material = obstacleMaterial;
                }

                pillar.transform.SetParent(parent.transform);
            }
        }

        private void GenerateCustomFeatures(GameObject parent)
        {
            // Generate a combination of different features
            GenerateRandomObstacles(parent);
            GenerateRandomRamps(parent);
            GenerateMazeWalls(parent);
            GenerateElevation(parent);
        }

        private void GenerateElevation(GameObject parent)
        {
            int elevationCount = 10;

            for (int i = 0; i < elevationCount; i++)
            {
                float elevationHeight = Random.Range(2f, maxElevation);
                float elevationRadius = Random.Range(5f, 15f);

                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f + elevationRadius, terrainSize.x / 2f - elevationRadius),
                    elevationHeight / 2f,
                    Random.Range(-terrainSize.z / 2f + elevationRadius, terrainSize.z / 2f - elevationRadius)
                );

                GameObject elevation = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                elevation.name = $"Elevation_{i}";
                elevation.transform.position = position;
                elevation.transform.localScale = new Vector3(elevationRadius, elevationHeight, elevationRadius);

                if (terrainMaterial != null)
                {
                    elevation.GetComponent<Renderer>().material = terrainMaterial;
                }

                elevation.transform.SetParent(parent.transform);
            }
        }

        private void GenerateBridges(GameObject parent)
        {
            for (int i = 0; i < bridgeCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    5f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject bridge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                bridge.name = $"Bridge_{i}";
                bridge.transform.position = position;
                bridge.transform.localScale = new Vector3(15f, 1f, 3f);

                if (bridgeMaterial != null)
                {
                    bridge.GetComponent<Renderer>().material = bridgeMaterial;
                }

                bridge.transform.SetParent(parent.transform);
            }
        }

        private void GenerateTunnels(GameObject parent)
        {
            for (int i = 0; i < tunnelCount; i++)
            {
                Vector3 position = new Vector3(
                    Random.Range(-terrainSize.x / 2f, terrainSize.x / 2f),
                    2f,
                    Random.Range(-terrainSize.z / 2f, terrainSize.z / 2f)
                );

                GameObject tunnel = GameObject.CreatePrimitive(PrimitiveType.Cube);
                tunnel.name = $"Tunnel_{i}";
                tunnel.transform.position = position;
                tunnel.transform.localScale = new Vector3(10f, 4f, 4f);

                if (tunnelMaterial != null)
                {
                    tunnel.GetComponent<Renderer>().material = tunnelMaterial;
                }

                tunnel.transform.SetParent(parent.transform);
            }
        }

        private void GenerateWater(GameObject parent)
        {
            GameObject water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "Water";
            water.transform.position = new Vector3(0f, waterLevel, 0f);
            water.transform.localScale = new Vector3(terrainSize.x / 10f, 1f, terrainSize.z / 10f);

            if (waterMaterial != null)
            {
                water.GetComponent<Renderer>().material = waterMaterial;
            }

            water.transform.SetParent(parent.transform);
        }

        private void ApplyPerformanceOptimizations(GameObject parent)
        {
            // Apply static batching
            if (useStaticBatching)
            {
                StaticBatchingUtility.Combine(parent);
            }

            // Generate LOD groups if enabled
            if (generateLOD)
            {
                GenerateLODGroups(parent);
            }

            // Optimize mesh renderers
            MeshRenderer[] renderers = parent.GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer renderer in renderers)
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private void GenerateLODGroups(GameObject parent)
        {
            // This is a simplified LOD generation
            // In a real implementation, you would create different LOD levels
            LODGroup[] existingLODGroups = parent.GetComponentsInChildren<LODGroup>();
            foreach (LODGroup lodGroup in existingLODGroups)
            {
                DestroyImmediate(lodGroup);
            }
        }
    }
} 