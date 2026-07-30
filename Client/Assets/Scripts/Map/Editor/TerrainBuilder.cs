using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 맵 지형 생성. Map Tool 파이프라인의 2단계이며, 창 상태에 의존하지 않는 정적 API 다.
///
/// 여기 있는 것은 전부 '실제로 NavMesh 를 구울 수 있는' 지오메트리다.
/// (기존 Tools > Terrain Generator 의 City/Forest/Mountain 등은 로그만 남기는 빈 구현이라 옮기지 않았다.
///  고급 편집이 필요하면 그 창을 그대로 쓰고, 여기서는 NavMesh 로 이어지는 최소 지형만 만든다.)
///
/// 생성물은 모두 하나의 루트(kRootName) 아래에 모은다. 다시 생성하면 루트째 교체되므로
/// 씬에 직접 배치한 마커(게이트/스폰)나 손으로 만든 오브젝트는 건드리지 않는다.
/// </summary>
public static class TerrainBuilder
{
    public const string RootName = "Terrain";

    public enum Preset
    {
        Flat,       // 평지만
        Obstacles,  // 평지 + 장애물/경사로
        Maze,       // 평지 + 미로 벽
    }

    [System.Serializable]
    public class Settings
    {
        public Preset preset = Preset.Obstacles;
        public Vector3 size = new Vector3(50f, 10f, 50f);

        public bool borderWalls = true;   // 맵 경계 벽(없으면 NavMesh 가 평면 끝까지 열려 캐릭터가 맵 밖으로 나간다)
        public float borderHeight = 3f;

        public int obstacleCount = 20;
        public float obstacleSize = 2f;

        public int rampCount = 5;
        public float rampHeight = 2f;

        public int mazeGridSize = 10;
        public float wallHeight = 3f;
        public float wallThickness = 0.2f;

        public int randomSeed = 0;        // 0 이면 매번 다르게

        // 색만 정하면 툴이 머티리얼을 만들어 붙인다.
        // (지정하지 않으면 유니티 기본 머티리얼이 붙어 지형 전체가 흰색으로 나온다.)
        public Color groundColor = new Color(0.42f, 0.52f, 0.34f);    // 풀색
        public Color obstacleColor = new Color(0.55f, 0.50f, 0.45f);  // 바위색

        // 직접 만든 머티리얼을 쓰고 싶을 때. 지정하면 위 색보다 우선한다.
        public Material groundMaterial;
        public Material obstacleMaterial;
    }

    // 툴이 만든 색 머티리얼을 두는 곳. 여기 에셋은 툴이 소유하므로 생성할 때마다 색을 덮어쓴다.
    private const string MaterialFolder = "Assets/Materials/MapTool";

    // Build 한 번 동안 쓰는 머티리얼(설정에서 확정된 값).
    private static Material groundMaterial;
    private static Material obstacleMaterial;

    /// <summary>씬에 지형을 만든다. 기존 지형 루트가 있으면 지우고 다시 만든다.</summary>
    public static GameObject Build(Settings settings)
    {
        if (settings == null)
            settings = new Settings();

        if (settings.randomSeed != 0)
            Random.InitState(settings.randomSeed);

        // 머티리얼을 미리 확정해 둔다(지정이 없으면 색으로 만들어 쓴다).
        groundMaterial = settings.groundMaterial != null
            ? settings.groundMaterial
            : EnsureMaterial("MapTool_Ground", settings.groundColor);
        obstacleMaterial = settings.obstacleMaterial != null
            ? settings.obstacleMaterial
            : EnsureMaterial("MapTool_Obstacle", settings.obstacleColor);

        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
            Object.DestroyImmediate(existing);

        var root = new GameObject(RootName);

        CreateGround(root, settings);

        switch (settings.preset)
        {
            case Preset.Obstacles:
                CreateObstacles(root, settings);
                CreateRamps(root, settings);
                break;
            case Preset.Maze:
                CreateMazeWalls(root, settings);
                break;
            case Preset.Flat:
            default:
                break;
        }

        if (settings.borderWalls)
            CreateBorderWalls(root, settings);

        return root;
    }

    private static void CreateGround(GameObject root, Settings s)
    {
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.SetParent(root.transform);
        ground.transform.position = Vector3.zero;
        // Unity 기본 Plane 은 한 변이 10 이라 크기/10 으로 맞춘다.
        ground.transform.localScale = new Vector3(s.size.x / 10f, 1f, s.size.z / 10f);

        ApplyMaterial(ground, groundMaterial);
    }

    private static void CreateObstacles(GameObject root, Settings s)
    {
        var container = new GameObject("Obstacles");
        container.transform.SetParent(root.transform);

        for (int i = 0; i < s.obstacleCount; i++)
        {
            var position = new Vector3(
                Random.Range(-s.size.x / 2f, s.size.x / 2f),
                s.obstacleSize / 2f,
                Random.Range(-s.size.z / 2f, s.size.z / 2f));

            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.name = $"Obstacle_{i}";
            obstacle.transform.SetParent(container.transform);
            obstacle.transform.position = position;
            obstacle.transform.localScale = Vector3.one * s.obstacleSize;

            ApplyMaterial(obstacle, obstacleMaterial);
        }
    }

