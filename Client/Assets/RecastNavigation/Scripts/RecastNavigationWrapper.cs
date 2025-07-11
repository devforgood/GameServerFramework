using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.IO;

namespace RecastNavigation.Unity
{
    public static class RecastNavigationWrapper
    {
        // Native plugin function imports for NavMesh generation only
        [DllImport("RecastNavigationUnity")]
        private static extern bool GenerateNavMeshFromObj(
            [MarshalAs(UnmanagedType.LPStr)] string objFilePath,
            [MarshalAs(UnmanagedType.LPStr)] string outputPath,
            float cellSize,
            float cellHeight,
            float walkableSlopeAngle,
            float walkableHeight,
            float walkableRadius,
            float walkableClimb,
            float minRegionArea,
            float mergeRegionArea,
            float maxSimplificationError,
            float maxEdgeLen,
            float detailSampleDistance,
            float detailSampleMaxError
        );

        // NavMesh file format constants
        private const int NAVMESHSET_MAGIC = ('M' << 24) | ('S' << 16) | ('E' << 8) | 'T'; // 'MSET'
        private const int NAVMESHSET_VERSION = 1;
        private const int DT_NAVMESH_MAGIC = ('D' << 24) | ('N' << 16) | ('A' << 8) | 'V'; // 'DNAV'
        private const int DT_NAVMESH_VERSION = 7;

