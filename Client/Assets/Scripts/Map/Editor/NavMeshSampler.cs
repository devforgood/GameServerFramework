using System.Collections.Generic;
using System.IO;
using RecastNavigation.Unity;
using UnityEngine;

/// <summary>
/// 구워 둔 NavMesh 위에서만 좌표를 뽑는다(스폰 지점 자동 배치용).
///
/// 왜 씬 지오메트리(레이캐스트/경계)가 아니라 navmesh 파일을 읽는가:
/// 서버는 스폰 좌표를 navmesh 로 검증하고(Map::ValidateMapDataOnNavMesh), 벗어난 지점은
/// 몬스터 스폰 자체가 실패한다. 게다가 navmesh 는 에이전트 반경(walkableRadius)만큼
/// 안쪽으로 좁혀져 있어서 "바닥 위"와 "이동 가능"이 다르다. 그래서 서버가 읽는 바로 그
/// 파일을 그대로 읽어 그 위에서만 뽑는다.
///
/// 좌표계: RecastNavigationWrapper.LoadNavMesh 가 정점을 유니티 좌표로 되돌려 주므로
/// 여기서 나오는 점도 씬/Map.json 과 같은 좌표계다(서버가 읽을 때 x 를 뒤집는다).
/// </summary>
public class NavMeshSampler
{
    private struct Triangle
    {
        public Vector3 a, b, c;
    }

    private readonly List<Triangle> triangles = new List<Triangle>();

    // 삼각형별 누적 면적. 면적에 비례해 뽑아야 넓은 곳과 좁은 곳의 밀도가 같아진다
    // (삼각형을 균등하게 고르면 잘게 쪼개진 장애물 주변에 점이 몰린다).
    private float[] cumulativeArea;
    private float totalArea;

    private Bounds bounds;

    // 점 포함 판정용 균일 격자(셀 → 그 셀과 겹치는 삼각형 인덱스).
    // 가장자리 여백 검사에서 점당 8번씩 조회하므로 선형 탐색으로는 감당이 안 된다.
    private float cellSize;
    private int cellsX, cellsZ;
    private List<int>[] cells;

    public int TriangleCount { get { return triangles.Count; } }

    /// <summary>이동 가능 면적(수평 투영, m²).</summary>
    public float WalkableArea { get { return totalArea; } }

    public Bounds Bounds { get { return bounds; } }

    public static NavMeshSampler Load(string navMeshPath, out string error)
    {
        error = null;

        if (string.IsNullOrEmpty(navMeshPath) || !File.Exists(navMeshPath))
        {
            error = $"NavMesh 파일이 없습니다: {navMeshPath}";
            return null;
        }

        NavMeshData data = RecastNavigationWrapper.LoadNavMesh(navMeshPath);
        if (data == null || data.polygons == null || data.polygons.Length == 0)
        {
            error = $"NavMesh 를 읽지 못했습니다: {navMeshPath}";
            return null;
        }

        var sampler = new NavMeshSampler();
        sampler.Build(data.polygons);

        if (sampler.triangles.Count == 0)
        {
            error = "NavMesh 에 이동 가능한 폴리곤이 없습니다. 다시 구워 보세요.";
            return null;
        }

        sampler.BuildLookup();
        return sampler;
    }

    private void Build(NavMeshPolygon[] polygons)
    {
        bool first = true;

        foreach (var poly in polygons)
        {
            // flags 0 은 서버 필터(dtQueryFilter::setIncludeFlags)에서 걸러지는 폴리곤이다.
            // 여기서 뽑아 봐야 서버가 쓰지 못한다.
            if (poly.vertices == null || poly.vertices.Length < 3 || poly.flags == 0)
                continue;

            // 볼록 폴리곤이므로 부채꼴 분할로 충분하다.
            for (int i = 1; i + 1 < poly.vertices.Length; i++)
            {
                var tri = new Triangle
                {
                    a = poly.vertices[0],
                    b = poly.vertices[i],
                    c = poly.vertices[i + 1],
                };

                float area = ProjectedArea(tri);
                if (area <= 0f)
                    continue;   // 수평 투영 면적이 0 인 면(수직 벽)에는 점을 놓을 수 없다

                triangles.Add(tri);
                totalArea += area;

                if (first)
                {
                    bounds = new Bounds(tri.a, Vector3.zero);
                    first = false;
                }
                bounds.Encapsulate(tri.a);
                bounds.Encapsulate(tri.b);
                bounds.Encapsulate(tri.c);
            }
        }

        cumulativeArea = new float[triangles.Count];
        float running = 0f;
        for (int i = 0; i < triangles.Count; i++)
        {
            running += ProjectedArea(triangles[i]);
            cumulativeArea[i] = running;
        }
    }

