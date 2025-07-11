using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace GameServerFramework.Editor
{
    public class SampleTerrainGenerator : EditorWindow
    {
        private enum TerrainType
        {
            SimplePlane,
            Maze,
            City,
            Forest,
            Mountain,
            Custom
        }

        private TerrainType selectedTerrainType = TerrainType.SimplePlane;
        private Vector3 terrainSize = new Vector3(50f, 10f, 50f);
        private int gridSize = 10;
        private float wallHeight = 3f;
        private float wallThickness = 0.5f;
        private bool generateObstacles = true;
        private int obstacleCount = 20;
        private float obstacleSize = 2f;
        private bool generateRamps = true;
        private int rampCount = 5;
        private float rampHeight = 2f;
        private bool generateWater = false;
        private float waterLevel = 0f;
        private Material terrainMaterial;
        private Material obstacleMaterial;
        private Material waterMaterial;

        [MenuItem("Tools/Sample Terrain Generator")]
        public static void ShowWindow()
        {
            GetWindow<SampleTerrainGenerator>("Sample Terrain Generator");
        }

        private void OnEnable()
        {
            // Load default materials
            terrainMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/TerrainMaterial.mat");
            obstacleMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/ObstacleMaterial.mat");
            waterMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Materials/WaterMaterial.mat");
            
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
            EditorGUILayout.LabelField("Sample Terrain Generator", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // Terrain type selection
            EditorGUILayout.LabelField("Terrain Type", EditorStyles.boldLabel);
            selectedTerrainType = (TerrainType)EditorGUILayout.EnumPopup("Type:", selectedTerrainType);
            
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

            // Obstacle settings
            EditorGUILayout.LabelField("Obstacle Settings", EditorStyles.boldLabel);
            generateObstacles = EditorGUILayout.Toggle("Generate Obstacles:", generateObstacles);
            if (generateObstacles)
            {
                obstacleCount = EditorGUILayout.IntField("Obstacle Count:", obstacleCount);
                obstacleSize = EditorGUILayout.FloatField("Obstacle Size:", obstacleSize);
            }
            
            EditorGUILayout.Space();

            // Ramp settings
            EditorGUILayout.LabelField("Ramp Settings", EditorStyles.boldLabel);
            generateRamps = EditorGUILayout.Toggle("Generate Ramps:", generateRamps);
            if (generateRamps)
            {
                rampCount = EditorGUILayout.IntField("Ramp Count:", rampCount);
                rampHeight = EditorGUILayout.FloatField("Ramp Height:", rampHeight);
            }
            
            EditorGUILayout.Space();

            // Water settings
            EditorGUILayout.LabelField("Water Settings", EditorStyles.boldLabel);
            generateWater = EditorGUILayout.Toggle("Generate Water:", generateWater);
            if (generateWater)
            {
                waterLevel = EditorGUILayout.FloatField("Water Level:", waterLevel);
            }
            
            EditorGUILayout.Space();

            // Material settings
            EditorGUILayout.LabelField("Material Settings", EditorStyles.boldLabel);
            terrainMaterial = (Material)EditorGUILayout.ObjectField("Terrain Material:", terrainMaterial, typeof(Material), false);
            obstacleMaterial = (Material)EditorGUILayout.ObjectField("Obstacle Material:", obstacleMaterial, typeof(Material), false);
            waterMaterial = (Material)EditorGUILayout.ObjectField("Water Material:", waterMaterial, typeof(Material), false);
            
            EditorGUILayout.Space();

            // Generate button
            if (GUILayout.Button("Generate Sample Terrain", GUILayout.Height(30)))
            {
                GenerateTerrain();
            }
        }

        private void GenerateTerrain()
        {
            // Create parent GameObject for the terrain
            GameObject terrainParent = new GameObject($"SampleTerrain_{selectedTerrainType}");
            
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
                case TerrainType.Custom:
                    GenerateCustom(terrainParent);
                    break;
            }

            // Select the generated terrain
            Selection.activeGameObject = terrainParent;
            EditorUtility.SetDirty(terrainParent);
            
            Debug.Log($"Generated {selectedTerrainType} terrain successfully!");
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
        }

        private void GenerateCity(GameObject parent)
        {
            // Create base plane
            GameObject plane = CreatePlane("CityBase", terrainSize.x, terrainSize.z, Vector3.zero, terrainMaterial);
            plane.transform.SetParent(parent.transform);

            // Generate city buildings
            GenerateCityBuildings(parent);

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

            if (generateWater)
            {
                GenerateWater(parent);
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
                new Vector3(-2f, 0f, -2f),
                new Vector3(2f, 0f, -2f),
                new Vector3(2f, height, 2f),
                new Vector3(-2f, height, 2f)
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
                    if (Random.Range(0f, 1f) < 0.3f) // 30% chance to place a wall
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
            int buildingCount = Mathf.FloorToInt((terrainSize.x * terrainSize.z) / 100f);

            for (int i = 0; i < buildingCount; i++)
            {
                float buildingHeight = Random.Range(5f, 15f);
                float buildingWidth = Random.Range(3f, 8f);

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
            int treeCount = Mathf.FloorToInt((terrainSize.x * terrainSize.z) / 50f);

            for (int i = 0; i < treeCount; i++)
            {
                float treeHeight = Random.Range(3f, 8f);
                float treeRadius = Random.Range(1f, 3f);

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
            int mountainCount = 5;

            for (int i = 0; i < mountainCount; i++)
            {
                float mountainHeight = Random.Range(8f, 20f);
                float mountainRadius = Random.Range(8f, 15f);

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

        private void GenerateCustomFeatures(GameObject parent)
        {
            // Generate a combination of different features
            GenerateRandomObstacles(parent);
            GenerateRandomRamps(parent);
            GenerateMazeWalls(parent);
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
    }
} 