    private static void CreateRamps(GameObject root, Settings s)
    {
        if (s.rampCount <= 0)
            return;

        var container = new GameObject("Ramps");
        container.transform.SetParent(root.transform);

        for (int i = 0; i < s.rampCount; i++)
        {
            var position = new Vector3(
                Random.Range(-s.size.x / 2f, s.size.x / 2f),
                0f,
                Random.Range(-s.size.z / 2f, s.size.z / 2f));

            var ramp = new GameObject($"Ramp_{i}");
            ramp.transform.SetParent(container.transform);
            ramp.transform.position = position;

            var mesh = new Mesh
            {
                vertices = new[]
                {
                    new Vector3(-2f, 0f, -2f),
                    new Vector3(2f, 0f, -2f),
                    new Vector3(-2f, s.rampHeight, 2f),
                    new Vector3(2f, s.rampHeight, 2f)
                },
                triangles = new[] { 0, 2, 1, 1, 2, 3 }
            };
            mesh.RecalculateNormals();

            ramp.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = ramp.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = groundMaterial;

            // OBJ 추출은 MeshFilter 만 보지만, 클라 이동/충돌을 위해 콜라이더도 붙인다.
            ramp.AddComponent<MeshCollider>().sharedMesh = mesh;
        }
    }

    private static void CreateMazeWalls(GameObject root, Settings s)
    {
        var container = new GameObject("MazeWalls");
        container.transform.SetParent(root.transform);

        int grid = Mathf.Max(2, s.mazeGridSize);
        float cellWidth = s.size.x / grid;
        float cellLength = s.size.z / grid;

        // 세로 벽(Z 방향으로 늘어선 칸 사이)
        for (int x = 0; x < grid; x++)
        {
            for (int z = 0; z < grid - 1; z++)
            {
                if (Random.value >= 0.7f)
                    continue;

                var position = new Vector3(
                    (x - grid / 2f) * cellWidth + cellWidth / 2f,
                    s.wallHeight / 2f,
                    (z - grid / 2f) * cellLength + cellLength);

                CreateWall(container, $"Wall_V_{x}_{z}", position,
                    new Vector3(cellWidth, s.wallHeight, s.wallThickness), s);
            }
        }

        // 가로 벽
        for (int x = 0; x < grid - 1; x++)
        {
            for (int z = 0; z < grid; z++)
            {
                if (Random.value >= 0.7f)
                    continue;

                var position = new Vector3(
                    (x - grid / 2f) * cellWidth + cellWidth,
                    s.wallHeight / 2f,
                    (z - grid / 2f) * cellLength + cellLength / 2f);

                CreateWall(container, $"Wall_H_{x}_{z}", position,
                    new Vector3(s.wallThickness, s.wallHeight, cellLength), s);
            }
        }
    }

    // 맵 경계. NavMesh 는 지오메트리 끝까지 걸어갈 수 있게 구워지므로,
    // 경계 벽이 없으면 서버가 캐릭터를 맵 밖 평면 끝까지 이동시킬 수 있다.
    private static void CreateBorderWalls(GameObject root, Settings s)
    {
        var container = new GameObject("Borders");
        container.transform.SetParent(root.transform);

        float halfX = s.size.x / 2f;
        float halfZ = s.size.z / 2f;
        float thickness = 1f;
        float height = Mathf.Max(1f, s.borderHeight);

        CreateWall(container, "Border_North", new Vector3(0f, height / 2f, halfZ),
            new Vector3(s.size.x + thickness, height, thickness), s);
        CreateWall(container, "Border_South", new Vector3(0f, height / 2f, -halfZ),
            new Vector3(s.size.x + thickness, height, thickness), s);
        CreateWall(container, "Border_East", new Vector3(halfX, height / 2f, 0f),
            new Vector3(thickness, height, s.size.z + thickness), s);
        CreateWall(container, "Border_West", new Vector3(-halfX, height / 2f, 0f),
            new Vector3(thickness, height, s.size.z + thickness), s);
    }

    private static void CreateWall(GameObject parent, string name, Vector3 position, Vector3 scale, Settings s)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent.transform);
        wall.transform.position = position;
        wall.transform.localScale = scale;

        ApplyMaterial(wall, obstacleMaterial);
    }

    // 색 머티리얼을 프로젝트 에셋으로 만들어 재사용한다.
    // 씬 오브젝트마다 new Material 을 만들면 씬에 익명 머티리얼이 쌓이고 저장 시 누락되기 쉽다.
    private static Material EnsureMaterial(string assetName, Color color)
    {
        string path = $"{MaterialFolder}/{assetName}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            Directory.CreateDirectory(MaterialFolder);
            // 이 프로젝트는 빌트인 렌더 파이프라인이라 Standard 를 쓴다.
            Shader shader = Shader.Find("Standard") ?? Shader.Find("Diffuse");
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }

        if (material.color != color)
        {
            material.color = color;
            EditorUtility.SetDirty(material);
        }
        return material;
    }

    private static void ApplyMaterial(GameObject go, Material material)
    {
        if (material == null)
            return;

        var renderer = go.GetComponent<Renderer>();
        if (renderer != null)
            renderer.sharedMaterial = material;
    }
}