        // NavMesh file format structures
        [StructLayout(LayoutKind.Sequential)]
        private struct NavMeshSetHeader
        {
            public int magic;
            public int version;
            public int numTiles;
            public dtNavMeshParams params_;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct NavMeshTileHeader
        {
            public uint tileRef;  // dtTileRef is uint in C++
            public int dataSize;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct dtNavMeshParams
        {
            public float orig0;
            public float orig1;
            public float orig2;
            public float tileWidth;
            public float tileHeight;
            public int maxTiles;
            public int maxPolys;
        }

        // Tile data structures
        [StructLayout(LayoutKind.Sequential)]
        private struct dtMeshHeader
        {
            public int magic;
            public int version;
            public int x;
            public int y;
            public int layer;
            public uint userId;
            public int polyCount;
            public int vertCount;
            public int maxLinkCount;
            public int detailMeshCount;
            public int detailVertCount;
            public int detailTriCount;
            public int bvNodeCount;
            public int offMeshConCount;
            public int offMeshBase;
            public float walkableHeight;
            public float walkableRadius;
            public float walkableClimb;
            public float bmin0;
            public float bmin1;
            public float bmin2;
            public float bmax0;
            public float bmax1;
            public float bmax2;
            public float bvQuantFactor;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct dtPoly
        {
            public uint firstLink;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ushort[] verts;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public ushort[] neis;
            public ushort flags;
            public byte vertCount;
            public byte areaAndtype;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct dtPolyDetail
        {
            public uint vertBase;
            public uint triBase;
            public byte vertCount;
            public byte triCount;
        }

        // Public interface methods
        public static bool GenerateNavMesh(
            string objFilePath,
            string outputPath,
            float cellSize,
            float cellHeight,
            float walkableSlopeAngle,
            float walkableHeight,
            float walkableRadius,
            float walkableClimb,
            float minRegionArea,
            float mergeRegionArea,
            float maxSimplificationError,
            float maxEdgeLen,
            float detailSampleDistance,
            float detailSampleMaxError)
        {
            try
            {
                Debug.Log($"Generating NavMesh from: {objFilePath}");
                Debug.Log($"Output path: {outputPath}");
                
                bool result = GenerateNavMeshFromObj(
                    objFilePath,
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
                
                if (result)
                {
                    Debug.Log("NavMesh generation completed successfully");
                }
                else
                {
                    Debug.LogError("NavMesh generation failed");
                }
                
                return result;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in GenerateNavMesh: {e.Message}");
                return false;
            }
        }
        
        public static NavMeshData LoadNavMesh(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"NavMesh file not found: {filePath}");
                    return null;
                }
                
                Debug.Log($"Loading NavMesh from: {filePath}");
                
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                using (var reader = new BinaryReader(stream))
                {
                    // Read header
                    NavMeshSetHeader header = ReadNavMeshSetHeader(reader);
                    
                    if (header.magic != NAVMESHSET_MAGIC)
                    {
                        Debug.LogError($"Invalid NavMesh file magic: {header.magic:X8}");
                        return null;
                    }
                    
                    if (header.version != NAVMESHSET_VERSION)
                    {
                        Debug.LogError($"Unsupported NavMesh version: {header.version}");
                        return null;
                    }
                    
                    Debug.Log($"NavMesh header: {header.numTiles} tiles");
                    Debug.Log($"NavMesh params: orig=({header.params_.orig0}, {header.params_.orig1}, {header.params_.orig2})");
                    Debug.Log($"NavMesh params: tileSize=({header.params_.tileWidth}, {header.params_.tileHeight})");
                    Debug.Log($"NavMesh params: maxTiles={header.params_.maxTiles}, maxPolys={header.params_.maxPolys}");
                    
                    // Create NavMesh data
                    NavMeshData navMeshData = new NavMeshData();
                    navMeshData.polygons = new NavMeshPolygon[0]; // Will be populated from tiles
                    
                    // Read tiles
                    var allPolygons = new System.Collections.Generic.List<NavMeshPolygon>();
                    
                    for (int i = 0; i < header.numTiles; ++i)
                    {
                        NavMeshTileHeader tileHeader = ReadNavMeshTileHeader(reader);
                        
                        Debug.Log($"Tile {i}: ref={tileHeader.tileRef}, size={tileHeader.dataSize}");
                        
                        if (tileHeader.tileRef == 0 || tileHeader.dataSize <= 0)
                        {
                            Debug.Log($"Skipping tile {i} (invalid ref or size)");
                            break;
                        }
                        
                        byte[] tileData = reader.ReadBytes(tileHeader.dataSize);
                        if (tileData.Length != tileHeader.dataSize)
                        {
                            Debug.LogError($"Failed to read tile data for tile {i}: expected {tileHeader.dataSize}, got {tileData.Length}");
                            return null;
                        }
                        
                        Debug.Log($"Tile {i} data read successfully: {tileData.Length} bytes");
                        
                        // Parse tile data
                        var tilePolygons = ParseTileData(tileData);
                        Debug.Log($"Tile {i} parsed: {tilePolygons.Length} polygons");
                        allPolygons.AddRange(tilePolygons);
                        
                        // Update bounds from first tile
                        if (i == 0 && tilePolygons.Length > 0)
                        {
                            navMeshData.bounds = CalculateBounds(tilePolygons);
                        }
                    }
                    
                    navMeshData.polygons = allPolygons.ToArray();
                    
                    Debug.Log($"NavMesh loaded successfully with {navMeshData.polygons.Length} polygons");
                    return navMeshData;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error in LoadNavMesh: {e.Message}");
                return null;
            }
            
            return null;
        }
        
        private static NavMeshSetHeader ReadNavMeshSetHeader(BinaryReader reader)
        {
            var header = new NavMeshSetHeader();
            header.magic = reader.ReadInt32();
            header.version = reader.ReadInt32();
            header.numTiles = reader.ReadInt32();
            
            // Read dtNavMeshParams
            header.params_.orig0 = reader.ReadSingle();
            header.params_.orig1 = reader.ReadSingle();
            header.params_.orig2 = reader.ReadSingle();
            header.params_.tileWidth = reader.ReadSingle();
            header.params_.tileHeight = reader.ReadSingle();
            header.params_.maxTiles = reader.ReadInt32();
            header.params_.maxPolys = reader.ReadInt32();
            
            return header;
        }
        
        private static NavMeshTileHeader ReadNavMeshTileHeader(BinaryReader reader)
        {
            var header = new NavMeshTileHeader();
            header.tileRef = reader.ReadUInt32();  // dtTileRef is uint
            header.dataSize = reader.ReadInt32();
            return header;
        }
        
        private static NavMeshPolygon[] ParseTileData(byte[] tileData)
        {
            using (var stream = new MemoryStream(tileData))
            using (var reader = new BinaryReader(stream))
            {
                // Read mesh header
                dtMeshHeader header = ReadMeshHeader(reader);
                
                Debug.Log($"Tile header: magic={header.magic:X8}, version={header.version}");
                Debug.Log($"Tile header: x={header.x}, y={header.y}, layer={header.layer}");
                Debug.Log($"Tile header: polyCount={header.polyCount}, vertCount={header.vertCount}");
                Debug.Log($"Tile header: detailMeshCount={header.detailMeshCount}, detailVertCount={header.detailVertCount}, detailTriCount={header.detailTriCount}");
                Debug.Log($"Tile header: bvNodeCount={header.bvNodeCount}, offMeshConCount={header.offMeshConCount}");
                
                if (header.magic != DT_NAVMESH_MAGIC)
                {
                    Debug.LogError($"Invalid tile magic: {header.magic:X8} (expected: {DT_NAVMESH_MAGIC:X8})");
                    return new NavMeshPolygon[0];
                }
                
                if (header.version != DT_NAVMESH_VERSION)
                {
                    Debug.LogError($"Unsupported tile version: {header.version} (expected: {DT_NAVMESH_VERSION})");
                    return new NavMeshPolygon[0];
                }
                
                // Read vertices
                float[] vertices = new float[header.vertCount * 3];
                for (int i = 0; i < header.vertCount * 3; i++)
                {
                    vertices[i] = reader.ReadSingle();
                }
                
                // Read polygons
                dtPoly[] polys = new dtPoly[header.polyCount];
                for (int i = 0; i < header.polyCount; i++)
                {
                    polys[i] = ReadPoly(reader);
                }
                
                // Skip links
                for (int i = 0; i < header.maxLinkCount; i++)
                {
                    reader.ReadUInt32(); // ref
                    reader.ReadUInt32(); // next
                    reader.ReadByte();   // edge
                    reader.ReadByte();   // side
                    reader.ReadByte();   // bmin
                    reader.ReadByte();   // bmax
                }
                
                // Skip detail meshes
                for (int i = 0; i < header.detailMeshCount; i++)
                {
                    reader.ReadUInt32(); // vertBase
                    reader.ReadUInt32(); // triBase
                    reader.ReadByte();   // vertCount
                    reader.ReadByte();   // triCount
                }
                
                // Skip detail vertices
                for (int i = 0; i < header.detailVertCount * 3; i++)
                {
                    reader.ReadSingle(); // x, y, z
                }
                
                // Skip detail triangles
                for (int i = 0; i < header.detailTriCount * 4; i++)
                {
                    reader.ReadByte(); // vertA, vertB, vertC, triFlags
                }
                
                // Skip BV tree
                for (int i = 0; i < header.bvNodeCount; i++)
                {
                    reader.ReadUInt16(); // bmin[0]
                    reader.ReadUInt16(); // bmin[1]
                    reader.ReadUInt16(); // bmin[2]
                    reader.ReadUInt16(); // bmax[0]
                    reader.ReadUInt16(); // bmax[1]
                    reader.ReadUInt16(); // bmax[2]
                    reader.ReadInt32();  // i
                }
                
                // Skip off-mesh connections
                for (int i = 0; i < header.offMeshConCount; i++)
                {
                    for (int j = 0; j < 6; j++) // pos[6]
                        reader.ReadSingle();
                    reader.ReadSingle(); // rad
                    reader.ReadUInt16(); // poly
                    reader.ReadByte();   // flags
                    reader.ReadByte();   // side
                    reader.ReadUInt32(); // userId
                }
                
                // Convert to NavMeshPolygon format
                var polygons = new NavMeshPolygon[header.polyCount];
                for (int i = 0; i < header.polyCount; i++)
                {
                    var poly = polys[i];
                    var polygon = new NavMeshPolygon();
                    
                    polygon.vertices = new Vector3[poly.vertCount];
                    for (int j = 0; j < poly.vertCount; j++)
                    {
                        int vertIndex = poly.verts[j] * 3;
                        Vector3 originalVertex = new Vector3(
                            vertices[vertIndex],
                            vertices[vertIndex + 1],
                            vertices[vertIndex + 2]
                        );
                        
                        // Apply Y-axis 180 degree rotation and Z-axis inversion
                        // Create rotation matrix for Y-axis 180 degree rotation
                        Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
                        
                        // Apply Y-axis 180 degree rotation
                        Vector3 rotatedVertex = rotationMatrix.MultiplyPoint3x4(originalVertex);
                        
                        // Apply Z-axis inversion
                        polygon.vertices[j] = new Vector3(
                            rotatedVertex.x,
                            rotatedVertex.y,
                            -rotatedVertex.z
                        );
                    }
                    
                    polygon.area = poly.areaAndtype & 0x3f;
                    polygon.flags = poly.flags;
                    
                    polygons[i] = polygon;
                }
                
                return polygons;
            }
        }
        
        private static dtMeshHeader ReadMeshHeader(BinaryReader reader)
        {
            var header = new dtMeshHeader();
            header.magic = reader.ReadInt32();
            header.version = reader.ReadInt32();
            header.x = reader.ReadInt32();
            header.y = reader.ReadInt32();
            header.layer = reader.ReadInt32();
            header.userId = reader.ReadUInt32();
            header.polyCount = reader.ReadInt32();
            header.vertCount = reader.ReadInt32();
            header.maxLinkCount = reader.ReadInt32();
            header.detailMeshCount = reader.ReadInt32();
            header.detailVertCount = reader.ReadInt32();
            header.detailTriCount = reader.ReadInt32();
            header.bvNodeCount = reader.ReadInt32();
            header.offMeshConCount = reader.ReadInt32();
            header.offMeshBase = reader.ReadInt32();
            header.walkableHeight = reader.ReadSingle();
            header.walkableRadius = reader.ReadSingle();
            header.walkableClimb = reader.ReadSingle();
            
            header.bmin0 = reader.ReadSingle();
            header.bmin1 = reader.ReadSingle();
            header.bmin2 = reader.ReadSingle();
            
            header.bmax0 = reader.ReadSingle();
            header.bmax1 = reader.ReadSingle();
            header.bmax2 = reader.ReadSingle();
            
            header.bvQuantFactor = reader.ReadSingle();
            
            return header;
        }
        
        private static dtPoly ReadPoly(BinaryReader reader)
        {
            var poly = new dtPoly();
            poly.firstLink = reader.ReadUInt32();
            
            poly.verts = new ushort[6];
            for (int i = 0; i < 6; i++)
            {
                poly.verts[i] = reader.ReadUInt16();
            }
            
            poly.neis = new ushort[6];
            for (int i = 0; i < 6; i++)
            {
                poly.neis[i] = reader.ReadUInt16();
            }
            
            poly.flags = reader.ReadUInt16();
            poly.vertCount = reader.ReadByte();
            poly.areaAndtype = reader.ReadByte();
            
            return poly;
        }
        
        private static Bounds CalculateBounds(NavMeshPolygon[] polygons)
        {
            if (polygons.Length == 0)
                return new Bounds();
            
            Vector3 min = polygons[0].vertices[0];
            Vector3 max = polygons[0].vertices[0];
            
            foreach (var polygon in polygons)
            {
                foreach (var vertex in polygon.vertices)
                {
                    min = Vector3.Min(min, vertex);
                    max = Vector3.Max(max, vertex);
                }
            }
            
            Bounds bounds = new Bounds();
            bounds.SetMinMax(min, max);
            return bounds;
        }
        
        // Utility method to convert Unity coordinates to RecastNavigation coordinates
        public static Vector3 UnityToRecast(Vector3 unityPos)
        {
            // Apply Y-axis 180 degree rotation and Z-axis inversion
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
            Vector3 rotatedVertex = rotationMatrix.MultiplyPoint3x4(unityPos);
            
            return new Vector3(
                rotatedVertex.x,
                rotatedVertex.y,
                -rotatedVertex.z
            );
        }
        
        // Utility method to convert RecastNavigation coordinates to Unity coordinates
        public static Vector3 RecastToUnity(Vector3 recastPos)
        {
            // Apply Z-axis inversion first, then Y-axis 180 degree rotation (inverse)
            Vector3 invertedPos = new Vector3(
                recastPos.x,
                recastPos.y,
                -recastPos.z
            );
            
            // Apply inverse Y-axis 180 degree rotation
            Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));
            return rotationMatrix.MultiplyPoint3x4(invertedPos);
        }
        