    private void BuildLookup()
    {
        float extent = Mathf.Max(bounds.size.x, bounds.size.z);
        cellSize = Mathf.Max(1f, extent / 128f);

        cellsX = Mathf.Max(1, Mathf.CeilToInt(bounds.size.x / cellSize) + 1);
        cellsZ = Mathf.Max(1, Mathf.CeilToInt(bounds.size.z / cellSize) + 1);
        cells = new List<int>[cellsX * cellsZ];

        for (int i = 0; i < triangles.Count; i++)
        {
            var tri = triangles[i];
            float minX = Mathf.Min(tri.a.x, Mathf.Min(tri.b.x, tri.c.x));
            float maxX = Mathf.Max(tri.a.x, Mathf.Max(tri.b.x, tri.c.x));
            float minZ = Mathf.Min(tri.a.z, Mathf.Min(tri.b.z, tri.c.z));
            float maxZ = Mathf.Max(tri.a.z, Mathf.Max(tri.b.z, tri.c.z));

            int x0 = ClampCellX(CellX(minX)), x1 = ClampCellX(CellX(maxX));
            int z0 = ClampCellZ(CellZ(minZ)), z1 = ClampCellZ(CellZ(maxZ));

            for (int z = z0; z <= z1; z++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int index = z * cellsX + x;
                    if (cells[index] == null)
                        cells[index] = new List<int>();
                    cells[index].Add(i);
                }
            }
        }
    }

    private int CellX(float x) { return Mathf.FloorToInt((x - bounds.min.x) / cellSize); }
    private int CellZ(float z) { return Mathf.FloorToInt((z - bounds.min.z) / cellSize); }
    private int ClampCellX(int x) { return Mathf.Clamp(x, 0, cellsX - 1); }
    private int ClampCellZ(int z) { return Mathf.Clamp(z, 0, cellsZ - 1); }

    /// <summary>이 좌표가 navmesh 안인지(높이는 보지 않는다).</summary>
    public bool Contains(float x, float z)
    {
        int cx = CellX(x);
        int cz = CellZ(z);
        if (cx < 0 || cz < 0 || cx >= cellsX || cz >= cellsZ)
            return false;

        List<int> bucket = cells[cz * cellsX + cx];
        if (bucket == null)
            return false;

        for (int i = 0; i < bucket.Count; i++)
        {
            if (PointInTriangle(x, z, triangles[bucket[i]]))
                return true;
        }
        return false;
    }

    // 여백 검사 방향(8방). 반경 전체를 검사하는 대신 표본을 쓴다 — 얇은 벽이 두 방향 사이로
    // 빠져나갈 수는 있지만, 가장자리·장애물 바로 옆을 걸러내는 데는 충분하다.
    private static readonly Vector2[] ClearanceDirections =
    {
        new Vector2(1f, 0f), new Vector2(-1f, 0f), new Vector2(0f, 1f), new Vector2(0f, -1f),
        new Vector2(0.7071f, 0.7071f), new Vector2(-0.7071f, 0.7071f),
        new Vector2(0.7071f, -0.7071f), new Vector2(-0.7071f, -0.7071f),
    };

    /// <summary>점 주변 margin 만큼이 모두 navmesh 안인지(가장자리·장애물에 붙는 것을 막는다).</summary>
    public bool HasClearance(Vector3 point, float margin)
    {
        if (margin <= 0f)
            return true;

        for (int i = 0; i < ClearanceDirections.Length; i++)
        {
            Vector2 dir = ClearanceDirections[i];
            if (!Contains(point.x + dir.x * margin, point.z + dir.y * margin))
                return false;
        }
        return true;
    }

    /// <summary>
    /// 최소 간격을 지키며 실제로 들어가는 대략적인 점 개수.
    ///
    /// 육각 충전(면적 / 0.866·간격²)이 아니라 무작위 배치의 포화 한계(RSA, 면적 / 1.44·간격²)를 쓴다.
    /// 무작위로 뿌리면 빈틈이 남아 이론적 최대치까지 채워지지 않는다 — Arcadia Plains(184,328m²)에서
    /// 간격 5m 로 8,000개를 요청했을 때 육각 기준은 8,514개였지만 실제로는 5,287개에서 멈췄다.
    /// </summary>
    public int EstimateCapacity(float minSpacing)
    {
        if (minSpacing <= 0f)
            return int.MaxValue;
        return Mathf.FloorToInt(totalArea / (minSpacing * minSpacing * 1.44f));
    }

    /// <summary>
    /// navmesh 위에 점을 흩뿌린다. 요청한 개수를 다 채우지 못할 수 있으므로(간격이 빡빡하면
    /// 자리가 없다) 실제로 놓인 목록을 돌려준다.
    /// </summary>
    /// <param name="seed">0 이면 매번 다른 배치.</param>
    /// <param name="onProgress">진행률(0~1)을 받고 false 를 돌려주면 중단한다. null 가능.</param>
    public List<Vector3> Scatter(int count, float minSpacing, float edgeMargin, int seed,
        System.Func<float, bool> onProgress)
    {
        var placed = new List<Vector3>(Mathf.Max(0, count));
        if (count <= 0)
            return placed;

        var rng = seed == 0 ? new System.Random() : new System.Random(seed);

        // 최소 간격 검사용 해시. 셀 크기를 간격과 같게 잡으면 3x3 이웃만 보면 된다.
        var occupied = new Dictionary<long, List<Vector3>>();

        // 자리가 없을 때 무한히 돌지 않도록 시도 횟수를 묶어 둔다.
        long maxAttempts = Mathf.Max(count * 40, 10000);
        maxAttempts = System.Math.Min(maxAttempts, 4000000L);

        for (long attempt = 0; attempt < maxAttempts && placed.Count < count; attempt++)
        {
            if (onProgress != null && (attempt & 1023) == 0)
            {
                if (!onProgress((float)placed.Count / count))
                    break;
            }

            Vector3 point = SamplePoint(rng);

            if (!HasClearance(point, edgeMargin))
                continue;
            if (minSpacing > 0f && IsTooClose(occupied, point, minSpacing))
                continue;

            placed.Add(point);
            if (minSpacing > 0f)
                Occupy(occupied, point, minSpacing);
        }

        return placed;
    }

    /// <summary>navmesh 면적에 비례해 균일하게 한 점을 뽑는다.</summary>
    public Vector3 SamplePoint(System.Random rng)
    {
        float target = (float)rng.NextDouble() * totalArea;

        int lo = 0, hi = cumulativeArea.Length - 1;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (cumulativeArea[mid] < target)
                lo = mid + 1;
            else
                hi = mid;
        }

        Triangle tri = triangles[lo];

        // 삼각형 내부 균일 분포(무게중심 좌표를 접어서 만든다).
        float u = (float)rng.NextDouble();
        float v = (float)rng.NextDouble();
        if (u + v > 1f)
        {
            u = 1f - u;
            v = 1f - v;
        }
        return tri.a + (tri.b - tri.a) * u + (tri.c - tri.a) * v;
    }

    private static long CellKey(Vector3 p, float spacing)
    {
        long cx = (long)Mathf.Floor(p.x / spacing);
        long cz = (long)Mathf.Floor(p.z / spacing);
        return (cx << 32) ^ (cz & 0xFFFFFFFFL);
    }

    private static bool IsTooClose(Dictionary<long, List<Vector3>> occupied, Vector3 point, float spacing)
    {
        long cx = (long)Mathf.Floor(point.x / spacing);
        long cz = (long)Mathf.Floor(point.z / spacing);
        float sqr = spacing * spacing;

        for (long dz = -1; dz <= 1; dz++)
        {
            for (long dx = -1; dx <= 1; dx++)
            {
                long key = ((cx + dx) << 32) ^ ((cz + dz) & 0xFFFFFFFFL);
                List<Vector3> bucket;
                if (!occupied.TryGetValue(key, out bucket))
                    continue;

                for (int i = 0; i < bucket.Count; i++)
                {
                    float ddx = bucket[i].x - point.x;
                    float ddz = bucket[i].z - point.z;
                    if (ddx * ddx + ddz * ddz < sqr)
                        return true;
                }
            }
        }
        return false;
    }

    private static void Occupy(Dictionary<long, List<Vector3>> occupied, Vector3 point, float spacing)
    {
        long key = CellKey(point, spacing);
        List<Vector3> bucket;
        if (!occupied.TryGetValue(key, out bucket))
        {
            bucket = new List<Vector3>();
            occupied[key] = bucket;
        }
        bucket.Add(point);
    }

    private static float ProjectedArea(Triangle tri)
    {
        float abx = tri.b.x - tri.a.x, abz = tri.b.z - tri.a.z;
        float acx = tri.c.x - tri.a.x, acz = tri.c.z - tri.a.z;
        return Mathf.Abs(abx * acz - acx * abz) * 0.5f;
    }

    // 감는 방향(winding)을 모르므로 세 변의 부호가 섞이지 않는지로 판정한다.
    private static bool PointInTriangle(float x, float z, Triangle tri)
    {
        float d1 = EdgeSign(x, z, tri.a, tri.b);
        float d2 = EdgeSign(x, z, tri.b, tri.c);
        float d3 = EdgeSign(x, z, tri.c, tri.a);

        bool hasNegative = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPositive = d1 > 0f || d2 > 0f || d3 > 0f;
        return !(hasNegative && hasPositive);
    }

    private static float EdgeSign(float x, float z, Vector3 p, Vector3 q)
    {
        return (x - q.x) * (p.z - q.z) - (p.x - q.x) * (z - q.z);
    }
}
