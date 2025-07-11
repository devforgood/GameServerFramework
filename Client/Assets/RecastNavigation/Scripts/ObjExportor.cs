using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

struct ObjMaterial
{
	public string name;
	public string textureName;
}

public class ObjExportor
{
	private static int vertexOffset = 0;
	private static int normalOffset = 0;
	private static int uvOffset = 0;


	//User should probably be able to change this. It is currently left as an excercise for
	//the reader.
	private static string targetFolder = "Assets/GeneratedObj";


	private static string MeshToString2(MeshFilter mf, Dictionary<string, ObjMaterial> materialList)
	{
		Mesh m = mf.sharedMesh;
		Material[] mats = mf.GetComponent<Renderer>().sharedMaterials;

		StringBuilder sb = new StringBuilder();

		sb.Append("g ").Append(mf.name).Append("\n");
		foreach (Vector3 lv in m.vertices)
		{
			Vector3 wv = mf.transform.TransformPoint(lv);

			//This is sort of ugly - inverting x-component since we're in
			//a different coordinate system than "everyone" is "used to".
			sb.Append(string.Format("v {0} {1} {2}\n", wv.x, wv.y, wv.z));
		}
		sb.Append("\n");

		foreach (Vector3 lv in m.normals)
		{
			Vector3 wv = mf.transform.TransformDirection(lv);

			sb.Append(string.Format("vn {0} {1} {2}\n", wv.x, wv.y, wv.z));
		}
		sb.Append("\n");

		foreach (Vector3 v in m.uv)
		{
			sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}

		for (int material = 0; material < m.subMeshCount; material++)
		{
			sb.Append("\n");
			sb.Append("usemtl ").Append(mats[material].name).Append("\n");
			sb.Append("usemap ").Append(mats[material].name).Append("\n");

			//See if this material is already in the materiallist.
			try
			{
				ObjMaterial objMaterial = new ObjMaterial();

				objMaterial.name = mats[material].name;

				if (mats[material].mainTexture)
					objMaterial.textureName = AssetDatabase.GetAssetPath(mats[material].mainTexture);
				else
					objMaterial.textureName = null;

				materialList.Add(objMaterial.name, objMaterial);
			}
			catch (ArgumentException)
			{
				//Already in the dictionary
			}


			int[] triangles = m.GetTriangles(material);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				//Because we inverted the x-component, we also needed to alter the triangle winding.
				sb.Append(string.Format("f {1}/{1}/{1} {0}/{0}/{0} {2}/{2}/{2}\n",
					triangles[i] + 1 + vertexOffset, triangles[i + 1] + 1 + normalOffset, triangles[i + 2] + 1 + uvOffset));
			}
		}

		vertexOffset += m.vertices.Length;
		normalOffset += m.normals.Length;
		uvOffset += m.uv.Length;