        public static void UnloadNavMesh(NavMeshData navMeshData)
        {
            if (navMeshData != null)
            {
                navMeshData.polygons = null;
            }
        }
        
        // Utility methods
        public static bool IsNavMeshValid(NavMeshData navMeshData)
        {
            return navMeshData != null && 
                   navMeshData.polygons != null && 
                   navMeshData.polygons.Length > 0;
        }
        
        public static void LogNavMeshInfo(NavMeshData navMeshData)
        {
            if (!IsNavMeshValid(navMeshData))
            {
                Debug.LogWarning("NavMesh data is not valid");
                return;
            }
            
            Debug.Log($"NavMesh Info:");
            Debug.Log($"  Bounds: {navMeshData.bounds}");
            Debug.Log($"  Polygon Count: {navMeshData.polygons.Length}");
            
            int totalVertices = 0;
            for (int i = 0; i < navMeshData.polygons.Length; i++)
            {
                if (navMeshData.polygons[i].vertices != null)
                {
                    totalVertices += navMeshData.polygons[i].vertices.Length;
                }
            }
            Debug.Log($"  Total Vertices: {totalVertices}");
        }
    }
    
    [StructLayout(LayoutKind.Sequential)]
    public struct NavMeshPolygon
    {
        public Vector3[] vertices;
        public int area;
        public int flags;
    }
    
    public class NavMeshData
    {
        public NavMeshPolygon[] polygons;
        public Bounds bounds;
        
        public NavMeshData()
        {
            polygons = null;
            bounds = new Bounds();
        }
    }
} 