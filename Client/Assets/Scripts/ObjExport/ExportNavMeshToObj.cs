using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using SharpNav;
using SharpNav.Geometry;
using Vector3 = UnityEngine.Vector3;
using SharpNav.IO;
using SharpNav.IO.Json;

// Obj exporter component based on: http://wiki.unity3d.com/index.php?title=ObjExporter

public class ExportNavMeshToObj : MonoBehaviour
{

    [MenuItem("Custom/Export NavMesh to mesh")]
    static void Export()
    {
        NavMeshTriangulation triangulatedNavMesh = UnityEngine.AI.NavMesh.CalculateTriangulation();

        Mesh mesh = new Mesh();
        mesh.name = "ExportedNavMesh";
        mesh.vertices = triangulatedNavMesh.vertices;
        mesh.triangles = triangulatedNavMesh.indices;
        string filename = Application.dataPath + "/" + Path.GetFileNameWithoutExtension(EditorApplication.currentScene) + " Exported NavMesh.obj";
        MeshToFile(mesh, filename);
        print("NavMesh exported as '" + filename + "'");
        AssetDatabase.Refresh();
    }

    public static Vector3 ToUnityVector(SharpNav.Geometry.Vector3 vector)
    {
        return new Vector3(vector.X, vector.Y, vector.Z);
    }

    // converts unity vector3 to sharpnav vector3
    public static SharpNav.Geometry.Vector3 ToSharpVector(Vector3 vector)
    {
        return new SharpNav.Geometry.Vector3(vector.x, vector.y, vector.z);
    }


    static string MeshToString(Mesh mesh)
    {
        var vertices = mesh.vertices;
        var indices = mesh.triangles;
        var triangleCount = mesh.triangles.Length;

        // convert into sharpnav Vector3 array
        SharpNav.Geometry.Vector3[] navVerts = new SharpNav.Geometry.Vector3[vertices.Length];
        for (int i = 0, length = vertices.Length; i < length; i++)
        {
            navVerts[i] = ToSharpVector(vertices[i]);
            //if(i<length-1)
            //    Debug.DrawLine(vertices[i], vertices[i+1], Color.red, 99);

        }

        //prepare the geometry from your mesh data
        var tris = TriangleEnumerable.FromVector3(navVerts, 0, 1, vertices.Length / 3);

        // check bounds
        var bounds = tris.GetBoundingBox();
        Debug.DrawLine(ToUnityVector(bounds.Min.Xzy), ToUnityVector(bounds.Max.Xzy), Color.red, 99);

        //use the default generation settings
        var settings = NavMeshGenerationSettings.Default;
        settings.AgentHeight = 1.7f;
        settings.AgentRadius = 0.6f;

        //generate the mesh
        var navMesh = SharpNav.NavMesh.Generate(tris, settings);
        new NavMeshJsonSerializer().Serialize(@"d:\tmp\test.mesh", navMesh);





        StringBuilder sb = new StringBuilder();

        sb.Append("g ").Append(mesh.name).Append("\n");
        foreach (Vector3 v in mesh.vertices)
        {
            sb.Append(string.Format("v {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in mesh.normals)
        {
            sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
        }
        sb.Append("\n");
        foreach (Vector3 v in mesh.uv)
        {
            sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
        }
        for (int material = 0; material < mesh.subMeshCount; material++)
        {
            sb.Append("\n");
            //sb.Append("usemtl ").Append(mats[material].name).Append("\n");
            //sb.Append("usemap ").Append(mats[material].name).Append("\n");

            int[] triangles = mesh.GetTriangles(material);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                sb.Append(string.Format("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}\n", triangles[i] + 1, triangles[i + 1] + 1, triangles[i + 2] + 1));
            }
        }
        return sb.ToString();
    }

    static void MeshToFile(Mesh mesh, string filename)
    {
        using (StreamWriter sw = new StreamWriter(filename))
        {
            sw.Write(MeshToString(mesh));
        }
    }
}