		return sb.ToString();
	}

	private static string MeshToString(MeshFilter mf, Dictionary<string, ObjMaterial> materialList)
	{
		Mesh m = mf.sharedMesh;
		Transform t = mf.transform;

		StringBuilder sb = new StringBuilder();

		// 그룹 이름 추가
		sb.Append("g ").Append(mf.name).Append("\n");

		// Y축 180도 회전 행렬
		Matrix4x4 rotationMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0));

		// 정점(Vertex) 좌표 추가 (Y축 180도 회전 및 Z축 반전)
		foreach (Vector3 lv in m.vertices)
		{
			// TransformPoint를 사용하여 위치, 회전, 스케일을 모두 적용
			Vector3 wv = t.localToWorldMatrix.MultiplyPoint3x4(lv);

			// Y축 180도 회전 적용
			wv = rotationMatrix.MultiplyPoint3x4(wv);

			// Z축 반전
			sb.Append(string.Format("v {0} {1} {2}\n", wv.x, wv.y, -wv.z));
		}
		sb.Append("\n");

		// 노멀(Normal) 벡터 추가 (Y축 180도 회전 및 Z축 반전)
		foreach (Vector3 lv in m.normals)
		{
			Vector3 wv = t.TransformDirection(lv);

			// Y축 180도 회전 적용
			wv = rotationMatrix.MultiplyVector(wv);

			// Z축 반전
			sb.Append(string.Format("vn {0} {1} {2}\n", wv.x, wv.y, -wv.z));
		}
		sb.Append("\n");

		// 텍스처 좌표 추가 (필요한 경우)
		foreach (Vector3 v in m.uv)
		{
			sb.Append(string.Format("vt {0} {1}\n", v.x, v.y));
		}

		// 삼각형 정의 추가 (winding order 반전)
		for (int material = 0; material < m.subMeshCount; material++)
		{
			int[] triangles = m.GetTriangles(material);
			for (int i = 0; i < triangles.Length; i += 3)
			{
				sb.Append(string.Format("f {0} {2} {1}\n",
					triangles[i] + 1 + vertexOffset,
					triangles[i + 1] + 1 + vertexOffset,
					triangles[i + 2] + 1 + vertexOffset));
			}
		}

		// 정점 오프셋 업데이트
		vertexOffset += m.vertices.Length;

		return sb.ToString();
	}
	private static void Clear()
	{
		vertexOffset = 0;
		normalOffset = 0;
		uvOffset = 0;
	}

	private static Dictionary<string, ObjMaterial> PrepareFileWrite()
	{
		Clear();

		return new Dictionary<string, ObjMaterial>();
	}

	private static void MaterialsToFile(Dictionary<string, ObjMaterial> materialList, string folder, string filename)
	{
		using (StreamWriter sw = new StreamWriter(Path.Combine(folder, filename + ".mtl")))
		{
			foreach (KeyValuePair<string, ObjMaterial> kvp in materialList)
			{
				sw.Write("\n");
				sw.Write("newmtl {0}\n", kvp.Key);
				sw.Write("Ka  0.6 0.6 0.6\n");
				sw.Write("Kd  0.6 0.6 0.6\n");
				sw.Write("Ks  0.9 0.9 0.9\n");
				sw.Write("d  1.0\n");
				sw.Write("Ns  0.0\n");
				sw.Write("illum 2\n");

				if (kvp.Value.textureName != null)
				{
					string destinationFile = kvp.Value.textureName;


					int stripIndex = destinationFile.LastIndexOf(Path.PathSeparator);

					if (stripIndex >= 0)
						destinationFile = destinationFile.Substring(stripIndex + 1).Trim();


					string relativeFile = destinationFile;

					destinationFile = Path.Combine(folder , destinationFile);

					Debug.Log("Copying texture from " + kvp.Value.textureName + " to " + destinationFile);

					try
					{
						//Copy the source file
						File.Copy(kvp.Value.textureName, destinationFile);
					}
					catch
					{

					}


					sw.Write("map_Kd {0}", relativeFile);
				}

				sw.Write("\n\n\n");
			}
		}
	}

	private static void MeshToFile(MeshFilter mf, string folder, string filename)
	{
		Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();

		using (StreamWriter sw = new StreamWriter(Path.Combine(folder, filename + ".obj")))
		{
			sw.Write("mtllib ./" + filename + ".mtl\n");

			sw.Write(MeshToString(mf, materialList));
		}

		MaterialsToFile(materialList, folder, filename);
	}

	private static void MeshesToFile(MeshFilter[] mf, string folder, string filename)
	{
		Dictionary<string, ObjMaterial> materialList = PrepareFileWrite();

		using (StreamWriter sw = new StreamWriter(Path.Combine(folder, filename + ".obj")))
		{
			sw.Write("mtllib ./" + filename + ".mtl\n");

			for (int i = 0; i < mf.Length; i++)
			{
				sw.Write(MeshToString(mf[i], materialList));
			}
		}

		MaterialsToFile(materialList, folder, filename);
	}

	private static bool CreateTargetFolder()
	{
		try
		{
			System.IO.Directory.CreateDirectory(targetFolder);
		}
		catch
		{
			EditorUtility.DisplayDialog("Error!", "Failed to create target folder!", "OK");
			return false;
		}

		return true;
	}

	public static string ExportSelectionToSeparate()
	{
		if (!CreateTargetFolder())
			return "";

		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "OK");
			return "";
		}

		int exportedObjects = 0;
		string firstExportedFile = "";

		for (int i = 0; i < selection.Length; i++)
		{
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

			for (int m = 0; m < meshfilter.Length; m++)
			{
				exportedObjects++;
				string fileName = selection[i].name + "_" + i + "_" + m;
				MeshToFile((MeshFilter)meshfilter[m], targetFolder, fileName);
				
				// 첫 번째 생성된 파일 경로 저장
				if (string.IsNullOrEmpty(firstExportedFile))
				{
					firstExportedFile = Path.Combine(targetFolder, fileName + ".obj");
				}
			}
		}

		if (exportedObjects > 0)
		{
			EditorUtility.DisplayDialog("Objects exported", "Exported " + exportedObjects + " objects", "OK");
			return firstExportedFile;
		}
		else
		{
			EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "OK");
			return "";
		}
	}

	public static string ExportWholeSelectionToSingle()
	{
		if (!CreateTargetFolder())
			return "";

		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "OK");
			return "";
		}

		int exportedObjects = 0;

		ArrayList mfList = new ArrayList();

		for (int i = 0; i < selection.Length; i++)
		{
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

			for (int m = 0; m < meshfilter.Length; m++)
			{
				exportedObjects++;
				mfList.Add(meshfilter[m]);
			}
		}

		if (exportedObjects > 0)
		{
			MeshFilter[] mf = new MeshFilter[mfList.Count];

			for (int i = 0; i < mfList.Count; i++)
			{
				mf[i] = (MeshFilter)mfList[i];
			}

			string filename = EditorSceneManager.GetActiveScene().name + "_" + exportedObjects;

			int stripIndex = filename.LastIndexOf(Path.PathSeparator);

			if (stripIndex >= 0)
				filename = filename.Substring(stripIndex + 1).Trim();

			MeshesToFile(mf, targetFolder, filename);

			string exportedFilePath = Path.Combine(targetFolder, filename + ".obj");
			EditorUtility.DisplayDialog("Objects exported", "Exported " + exportedObjects + " objects to " + filename, "OK");
			return exportedFilePath;
		}
		else
		{
			EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "OK");
			return "";
		}
	}


	public static string ExportEachSelectionToSingle()
	{
		if (!CreateTargetFolder())
			return "";

		Transform[] selection = Selection.GetTransforms(SelectionMode.Editable | SelectionMode.ExcludePrefab);

		if (selection.Length == 0)
		{
			EditorUtility.DisplayDialog("No source object selected!", "Please select one or more target objects", "OK");
			return "";
		}

		int exportedObjects = 0;
		string firstExportedFile = "";

		for (int i = 0; i < selection.Length; i++)
		{
			Component[] meshfilter = selection[i].GetComponentsInChildren(typeof(MeshFilter));

			MeshFilter[] mf = new MeshFilter[meshfilter.Length];

			for (int m = 0; m < meshfilter.Length; m++)
			{
				exportedObjects++;
				mf[m] = (MeshFilter)meshfilter[m];
			}

			string fileName = selection[i].name + "_" + i;
			MeshesToFile(mf, targetFolder, fileName);
			
			// 첫 번째 생성된 파일 경로 저장
			if (string.IsNullOrEmpty(firstExportedFile))
			{
				firstExportedFile = Path.Combine(targetFolder, fileName + ".obj");
			}
		}

		if (exportedObjects > 0)
		{
			EditorUtility.DisplayDialog("Objects exported", "Exported " + exportedObjects + " objects", "OK");
			return firstExportedFile;
		}
		else
		{
			EditorUtility.DisplayDialog("Objects not exported", "Make sure at least some of your selected objects have mesh filters!", "OK");
			return "";
		}
	}
}
