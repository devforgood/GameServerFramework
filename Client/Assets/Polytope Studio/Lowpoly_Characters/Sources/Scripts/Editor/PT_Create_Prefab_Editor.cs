using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Polytope
{
    [CustomEditor(typeof(PT_Create_Prefab))]
    public class PT_Create_Prefab_Editor : Editor
    {
        private PT_Create_Prefab script = null;

        // Meshes
        private SerializedProperty meshes = null;
        private SerializedProperty loaded = null;
        private SerializedProperty material = null;
        private SerializedProperty preview = null;
        private SerializedProperty time = null;
        private SerializedProperty duplicateMaterial = null;
        private SerializedProperty setIndex = null;
        private SerializedProperty prefabName = null;
        private SerializedProperty lastSetName = null;

        // Preview
        private float frequency = 1f;

        // Set chosen from the grouped dropdown menu, applied on the next GUI pass
        // (GenericMenu callbacks fire outside the current OnInspectorGUI).
        private int _pendingSetSelection = -1;

        // Categories the active set actually populated. Used by the head-covering
        // rules so missing parts (no helmet / hair / beard / cape) stay hidden
        // instead of inheriting a leftover mesh from the previous set.
        private HashSet<string> _setCategories = new HashSet<string>();

        // Cross-session flags marking which categories the BODY rule hid (not the
        // user). When the user cycles the body off a _NH / _NL body, these flags
        // tell us we should re-show the helmet / legs that we previously hid.
        private bool _bodyHidHelmet = false;
        private bool _bodyHidLegs = false;

        private void OnEnable()
        {
            // Meshes
            script = (PT_Create_Prefab)target;
            loaded = serializedObject.FindProperty("loaded");
            meshes = serializedObject.FindProperty("meshes");
            material = serializedObject.FindProperty("material");
            preview = serializedObject.FindProperty("preview");
            time = serializedObject.FindProperty("time");
            duplicateMaterial = serializedObject.FindProperty("duplicateMaterial");
            setIndex = serializedObject.FindProperty("setIndex");
            prefabName = serializedObject.FindProperty("prefabName");
            lastSetName = serializedObject.FindProperty("lastSetName");

            if (setIndex == null || prefabName == null || lastSetName == null)
            {
                Debug.LogWarning(
                    "PT_Create_Prefab is missing fields required by this editor " +
                    "(setIndex / prefabName / lastSetName). Set-related features will be disabled " +
                    "until PT_Create_Prefab.cs is updated.");
            }

            EditorApplication.update += Preview;
            time.floatValue = 0f;
            serializedObject.ApplyModifiedProperties();
            //Debug.Log("Enable");
        }

        private void OnDisable()
        {
            //Debug.Log("Disable");
            EditorApplication.update -= Preview;
        }

        private void Preview()
        {
            if (preview == null || time == null)
            {
                return;
            }
            if(preview.boolValue)
            {
                if (EditorApplication.timeSinceStartup > time.floatValue)
                {
                    time.floatValue = (float)EditorApplication.timeSinceStartup + frequency;
                    serializedObject.ApplyModifiedProperties();
                    MeshesRandom();
                }
            }
        }    

        private void ShaderProperty_C(string name, string label)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";

            // Check if the shader has collor for this property
            if (IsMaterialProperty(shaderColor))
            {
                SerializedProperty color = serializedObject.FindProperty(name).FindPropertyRelative("color");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(color, new GUIContent(label + " Color"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderColor, color.colorValue);
                }
            }
        }

        private void ShaderProperty_CI(string name, string label)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderTex = "_" + name.ToUpper() + "MASK";

            // Check if the shader has collor for this property
            if (IsMaterialProperty(shaderColor))
            {
                SerializedProperty color = serializedObject.FindProperty(name).FindPropertyRelative("color");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(color, new GUIContent(label + " Color"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderColor, color.colorValue);
                }
            }

            // Check if the shader has texture for this property
            if (IsMaterialProperty(shaderTex))
            {
                SerializedProperty sprite = serializedObject.FindProperty(name).FindPropertyRelative("image");
                EditorGUI.BeginChangeCheck();
                sprite.objectReferenceValue = (Sprite)EditorGUILayout.ObjectField(label + " Sprite", sprite.objectReferenceValue, typeof(Sprite), allowSceneObjects: true);
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    string pathToScriptTexture = AssetDatabase.GetAssetPath(sprite.objectReferenceValue);
                    SetMaterialProperty(shaderTex, AssetDatabase.LoadAssetAtPath<Texture>(pathToScriptTexture));
                }
            }
        }

        private void ShaderProperty_CS(string name, string label)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSmoothness = "_" + name.ToUpper() + "SMOOTHNESS";

            // Check if the shader has collor for this property
            if (IsMaterialProperty(shaderColor))
            {
                SerializedProperty color = serializedObject.FindProperty(name).FindPropertyRelative("color");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(color, new GUIContent(label + " Color"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderColor, color.colorValue);
                }
            }

            // Check if the shader has smoothness for this property
            if (IsMaterialProperty(shaderSmoothness))
            {
                SerializedProperty smoothness = serializedObject.FindProperty(name).FindPropertyRelative("smoothness");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(smoothness, new GUIContent(label + " Smoothness"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderSmoothness, smoothness.floatValue);
                }
            }
        }

        private void ShaderProperty_CSM(string name, string label)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSmoothness = "_" + name.ToUpper() + "SMOOTHNESS";
            string shaderMetallic = "_" + name.ToUpper() + "METALLIC";

            // Check if the shader has collor for this property
            if (IsMaterialProperty(shaderColor))
            {
                SerializedProperty color = serializedObject.FindProperty(name).FindPropertyRelative("color");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(color, new GUIContent(label + " Color"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderColor, color.colorValue);
                }
            }

            // Check if the shader has smoothness for this property
            if (IsMaterialProperty(shaderSmoothness))
            {
                SerializedProperty smoothness = serializedObject.FindProperty(name).FindPropertyRelative("smoothness");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(smoothness, new GUIContent(label + " Smoothness"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderSmoothness, smoothness.floatValue);
                }
            }

            // Check if the shader has metallic for this property
            if (IsMaterialProperty(shaderMetallic))
            {
                SerializedProperty metallic = serializedObject.FindProperty(name).FindPropertyRelative("metallic");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(metallic, new GUIContent(label + " Metallic"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderMetallic, metallic.floatValue);
                }
            }
        }

        private void ShaderProperty_CSP(string name, string label)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSize = "_" + name.ToUpper() + "SIZE";
            string shaderPower = "_" + name.ToUpper() + "POWER";

            // Check if the shader has collor for this property
            if (IsMaterialProperty(shaderColor))
            {
                SerializedProperty color = serializedObject.FindProperty(name).FindPropertyRelative("color");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(color, new GUIContent(label + " Color"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderColor, color.colorValue);
                }
            }

            // Check if the shader has size for this property
            if (IsMaterialProperty(shaderSize))
            {
                SerializedProperty size = serializedObject.FindProperty(name).FindPropertyRelative("size");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(size, new GUIContent(label + " Size"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderSize, size.floatValue);
                }
            }

            // Check if the shader has power for this property
            if (IsMaterialProperty(shaderPower))
            {
                SerializedProperty power = serializedObject.FindProperty(name).FindPropertyRelative("power");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(power, new GUIContent(label + " Power"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty(shaderPower, power.floatValue);
                }
            }
        }

        private void ShaderProperty_UTL()
        {
            Material inst = material.FindPropertyRelative("instance").objectReferenceValue as Material;
            if (inst == null) { return; }

            if (IsMaterialProperty("_MetalicOn"))
            {
                SerializedProperty metallicOn = serializedObject.FindProperty("metallicOn");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(metallicOn, new GUIContent("Metallic ON"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty("_MetalicOn", metallicOn.boolValue ? 1f : 0f);
                }
            }

            if (IsMaterialProperty("_SmoothnessOn"))
            {
                SerializedProperty smoothnessOn = serializedObject.FindProperty("smoothnessOn");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(smoothnessOn, new GUIContent("Smoothness ON"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty("_SmoothnessOn", smoothnessOn.boolValue ? 1f : 0f);
                }
            }

            if (IsMaterialProperty("_OCCLUSION"))
            {
                SerializedProperty occlusion = serializedObject.FindProperty("occlusion");
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(occlusion, new GUIContent("Occlusion"));
                if (EditorGUI.EndChangeCheck())
                {
                    serializedObject.ApplyModifiedProperties();
                    SetMaterialProperty("_OCCLUSION", occlusion.floatValue);
                }
            }

            SerializedProperty gpuinstancing = serializedObject.FindProperty("gpuinstancing");
            SerializedProperty doubleSidedGI = serializedObject.FindProperty("doubleSidedGI");
            SerializedProperty renderQueue = serializedObject.FindProperty("renderQueue");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(gpuinstancing, new GUIContent("Gpu Instancing"));
            EditorGUILayout.PropertyField(doubleSidedGI, new GUIContent("Double Sided GI"));
            EditorGUILayout.PropertyField(renderQueue, new GUIContent("Render Queue"));
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                Undo.RecordObject(inst, "Edit Material Property");
                inst.enableInstancing = gpuinstancing.boolValue;
                inst.doubleSidedGI = doubleSidedGI.boolValue;
                inst.renderQueue = renderQueue.intValue;
                EditorUtility.SetDirty(inst);
            }
        }

        private void ShaderDropDownSelection()
        {
            List<string> materialNames = new List<string>();
            SerializedProperty list = material.FindPropertyRelative("assets");
            SerializedProperty idx = material.FindPropertyRelative("index");

            for (int i = 0; i < list.arraySize; i++)
            {
                materialNames.Add((list.GetArrayElementAtIndex(i).objectReferenceValue as Material).name);
            }

            int index = EditorGUILayout.Popup("Material", idx.intValue, materialNames.ToArray());
            if (index < 0) { index = 0; }
            if(idx.intValue != index)
            {
                idx.intValue = index;
                // Reference the selected asset directly (no copy) so slider edits
                // land on the real material and show on the parts using it.
                material.FindPropertyRelative("instance").objectReferenceValue =
                    list.GetArrayElementAtIndex(index).objectReferenceValue as Material;
                serializedObject.ApplyModifiedProperties();
                // Load the selected material's current values into the sliders.
                DefaultMaterialProperties();
                // No-op on a multi-material rig (guarded); on a single-material
                // character it points every part at the chosen material.
                ReapplyMaterials();
            }
        }

        private bool IsMaterialProperty(string name)
        {
            try
            {
                return (material.FindPropertyRelative("instance").objectReferenceValue as Material).HasProperty(name);
            }
            catch (Exception exception)
            {
                Debug.LogError("Property: " + name + " message: " + exception.Message);
                return false;
            }

        }

        // The shader UI edits the SELECTED material (material.instance, which now
        // references the chosen asset directly). Writes happen only from inside a
        // change-check block (i.e. when the user actually moves a slider), never on
        // a plain repaint, and only ever touch this one material -- never a loop
        // over every part. Undo + SetDirty make the edit persist and be revertable,
        // exactly like editing the material in the Project window.
        private void SetMaterialProperty(string name, float value)
        {
            Material instance = material.FindPropertyRelative("instance").objectReferenceValue as Material;
            if (instance != null)
            {
                Undo.RecordObject(instance, "Edit Material Property");
                instance.SetFloat(name, value);
                EditorUtility.SetDirty(instance);
            }
        }

        private void SetMaterialProperty(string name, Color color)
        {
            Material instance = material.FindPropertyRelative("instance").objectReferenceValue as Material;
            if (instance != null)
            {
                Undo.RecordObject(instance, "Edit Material Property");
                instance.SetColor(name, color);
                EditorUtility.SetDirty(instance);
            }
        }

        private void SetMaterialProperty(string name, Texture texture)
        {
            Material instance = material.FindPropertyRelative("instance").objectReferenceValue as Material;
            if (instance != null)
            {
                Undo.RecordObject(instance, "Edit Material Property");
                instance.SetTexture(name, texture);
                EditorUtility.SetDirty(instance);
            }
        }

        private void GetDefaultProperty_C(string name)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty(shaderColor))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("color").colorValue = dftmat.GetColor(shaderColor);
            }
        }

        private void GetDefaultProperty_CI(string name)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderTex = "_" + name.ToUpper() + "MASK";
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty(shaderColor))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("color").colorValue = dftmat.GetColor(shaderColor);
            }

            if (IsMaterialProperty(shaderTex))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("image").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GetAssetPath(dftmat.GetTexture(shaderTex)));
            }
        }

        private void GetDefaultProperty_CS(string name)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSmoothness = "_" + name.ToUpper() + "SMOOTHNESS";
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty(shaderColor))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("color").colorValue = dftmat.GetColor(shaderColor);

            }

            if (IsMaterialProperty(shaderSmoothness))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("smoothness").floatValue = dftmat.GetFloat(shaderSmoothness);
            }
        }

        private void GetDefaultPorperty_CSM(string name)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSmoothness = "_" + name.ToUpper() + "SMOOTHNESS";
            string shaderMetallic = "_" + name.ToUpper() + "METALLIC";
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty(shaderColor))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("color").colorValue = dftmat.GetColor(shaderColor);
            }

            if (IsMaterialProperty(shaderSmoothness))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("smoothness").floatValue = dftmat.GetFloat(shaderSmoothness);
            }

            if (IsMaterialProperty(shaderMetallic))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("metallic").floatValue = dftmat.GetFloat(shaderMetallic);
            }
        }

        private void GetDefaultProperty_CSP(string name)
        {
            string shaderColor = "_" + name.ToUpper() + "COLOR";
            string shaderSize = "_" + name.ToUpper() + "SIZE";
            string shaderPower = "_" + name.ToUpper() + "POWER";
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty(shaderColor))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("color").colorValue = dftmat.GetColor(shaderColor);
            }

            if (IsMaterialProperty(shaderSize))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("size").floatValue = dftmat.GetFloat(shaderSize);
            }

            if (IsMaterialProperty(shaderPower))
            {
                serializedObject.FindProperty(name).FindPropertyRelative("power").floatValue = dftmat.GetFloat(shaderPower);
            }
        }

        private void GetDefaultProperty_UTL()
        {
            Material dftmat = material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue as Material;

            if (IsMaterialProperty("_OCCLUSION"))
            {
                serializedObject.FindProperty("occlusion").floatValue = dftmat.GetFloat("_OCCLUSION");
            }

            if (IsMaterialProperty("_MetalicOn"))
            {
                serializedObject.FindProperty("metallicOn").boolValue = dftmat.GetFloat("_MetalicOn") > 0f ? true : false;
            }

            if (IsMaterialProperty("_SmoothnessOn"))
            {
                serializedObject.FindProperty("smoothnessOn").boolValue = dftmat.GetFloat("_SmoothnessOn") > 0f ? true : false;
            }

            SerializedProperty gpuinstancing = serializedObject.FindProperty("gpuinstancing");
            SerializedProperty doubleSidedGI = serializedObject.FindProperty("doubleSidedGI");
            SerializedProperty renderQueue = serializedObject.FindProperty("renderQueue");
            gpuinstancing.boolValue = dftmat.enableInstancing;
            doubleSidedGI.boolValue = dftmat.doubleSidedGI;
            renderQueue.intValue = dftmat.renderQueue;
        }

        private void DefaultMaterialProperties()
        {
            GetDefaultProperty_CS("skin");
            GetDefaultProperty_CS("eyes");
            GetDefaultProperty_CS("hair");
            GetDefaultProperty_CS("sclera");
            GetDefaultProperty_CS("lips");
            GetDefaultProperty_CS("scars");
            GetDefaultPorperty_CSM("metal1");
            GetDefaultPorperty_CSM("metal2");
            GetDefaultPorperty_CSM("metal3");
            GetDefaultProperty_CS("leather1");
            GetDefaultProperty_CS("leather2");
            GetDefaultProperty_CS("leather3");
            GetDefaultProperty_C("cloth1");
            GetDefaultProperty_C("cloth2");
            GetDefaultProperty_C("cloth3");
            GetDefaultProperty_CS("gems1");
            GetDefaultProperty_CS("gems2");
            GetDefaultProperty_CS("gems3");
            GetDefaultProperty_C("feathers1");
            GetDefaultProperty_C("feathers2");
            GetDefaultProperty_C("feathers3");
            GetDefaultProperty_CI("coatofarms");
            GetDefaultProperty_CSP("light1");
            GetDefaultProperty_CSP("light2");
            GetDefaultProperty_CSP("light3");
            GetDefaultProperty_UTL();
        }

        public void ExpansionDragAndDrop()
        {
            Event evt = Event.current;
            Rect area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));

            // Create the box
            int width = 2;
            int height = 2;
            GUIStyle style = new GUIStyle(GUI.skin.box);
            Texture2D texture = new Texture2D(width, height);
            Color background = Color.white;
            Color[] pix = new Color[width * height];

            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = background;
            }

            texture.SetPixels(pix);
            texture.Apply();
            style.normal.background = texture;
            GUI.Box(area, "Drag & Drop Polytope expansion prefab here", style);

            // Handle the event
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (!area.Contains(evt.mousePosition))
                        return;

                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                    if (evt.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        GameObject expansion = (GameObject)DragAndDrop.objectReferences[0];

                        Animator expansionAnimator = expansion != null ? expansion.GetComponent<Animator>() : null;
                        Animator baseAnimator = script.gameObject.GetComponent<Animator>();
                        Avatar baseAvatar = baseAnimator != null ? baseAnimator.avatar : null;

                        if (expansionAnimator == null || baseAvatar == null)
                        {
                            EditorUtility.DisplayDialog("Expansion",
                                "Prefab cannot be imported! Not a Polytope expansion prefab (missing Animator or Avatar).",
                                "Ok");
                            return;
                        }

                        // If the expansion's avatar doesn't match the base, offer to patch it
                        // on the expansion prefab itself so future imports of the same pack
                        // skip the prompt.
                        if (expansionAnimator.avatar != baseAvatar)
                        {
                            string expansionName = expansionAnimator.avatar != null
                                ? expansionAnimator.avatar.name : "(none)";

                            bool proceed = EditorUtility.DisplayDialog("Avatar mismatch",
                                "The expansion's avatar (" + expansionName + ") doesn't match " +
                                "the base prefab's avatar (" + baseAvatar.name + ").\n\n" +
                                "Update the expansion to use the base avatar and continue importing?",
                                "Update and import", "Cancel");

                            if (!proceed)
                            {
                                return;
                            }

                            string assetPath = AssetDatabase.GetAssetPath(expansion);
                            if (!string.IsNullOrEmpty(assetPath))
                            {
                                // Project prefab asset -> edit in place via prefab API.
                                GameObject contents = PrefabUtility.LoadPrefabContents(assetPath);
                                Animator contentsAnimator = contents != null ? contents.GetComponent<Animator>() : null;
                                if (contentsAnimator == null)
                                {
                                    PrefabUtility.UnloadPrefabContents(contents);
                                    EditorUtility.DisplayDialog("Expansion",
                                        "Could not update expansion: no Animator on the prefab root.",
                                        "Ok");
                                    return;
                                }
                                contentsAnimator.avatar = baseAvatar;
                                PrefabUtility.SaveAsPrefabAsset(contents, assetPath);
                                PrefabUtility.UnloadPrefabContents(contents);
                                AssetDatabase.SaveAssets();

                                // Re-fetch the now-patched expansion to import from.
                                expansion = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                                expansionAnimator = expansion.GetComponent<Animator>();
                            }
                            else
                            {
                                // In-scene instance.
                                Undo.RecordObject(expansionAnimator, "Patch expansion avatar");
                                expansionAnimator.avatar = baseAvatar;
                                EditorUtility.SetDirty(expansionAnimator);
                            }
                        }

                        SkinnedMeshRenderer[] importMeshes = expansion.GetComponentsInChildren<SkinnedMeshRenderer>();
                        foreach (SkinnedMeshRenderer mesh in importMeshes)
                        {
                            // File each imported mesh into its category the SAME way
                            // set discovery reads it: by parsed part token, which is
                            // case-insensitive. The old `mesh.name.Contains(type)`
                            // match was case-sensitive against the lowercase type
                            // strings, so a mesh named "..._Body_01" (capitalised, as
                            // the Ex2 pack uses) was silently skipped -- it never
                            // entered the category lists, so no set appeared for it.
                            PartInfo imported = ParseName(mesh);
                            if (!imported.valid)
                            {
                                continue; // not a recognisable part (root / helper / etc.)
                            }

                            SerializedProperty cat = MeshesGetType(imported.part);
                            if (cat == null)
                            {
                                continue;
                            }
                            SerializedProperty list = cat.FindPropertyRelative("list");
                            if (list.arraySize == 0)
                            {
                                continue; // no existing part here to copy the rig from
                            }

                            SkinnedMeshRenderer target = list.GetArrayElementAtIndex(0).objectReferenceValue as SkinnedMeshRenderer;
                            if (target == null)
                            {
                                continue;
                            }

                            SkinnedMeshRenderer instance = Instantiate<SkinnedMeshRenderer>(mesh);
                            instance.name = mesh.name; // drop the "(Clone)" suffix
                            instance.transform.parent = target.transform.parent;
                            instance.transform.localPosition = target.transform.localPosition;
                            instance.transform.localRotation = target.transform.localRotation;
                            instance.transform.localScale = target.transform.localScale;
                            instance.bones = target.bones;
                            instance.rootBone = target.rootBone;
                            instance.enabled = false;
                            list.arraySize += 1;
                            list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = instance;

                            mesh.enabled = false;
                        }

                        serializedObject.ApplyModifiedProperties();


                        EditorUtility.DisplayDialog("Expansion",
                                "Meshes imported successfully!",
                                "Ok");
                    }
                    break;
                default:
                    break;
            }
        }

        // Helmet / hair / beard cross-category enforcement, called after a single
        // category was touched (cycled or toggled). Each branch inspects the
        // actual current enabled state instead of assuming the touched category
        // just became visible, so it also works when the user UNchecks a part.
        private void MeshesUpdateSpecials(string typeOfMeshes)
        {
            SkinnedMeshRenderer helmet = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.helmet));
            SkinnedMeshRenderer hair = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.hair));
            SkinnedMeshRenderer beard = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.beard));

            bool helmetOn = helmet != null && helmet.enabled;

            if (typeOfMeshes.Equals(PT_Create_Prefab.TypeOfMesh.helmet))
            {
                if (helmetOn)
                {
                    if (hair != null) hair.enabled = false;
                    if (beard != null) beard.enabled = NameIsNv(helmet.name);
                }
                else
                {
                    if (hair != null) hair.enabled = true;
                    if (beard != null) beard.enabled = true;
                }
            }
            else if (typeOfMeshes.Equals(PT_Create_Prefab.TypeOfMesh.hair))
            {
                bool hairOn = hair != null && hair.enabled;
                if (hairOn)
                {
                    if (helmet != null) helmet.enabled = false;
                    if (beard != null) beard.enabled = true;
                }
            }
            else if (typeOfMeshes.Equals(PT_Create_Prefab.TypeOfMesh.beard))
            {
                bool beardOn = beard != null && beard.enabled;
                if (beardOn && helmet != null)
                {
                    if (NameIsNv(helmet.name))
                    {
                        helmet.enabled = true;
                        if (hair != null) hair.enabled = false;
                    }
                    else
                    {
                        helmet.enabled = false;
                        if (hair != null) hair.enabled = true;
                    }
                }
            }
            else if (typeOfMeshes.Equals(PT_Create_Prefab.TypeOfMesh.body))
            {
                // Body-driven cross-part rules. _NH / _NL are reversible: when
                // the user cycles off a restrictive body, the helmet / legs we
                // hid because of that body come back on. _NG is one-way (no
                // restore: once gauntlets are switched to _S, the user can
                // change them back manually if they want).
                SkinnedMeshRenderer body = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.body));
                PartInfo bodyInfo = body != null ? ParseName(body) : null;
                bool hasNG = bodyInfo != null && bodyInfo.HasTag("ng");
                bool hasNL = bodyInfo != null && bodyInfo.HasTag("nl");
                bool hasNH = bodyInfo != null && bodyInfo.HasTag("nh");

                if (hasNG)
                {
                    ForceTaggedPart(PT_Create_Prefab.TypeOfMesh.gauntlets, "s", "", "");
                }

                if (hasNL)
                {
                    HideCategory(PT_Create_Prefab.TypeOfMesh.legs);
                    _bodyHidLegs = true;
                }
                else if (_bodyHidLegs)
                {
                    SkinnedMeshRenderer legs = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.legs));
                    if (legs != null) legs.enabled = true;
                    _bodyHidLegs = false;
                }

                if (hasNH)
                {
                    if (helmet != null) helmet.enabled = false;
                    if (hair != null) hair.enabled = true;
                    if (beard != null) beard.enabled = true;
                    _bodyHidHelmet = true;
                }
                else if (_bodyHidHelmet)
                {
                    // Re-show the helmet that the previous _NH body hid, and
                    // re-apply the helmet/hair/beard cascade (helmet shown ->
                    // hair hidden, beard shown only when helmet is _NV).
                    if (helmet != null)
                    {
                        helmet.enabled = true;
                        if (hair != null) hair.enabled = false;
                        if (beard != null) beard.enabled = NameIsNv(helmet.name);
                    }
                    _bodyHidHelmet = false;
                }
            }
            else if (typeOfMeshes.Equals(PT_Create_Prefab.TypeOfMesh.legs))
            {
                // Legs-driven cross-part rules.
                SkinnedMeshRenderer legs = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.legs));
                if (legs != null && ParseName(legs).HasTag("lb"))
                {
                    ForceTaggedPart(PT_Create_Prefab.TypeOfMesh.boots, "l", "", "");
                }
            }
        }

        // Randomize by picking a whole set + variant. All set/tag rules and
        // fallbacks are applied via ApplySet, so the randomizer and the preview
        // (which calls into here on a timer) honor exactly the same logic as the
        // Set dropdown.
        public void MeshesRandom()
        {
            List<SetEntry> entries = BuildSetEntries();
            if (entries.Count == 0)
            {
                return;
            }

            int pick;
            if (entries.Count == 1)
            {
                pick = 0;
            }
            else
            {
                int current = setIndex != null ? setIndex.intValue : -1;
                do
                {
                    pick = UnityEngine.Random.Range(0, entries.Count);
                }
                while (pick == current); // avoid picking the same set twice in a row
            }

            if (setIndex != null)
            {
                setIndex.intValue = pick;
            }
            ApplySet(entries[pick].prefix, entries[pick].variant);
            serializedObject.ApplyModifiedProperties();
        }

        private bool MeshesCheckForBeard()
        {
            SerializedProperty beard = MeshesGetType(PT_Create_Prefab.TypeOfMesh.beard);
            return beard.FindPropertyRelative("list").arraySize > 0;
        }

        private SerializedProperty MeshesGetType(string typeOfMeshes)
        {
            if (meshes == null)
            {
                return null;
            }
            for (int i = 0; i < meshes.arraySize; i++)
            {
                if (meshes.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue == typeOfMeshes)
                {
                    return meshes.GetArrayElementAtIndex(i);
                }
            }

            return null;
        }

        // Underscore-joined file-style name for a set entry. Used as the default
        // GameObject name and as the marker for detecting whether the user has
        // hand-edited the name field (anything other than this exact string
        // counts as a manual override).
        private static string SetEntryFileName(SetEntry e)
        {
            if (e == null || string.IsNullOrEmpty(e.prefix))
            {
                return "";
            }
            return string.IsNullOrEmpty(e.variant) ? e.prefix : e.prefix + "_" + e.variant;
        }

        // Called after any per-category cycle/toggle/checkbox change. If the
        // current name still equals the original auto-generated set name, we
        // append "_custom" so the exported prefab reflects that the loadout
        // diverges from the set. If the user has already typed a custom name,
        // we leave it alone.
        private void MarkCustomized()
        {
            if (prefabName == null || lastSetName == null)
            {
                return;
            }
            string baseName = lastSetName.stringValue ?? "";
            string current = prefabName.stringValue ?? "";

            if (current == baseName && !string.IsNullOrEmpty(baseName))
            {
                prefabName.stringValue = baseName + "_custom";
                serializedObject.ApplyModifiedProperties();
            }
        }

        // ===================================================================
        // SET SELECTION
        // A "set" is a group of armor parts that share the same name prefix,
        // e.g. PT_Female_Armor_01_A_body / _boots / _hair / _head / _legs /
        // _gauntlets / _cape  ->  set "PT_Female_Armor_01_A".
        //
        // Naming convention for trailing tags (after the part token):
        //   - a single LOWERCASE letter (_a, _b, ...) is a VARIANT. Parts that
        //     share a variant are worn together and the variant is shown as its
        //     own entry in the Set dropdown.
        //   - UPPERCASE tokens (_NV, _NH, _LB, _L, ...) are RULE tags that drive
        //     cross-part constraints (see ApplySet / MeshesUpdateSpecials).
        // ===================================================================

        // Known part tokens. The part is the right-most of these in a mesh name.
        private static readonly HashSet<string> PartTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            PT_Create_Prefab.TypeOfMesh.hair,
            PT_Create_Prefab.TypeOfMesh.head,
            PT_Create_Prefab.TypeOfMesh.beard,
            PT_Create_Prefab.TypeOfMesh.helmet,
            PT_Create_Prefab.TypeOfMesh.body,
            PT_Create_Prefab.TypeOfMesh.boots,
            PT_Create_Prefab.TypeOfMesh.cape,
            PT_Create_Prefab.TypeOfMesh.gauntlets,
            PT_Create_Prefab.TypeOfMesh.legs,
            PT_Create_Prefab.TypeOfMesh.upper,
            PT_Create_Prefab.TypeOfMesh.lower,
        };

        // Parsed view of a mesh name.
        private class PartInfo
        {
            public SkinnedMeshRenderer smr = null;
            public string prefix = "";                       // e.g. PT_Female_Armor_01_A
            public string part = "";                         // e.g. body / legs / helmet
            public string variant = "";                      // "", "a", "b", ...
            public List<string> tags = new List<string>();   // lowercased: "nv","nh","lb","l"
            public bool valid = false;

            public bool HasTag(string t) { return tags.Contains(t); }
        }

        // Remove the "(Clone)" suffix Unity appends to instantiated meshes.
        private static string StripClone(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return name;
            }

            int p = name.IndexOf("(Clone)", StringComparison.OrdinalIgnoreCase);
            if (p >= 0)
            {
                name = name.Remove(p);
            }
            return name.Trim().TrimEnd('_', ' ');
        }

        // Parse a mesh name into prefix / part / variant / tags.
        private PartInfo ParseName(SkinnedMeshRenderer smr)
        {
            PartInfo info = new PartInfo();
            info.smr = smr;
            if (smr == null)
            {
                return info;
            }

            string name = StripClone(smr.name);
            string[] tokens = name.Split('_');

            int partIdx = -1;
            for (int i = tokens.Length - 1; i >= 0; i--)
            {
                if (PartTokens.Contains(tokens[i]))
                {
                    partIdx = i;
                    break;
                }
            }

            if (partIdx < 0)
            {
                return info; // no recognisable part token
            }

            info.part = tokens[partIdx].ToLowerInvariant();
            info.prefix = string.Join("_", tokens, 0, partIdx);

            // Ex2 organises its sets into 3 tiers tagged _A / _B / _C. For Ex2
            // prefixes those letters are treated as a variant TIER, not a rule
            // tag, so each tier becomes its own set. _L / _S (long boots / small
            // gauntlets) and every other tag are unaffected. The tier is captured
            // here and appended as the LAST variant component below, so it is
            // order-independent: "_01_A" and "_A_01" both yield variant "01_A".
            bool ex2 = IsEx2(info.prefix);
            string ex2Tier = null;

            for (int j = partIdx + 1; j < tokens.Length; j++)
            {
                string t = tokens[j];
                if (t.Length == 0)
                {
                    continue;
                }

                bool isLowerLetter = t.Length == 1 && char.IsLetter(t[0]) && char.IsLower(t[0]);
                bool isNumeric = t.Length > 0 && t.All(char.IsDigit);
                bool isEx2Tier = ex2 && t.Length == 1 && (t == "A" || t == "B" || t == "C");

                if (isEx2Tier)
                {
                    ex2Tier = t; // preserve case to keep the _A/_B/_C convention
                }
                else if (isLowerLetter || isNumeric)
                {
                    // Variant component. Accumulate so e.g. "_a_01" -> "a_01".
                    info.variant = string.IsNullOrEmpty(info.variant) ? t : info.variant + "_" + t;
                }
                else
                {
                    info.tags.Add(t.ToLowerInvariant());  // everything else -> rule tag
                }
            }

            if (ex2Tier != null)
            {
                info.variant = string.IsNullOrEmpty(info.variant) ? ex2Tier : info.variant + "_" + ex2Tier;
            }

            info.valid = true;
            return info;
        }

        // Case-insensitive helmet "_nv" check (asset naming uses _NV).
        private static bool NameIsNv(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf("_nv", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool NameIsNaked(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                   name.IndexOf("naked", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        // Drop the "naked" token from a naked prefix so the set can borrow that
        // gender's shared head / hair: "PT_Male_Armor_naked" -> "PT_Male_Armor".
        private static string NakedBasePrefix(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return prefix;
            }
            return string.Join("_", prefix.Split('_')
                .Where(t => !t.Equals("naked", StringComparison.OrdinalIgnoreCase)));
        }

        // Return the SkinnedMeshRenderer currently pointed to by a category's idx (or null).
        private SkinnedMeshRenderer CategoryCurrent(SerializedProperty category)
        {
            if (category == null)
            {
                return null;
            }

            SerializedProperty list = category.FindPropertyRelative("list");
            if (list.arraySize == 0)
            {
                return null;
            }

            int idx = category.FindPropertyRelative("idx").intValue;
            if (idx < 0 || idx >= list.arraySize)
            {
                return null;
            }

            return list.GetArrayElementAtIndex(idx).objectReferenceValue as SkinnedMeshRenderer;
        }

        // First index in a category list whose parsed info satisfies pred (-1 if none).
        private int FindIndex(SerializedProperty list, System.Func<PartInfo, bool> pred)
        {
            for (int k = 0; k < list.arraySize; k++)
            {
                PartInfo info = ParseName(list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer);
                if (info.smr != null && pred(info))
                {
                    return k;
                }
            }
            return -1;
        }

        // Pick the best part in a category for (prefix, variant), optionally requiring a tag.
        // Prefers an exact variant match, then falls back to a no-variant part.
        // Variant parent chain: "01_a" -> "01" -> "".
        // A child variant inherits parts from its parent variant by walking up
        // the chain on each category lookup. Lets a Jester _01_a set use the
        // body/legs/head/etc. tagged just "_01".
        private static IEnumerable<string> VariantChain(string variant)
        {
            string v = variant ?? "";
            while (!string.IsNullOrEmpty(v))
            {
                yield return v;
                int i = v.LastIndexOf('_');
                v = i > 0 ? v.Substring(0, i) : "";
            }
            yield return "";
        }

        // Pick the best part in a category for (prefix, variant), optionally
        // requiring a tag. Walks the variant parent chain so a sub-variant
        // (e.g. "01_a") falls back to its parent ("01") and finally to the
        // no-variant parts before giving up.
        private int PickIndexForSet(SerializedProperty list, string prefix, string variant, string requireTag)
        {
            foreach (string v in VariantChain(variant))
            {
                int idx = FindIndex(list, info =>
                    info.prefix == prefix && info.variant == v &&
                    (requireTag == null || info.HasTag(requireTag)));
                if (idx >= 0)
                {
                    return idx;
                }
            }
            return -1;
        }

        // True when a prefix carries the "_Ex1" expansion tag (token-exact, so it
        // does not match Ex2 / Ex3 / Ex10 etc.). The Ex1 pack was not authored as
        // matched sets, so it uses index-driven sets with random gap-filling.
        private static bool IsEx1(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }
            foreach (string t in prefix.Split('_'))
            {
                if (string.Equals(t, "Ex1", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // True when a prefix carries the "_Ex2" expansion tag (token-exact). Ex2
        // is authored as complete sets organised into 3 tiers (_A / _B / _C),
        // which are handled as variant tiers in ParseName.
        private static bool IsEx2(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }
            foreach (string t in prefix.Split('_'))
            {
                if (string.Equals(t, "Ex2", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        // Deterministic 32-bit FNV-1a hash. Used to make the Ex1 random fill
        // stable: the same (set, category) always resolves to the same piece, so
        // selecting a set in the dropdown and exporting it are reproducible.
        private static uint StableHash(string s)
        {
            unchecked
            {
                uint hash = 2166136261;
                for (int i = 0; i < s.Length; i++)
                {
                    hash ^= s[i];
                    hash *= 16777619;
                }
                return hash;
            }
        }

        // Ex1 part picker: the correspondent (prefix, variant) part if it exists,
        // otherwise a deterministic random piece chosen ONLY from Ex1 pieces in
        // this category (never the Naked piece). When a tag is required and at
        // least one Ex1 piece carries it, the random pool is restricted to those.
        // The seed is (variant + category), so the choice is stable across repaints
        // and exports. Returns -1 only if the Ex1 pool for this category is empty.
        private int PickIndexForSetEx1(SerializedProperty list, string prefix, string variant, string requireTag, string categoryType)
        {
            int idx = PickIndexForSet(list, prefix, variant, requireTag);
            if (idx >= 0)
            {
                return idx;
            }

            List<int> candidates = new List<int>();
            List<int> tagged = new List<int>();
            for (int k = 0; k < list.arraySize; k++)
            {
                PartInfo info = ParseName(list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer);
                if (info.smr == null || !info.valid)
                {
                    continue;
                }
                if (!IsEx1(info.prefix) || NameIsNaked(info.smr.name))
                {
                    continue; // Ex1 pool only, never the Naked piece
                }
                candidates.Add(k);
                if (!string.IsNullOrEmpty(requireTag) && info.HasTag(requireTag))
                {
                    tagged.Add(k);
                }
            }

            List<int> pool = tagged.Count > 0 ? tagged : candidates;
            if (pool.Count == 0)
            {
                return -1;
            }

            uint h = StableHash(variant + "|" + categoryType);
            return pool[(int)(h % (uint)pool.Count)];
        }

        private class SetEntry
        {
            public string display;
            public string prefix;
            public string variant;
        }

        // Enumerate the selectable sets. A prefix qualifies if it owns a body
        // part. Each non-empty variant becomes its own entry; the Naked
        // fallback parts are excluded.
        private List<SetEntry> BuildSetEntries()
        {
            Dictionary<string, bool> hasBody = new Dictionary<string, bool>();
            Dictionary<string, SortedSet<string>> variants = new Dictionary<string, SortedSet<string>>();

            if (meshes == null)
            {
                return new List<SetEntry>();
            }

            for (int i = 0; i < meshes.arraySize; i++)
            {
                SerializedProperty list = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("list");
                for (int k = 0; k < list.arraySize; k++)
                {
                    PartInfo info = ParseName(list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer);
                    // Naked groups (PT_*_Armor_naked_*) are no longer excluded here:
                    // a naked group that owns a body forms its own "naked character"
                    // set (gated by hasBody below, like any other prefix). Naked
                    // pieces remain usable as fallbacks for other sets regardless.
                    if (!info.valid || string.IsNullOrEmpty(info.prefix))
                    {
                        continue;
                    }

                    if (!variants.ContainsKey(info.prefix))
                    {
                        variants[info.prefix] = new SortedSet<string>(StringComparer.Ordinal);
                        hasBody[info.prefix] = false;
                    }

                    if (info.variant != "")
                    {
                        // Ex1 capes run to 44 but we only want sets up to the
                        // helmet count, so cape variants do NOT spawn their own
                        // sets. Capes stay pickable; they just don't create entries.
                        bool ex1CapeVariant = IsEx1(info.prefix) &&
                                              info.part == PT_Create_Prefab.TypeOfMesh.cape;
                        if (!ex1CapeVariant)
                        {
                            variants[info.prefix].Add(info.variant);
                        }
                    }
                    if (info.part == PT_Create_Prefab.TypeOfMesh.body)
                    {
                        hasBody[info.prefix] = true;
                    }
                }
            }

            List<SetEntry> entries = new List<SetEntry>();
            foreach (string prefix in variants.Keys)
            {
                // Ex1 sets are index-driven and emit every variant even where the
                // body (or other parts) are missing -- the gaps are random-filled
                // in ApplySet. Non-Ex1 sets still require their own body part.
                if (!IsEx1(prefix) && !hasBody[prefix])
                {
                    continue;
                }

                SortedSet<string> vset = variants[prefix];
                if (vset.Count == 0)
                {
                    entries.Add(new SetEntry { prefix = prefix, variant = "", display = prefix });
                    continue;
                }

                // A variant is a "parent" if another variant in the same set
                // extends it via the "_" separator (e.g. "01" is the parent of
                // "01_a" and "01_b"). Parents are hidden because their child
                // variants already inherit all of their parts through the
                // variant chain walk in PickIndexForSet.
                HashSet<string> parents = new HashSet<string>(StringComparer.Ordinal);
                foreach (string v in vset)
                {
                    int i = v.LastIndexOf('_');
                    while (i > 0)
                    {
                        string ancestor = v.Substring(0, i);
                        if (vset.Contains(ancestor))
                        {
                            parents.Add(ancestor);
                        }
                        i = ancestor.LastIndexOf('_');
                    }
                }

                foreach (string v in vset)
                {
                    if (parents.Contains(v))
                    {
                        continue; // child variants cover this one
                    }
                    entries.Add(new SetEntry { prefix = prefix, variant = v, display = prefix + "_" + v });
                }
            }

            entries.Sort((x, y) => string.CompareOrdinal(x.display, y.display));
            return entries;
        }

        // Reaper / Skeleton sets use Skeleton parts as their fallback instead
        // of the Naked / head_01 parts (the Naked meshes are humanoid).
        private static bool IsSkeletalSet(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return false;
            }
            string p = prefix.ToLowerInvariant();
            return p.Contains("skeleton") || p.Contains("reaper");
        }

        // Find a Skeleton fallback part in this category. Prefers the canonical
        // first index ("_01"), then any mesh whose name contains "skeleton".
        private int FindSkeletonIndex(SerializedProperty list)
        {
            int idx = FindIndex(list, info =>
                info.smr.name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) >= 0 &&
                StripClone(info.smr.name).EndsWith("_01", StringComparison.OrdinalIgnoreCase));
            if (idx >= 0)
            {
                return idx;
            }
            return FindIndex(list, info =>
                info.smr.name.IndexOf("skeleton", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        // Look up which part a given category WOULD pick for (prefix, variant)
        // without applying any cross-part constraints. Used to peek at the body
        // and legs before the main pick loop, so we can derive constraint tags
        // (e.g. _NG body -> require _S gauntlets) and apply them during picking.
        private PartInfo PeekSetPart(string prefix, string variant, string categoryType)
        {
            SerializedProperty cat = MeshesGetType(categoryType);
            if (cat == null)
            {
                return null;
            }
            SerializedProperty list = cat.FindPropertyRelative("list");
            int idx = IsEx1(prefix)
                ? PickIndexForSetEx1(list, prefix, variant, null, categoryType)
                : PickIndexForSet(list, prefix, variant, null);
            if (idx < 0)
            {
                return null;
            }
            return ParseName(list.GetArrayElementAtIndex(idx).objectReferenceValue as SkinnedMeshRenderer);
        }

        // Derive the required tag for a category from the CURRENT body / legs.
        // Used both by the cycle browser (to filter the < / > walk) and by
        // MeshesUpdateSpecials when the body / legs are themselves cycled.
        //   gauntlets -> "s" if body has _NG
        //   boots     -> "l" if legs has _LB
        private string ConstraintTagForCategory(string categoryType)
        {
            if (categoryType == PT_Create_Prefab.TypeOfMesh.gauntlets)
            {
                SkinnedMeshRenderer body = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.body));
                if (body != null && ParseName(body).HasTag("ng"))
                {
                    return "s";
                }
            }
            else if (categoryType == PT_Create_Prefab.TypeOfMesh.boots)
            {
                SkinnedMeshRenderer legs = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.legs));
                if (legs != null && ParseName(legs).HasTag("lb"))
                {
                    return "l";
                }
            }
            return null;
        }

        // Step from currentIdx by direction (+1 / -1), wrapping. If requireTag
        // is set, only stop on parts whose name carries that tag; returns -1
        // when no different part in the list qualifies.
        private int CycleIndex(SerializedProperty list, int currentIdx, int direction, string requireTag)
        {
            int n = list.arraySize;
            if (n == 0)
            {
                return -1;
            }
            if (string.IsNullOrEmpty(requireTag))
            {
                return (currentIdx + direction + n) % n;
            }

            int idx = currentIdx;
            for (int step = 0; step < n; step++)
            {
                idx = (idx + direction + n) % n;
                if (idx == currentIdx)
                {
                    return -1; // wrapped without finding another match
                }
                SkinnedMeshRenderer smr = list.GetArrayElementAtIndex(idx).objectReferenceValue as SkinnedMeshRenderer;
                if (smr != null && ParseName(smr).HasTag(requireTag))
                {
                    return idx;
                }
            }
            return -1;
        }

        // Make the parts that belong to (prefix, variant) visible, hide the rest.
        // Fallback chain (per missing part):
        //   - Skeleton / Reaper sets  -> Skeleton _01 fallback
        //   - head                    -> head_01 fallback
        //   - gauntlets / boots       -> Naked fallback
        private void ApplySet(string prefix, string variant)
        {
            if (string.IsNullOrEmpty(prefix))
            {
                return;
            }

            bool isSkeletal = IsSkeletalSet(prefix);
            bool isEx1 = IsEx1(prefix);
            _setCategories.Clear();

            // Reset the prefab name to the freshly applied set name. Any later
            // cycle/toggle from the user will append "_custom" via MarkCustomized.
            string newName = string.IsNullOrEmpty(variant) ? prefix : prefix + "_" + variant;
            if (lastSetName != null)
            {
                lastSetName.stringValue = newName;
            }
            if (prefabName != null)
            {
                prefabName.stringValue = newName;
            }

            // Peek the body and legs the set will actually pick, so cross-part
            // rules driven by their tags ( _NG / _NL / _NH on body, _LB on legs )
            // can be applied while picking the affected categories below — not
            // as a post-hoc correction. The peek walks the same variant chain
            // PickIndexForSet uses, so the result matches what the loop will
            // ultimately pick for body / legs themselves.
            PartInfo bodyPeek = PeekSetPart(prefix, variant, PT_Create_Prefab.TypeOfMesh.body);
            PartInfo legsPeek = PeekSetPart(prefix, variant, PT_Create_Prefab.TypeOfMesh.legs);

            bool skipHelmet     = bodyPeek != null && bodyPeek.HasTag("nh");
            bool skipLegs       = bodyPeek != null && bodyPeek.HasTag("nl");
            string gauntletsTag = (bodyPeek != null && bodyPeek.HasTag("ng")) ? "s" : null;
            string bootsTag     = (legsPeek != null && legsPeek.HasTag("lb")) ? "l" : null;

            for (int i = 0; i < meshes.arraySize; i++)
            {
                SerializedProperty category = meshes.GetArrayElementAtIndex(i);
                string type = category.FindPropertyRelative("type").stringValue;
                SerializedProperty list = category.FindPropertyRelative("list");
                SerializedProperty idx = category.FindPropertyRelative("idx");

                if (list.arraySize == 0)
                {
                    continue;
                }

                // Hide every mesh in this category first. Skip null entries
                // (lists can carry stale null refs when scene meshes were
                // deleted between reloads).
                for (int k = 0; k < list.arraySize; k++)
                {
                    SkinnedMeshRenderer m = list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer;
                    if (m != null)
                    {
                        m.enabled = false;
                    }
                }

                // Categories that body tags forbid get picked (so _setCategories
                // still records them and we can restore them when the user cycles
                // the body off the restrictive tag) but stay visually hidden.
                bool forbiddenByBody =
                    (type == PT_Create_Prefab.TypeOfMesh.helmet && skipHelmet) ||
                    (type == PT_Create_Prefab.TypeOfMesh.legs   && skipLegs);

                // Required tag from cross-part constraints (set-driven).
                string requireTag = null;
                if (type == PT_Create_Prefab.TypeOfMesh.gauntlets) requireTag = gauntletsTag;
                else if (type == PT_Create_Prefab.TypeOfMesh.boots) requireTag = bootsTag;

                int match;

                if (isEx1)
                {
                    // Ex1 expansion: the correspondent piece for this index, else a
                    // deterministic random Ex1 piece in this category. No Naked /
                    // Skeleton fallbacks -- the random fill covers gaps and never
                    // uses the Naked piece. Constraint tags (requireTag) are still
                    // honored when an Ex1 piece carries them.
                    match = PickIndexForSetEx1(list, prefix, variant, requireTag, type);

                    // Ex1 ships no head meshes of its own, and some Ex1 helmets
                    // don't fully cover the face, so every Ex1 set falls back to the
                    // gender-matched shared head (PT_Male_Armor_head_01 /
                    // PT_Female_Armor_head_01, chosen from the set's prefix).
                    if (match < 0 && type == PT_Create_Prefab.TypeOfMesh.head)
                    {
                        string headName =
                            prefix.IndexOf("Female", StringComparison.OrdinalIgnoreCase) >= 0
                                ? "PT_Female_Armor_head_01"
                                : "PT_Male_Armor_head_01";
                        match = FindIndex(list, info =>
                            StripClone(info.smr.name).Equals(headName, StringComparison.OrdinalIgnoreCase));
                    }
                }
                else
                {
                    match = PickIndexForSet(list, prefix, variant, requireTag);

                    // If a tag was required but no matching part exists in the set's
                    // variant chain, broaden to any part with that tag (e.g. any
                    // _S gauntlet at all, regardless of set).
                    if (match < 0 && !string.IsNullOrEmpty(requireTag))
                    {
                        match = FindIndex(list, info => info.HasTag(requireTag));
                    }

                    // Skeleton / Reaper fallback: any missing part uses the Skeleton _01 part.
                    if (match < 0 && isSkeletal)
                    {
                        match = FindSkeletonIndex(list);
                    }

                    // Naked sets borrow the gender-matched shared head and hair
                    // (e.g. PT_Male_Armor_head_01 / PT_Male_Armor_hair_01), since
                    // the naked group ships no head or hair of its own. Runs before
                    // the generic head_01 fallback so it can't grab the wrong gender.
                    if (match < 0 && NameIsNaked(prefix) &&
                        (type == PT_Create_Prefab.TypeOfMesh.head || type == PT_Create_Prefab.TypeOfMesh.hair))
                    {
                        string baseName = NakedBasePrefix(prefix) + "_" + type + "_01";
                        match = FindIndex(list, info =>
                            StripClone(info.smr.name).Equals(baseName, StringComparison.OrdinalIgnoreCase));
                    }

                    // head_01 fallback when the set has no head of its own.
                    if (match < 0 && type == PT_Create_Prefab.TypeOfMesh.head)
                    {
                        match = FindIndex(list, info => info.part == PT_Create_Prefab.TypeOfMesh.head &&
                                                        info.smr.name.ToLowerInvariant().Contains("head_01"));
                    }

                    // Naked fallback for missing gauntlets / boots (non-skeletal sets).
                    // Honors the constraint tag if one is required, so a _NG body
                    // never displays an unconstrained Naked gauntlet.
                    if (match < 0 &&
                        (type == PT_Create_Prefab.TypeOfMesh.gauntlets || type == PT_Create_Prefab.TypeOfMesh.boots))
                    {
                        if (!string.IsNullOrEmpty(requireTag))
                        {
                            match = FindIndex(list, info => NameIsNaked(info.smr.name) && info.HasTag(requireTag));
                        }
                        else
                        {
                            match = FindIndex(list, info => NameIsNaked(info.smr.name));
                        }
                    }
                }

                if (match >= 0)
                {
                    idx.intValue = match;
                    SkinnedMeshRenderer matched = list.GetArrayElementAtIndex(match).objectReferenceValue as SkinnedMeshRenderer;
                    if (matched != null)
                    {
                        matched.enabled = !forbiddenByBody;
                        _setCategories.Add(type);
                    }
                }
                // else: this set has no such part -> category stays hidden.
            }

            // Remember which categories the body rule hid, so the cycle cascade
            // in MeshesUpdateSpecials can restore them when the user cycles off.
            _bodyHidHelmet = skipHelmet && _setCategories.Contains(PT_Create_Prefab.TypeOfMesh.helmet);
            _bodyHidLegs   = skipLegs   && _setCategories.Contains(PT_Create_Prefab.TypeOfMesh.legs);

            ApplyHeadCoveringRules(skipHelmet);
            serializedObject.ApplyModifiedProperties();
        }

        // Force the given target category to a part tagged 'requiredTag', preferring
        // the chosen (prefix, variant) match, then any part with the tag. No-op if
        // the current selection already satisfies the tag, or no candidate exists.
        private void ForceTaggedPart(string targetType, string requiredTag, string prefix, string variant)
        {
            SerializedProperty cat = MeshesGetType(targetType);
            if (cat == null)
            {
                return;
            }

            SerializedProperty list = cat.FindPropertyRelative("list");
            if (list.arraySize == 0)
            {
                return;
            }

            SkinnedMeshRenderer current = CategoryCurrent(cat);
            if (current != null && ParseName(current).HasTag(requiredTag))
            {
                return; // already satisfied
            }

            int li = PickIndexForSet(list, prefix, variant, requiredTag);
            if (li < 0)
            {
                li = FindIndex(list, info => info.HasTag(requiredTag)); // any part with the tag
            }
            if (li < 0)
            {
                return;
            }

            for (int k = 0; k < list.arraySize; k++)
            {
                (list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer).enabled = false;
            }
            cat.FindPropertyRelative("idx").intValue = li;
            (list.GetArrayElementAtIndex(li).objectReferenceValue as SkinnedMeshRenderer).enabled = true;
        }

        // Hide every part in the given category.
        private void HideCategory(string targetType)
        {
            SerializedProperty cat = MeshesGetType(targetType);
            if (cat == null)
            {
                return;
            }
            SerializedProperty list = cat.FindPropertyRelative("list");
            for (int k = 0; k < list.arraySize; k++)
            {
                (list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer).enabled = false;
            }
        }

        // Helmet / hair / beard visibility rule, applied after a set is laid out.
        // Categories the set didn't populate (e.g. set has no beard) stay hidden;
        // we never inherit a fallback beard / helmet / cape from a previous set.
        // forceNoHelmet (from a _NH body) hides the helmet regardless of the set.
        private void ApplyHeadCoveringRules(bool forceNoHelmet = false)
        {
            SkinnedMeshRenderer helmet = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.helmet));
            SkinnedMeshRenderer hair = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.hair));
            SkinnedMeshRenderer beard = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.beard));

            bool setHasHelmet = _setCategories.Contains(PT_Create_Prefab.TypeOfMesh.helmet);
            bool setHasHair   = _setCategories.Contains(PT_Create_Prefab.TypeOfMesh.hair);
            bool setHasBeard  = _setCategories.Contains(PT_Create_Prefab.TypeOfMesh.beard);

            if (forceNoHelmet && helmet != null)
            {
                helmet.enabled = false;
            }

            bool helmetShown = setHasHelmet && helmet != null && helmet.enabled;

            if (helmetShown)
            {
                if (hair != null) hair.enabled = false;
                // Beard: only show if the set actually has one and the helmet is _NV.
                if (beard != null) beard.enabled = setHasBeard && NameIsNv(helmet.name);
            }
            else
            {
                if (helmet != null) helmet.enabled = false;
                // Hair / beard: only show if the set actually has them.
                if (hair != null) hair.enabled = setHasHair;
                if (beard != null) beard.enabled = setHasBeard;
            }
        }

        // Re-apply whatever set is currently selected (used on load).
        private void ApplySetByIndex()
        {
            if (setIndex == null || meshes == null)
            {
                return;
            }

            List<SetEntry> entries = BuildSetEntries();
            if (entries == null || entries.Count == 0)
            {
                return;
            }

            int current = setIndex.intValue;
            if (current < 0 || current >= entries.Count)
            {
                current = 0;
            }

            SetEntry chosen = entries[current];
            if (chosen == null || string.IsNullOrEmpty(chosen.prefix))
            {
                return;
            }

            setIndex.intValue = current;
            ApplySet(chosen.prefix, chosen.variant);
            SyncMaterialToSet();
        }

        // Index of a material within material.assets, or -1 if not present.
        private int FindAssetIndex(Material m)
        {
            if (m == null || material == null)
            {
                return -1;
            }
            SerializedProperty assets = material.FindPropertyRelative("assets");
            if (assets == null)
            {
                return -1;
            }
            for (int i = 0; i < assets.arraySize; i++)
            {
                if (assets.GetArrayElementAtIndex(i).objectReferenceValue == m)
                {
                    return i;
                }
            }
            return -1;
        }

        // After a set is applied, point the Material dropdown at the material that
        // set uses: the body part's material first, else the material used by the
        // most parts. Only materials already present in the dropdown list (assets)
        // are eligible; if none qualify the current selection is left untouched.
        private void SyncMaterialToSet()
        {
            if (material == null)
            {
                return;
            }
            SerializedProperty assets = material.FindPropertyRelative("assets");
            if (assets == null || assets.arraySize == 0)
            {
                return;
            }

            Material chosen = null;

            // 1) Prefer the body part's material.
            SkinnedMeshRenderer body = CategoryCurrent(MeshesGetType(PT_Create_Prefab.TypeOfMesh.body));
            if (body != null && body.enabled && FindAssetIndex(body.sharedMaterial) >= 0)
            {
                chosen = body.sharedMaterial;
            }

            // 2) Otherwise the material used by the most currently-shown parts.
            if (chosen == null)
            {
                Dictionary<Material, int> counts = new Dictionary<Material, int>();
                for (int i = 0; i < meshes.arraySize; i++)
                {
                    SkinnedMeshRenderer smr = CategoryCurrent(meshes.GetArrayElementAtIndex(i));
                    if (smr == null || !smr.enabled)
                    {
                        continue;
                    }
                    Material m = smr.sharedMaterial;
                    if (m == null || FindAssetIndex(m) < 0)
                    {
                        continue;
                    }
                    int c;
                    counts.TryGetValue(m, out c);
                    counts[m] = c + 1;
                }
                int best = 0;
                foreach (KeyValuePair<Material, int> kv in counts)
                {
                    if (kv.Value > best)
                    {
                        best = kv.Value;
                        chosen = kv.Key;
                    }
                }
            }

            if (chosen == null)
            {
                return;
            }
            int idx = FindAssetIndex(chosen);
            if (idx < 0)
            {
                return;
            }

            SerializedProperty indexProp = material.FindPropertyRelative("index");
            SerializedProperty instanceProp = material.FindPropertyRelative("instance");
            if (indexProp.intValue == idx && (instanceProp.objectReferenceValue as Material) == chosen)
            {
                return; // already selected
            }

            indexProp.intValue = idx;
            instanceProp.objectReferenceValue = chosen;
            serializedObject.ApplyModifiedProperties();
            DefaultMaterialProperties(); // refresh shader sliders to the set's material
        }

        // Group path for the set menu: "<pack>/<full display>". Master prefabs are
        // single-gender, so the menu groups only by pack (Base / Ex1 / Ex2 / ...)
        // and the top level is the pack list itself.
        private static string SetMenuPath(SetEntry e)
        {
            string prefix = e.prefix ?? "";

            string pack = "Base";
            foreach (string t in prefix.Split('_'))
            {
                if (t.Length > 2 &&
                    (t[0] == 'E' || t[0] == 'e') && (t[1] == 'x' || t[1] == 'X'))
                {
                    bool digits = true;
                    for (int c = 2; c < t.Length; c++)
                    {
                        if (!char.IsDigit(t[c])) { digits = false; break; }
                    }
                    if (digits) { pack = t; break; }
                }
            }
            return pack + "/" + e.display;
        }

        private void SetDrawControls()
        {
            if (setIndex == null)
            {
                EditorGUILayout.HelpBox("Set selection requires updating PT_Create_Prefab.cs to expose 'setIndex', 'prefabName', and 'lastSetName'.", MessageType.Warning);
                return;
            }

            List<SetEntry> entries = BuildSetEntries();
            if (entries.Count == 0)
            {
                EditorGUILayout.HelpBox("No armor sets detected. Sets are grouped by name prefix, e.g. PT_Female_Armor_01_A_body. Variant parts (_a / _b) are listed as separate sets.", MessageType.Info);
                return;
            }

            int current = setIndex.intValue;
            if (current < 0 || current >= entries.Count)
            {
                current = 0;
            }

            // Grouped dropdown: a flat list of every set across all packs is a tall
            // scroll, so sets are presented as submenus by pack.
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Armor Set");
            if (GUILayout.Button(entries[current].display, EditorStyles.popup))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < entries.Count; i++)
                {
                    int captured = i;
                    menu.AddItem(new GUIContent(SetMenuPath(entries[i])), i == current,
                        () => { _pendingSetSelection = captured; });
                }
                menu.ShowAsContext();
            }
            EditorGUILayout.EndHorizontal();

            if (_pendingSetSelection >= 0 && _pendingSetSelection < entries.Count)
            {
                int sel = _pendingSetSelection;
                _pendingSetSelection = -1;
                setIndex.intValue = sel;
                serializedObject.ApplyModifiedProperties();
                ApplySet(entries[sel].prefix, entries[sel].variant);
                SyncMaterialToSet();
            }
        }

        private void MeshesDrawControls()
        {
            for (int i = 0; i < meshes.arraySize; i++)
            {
                SerializedProperty list = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("list");
                SerializedProperty typeOfMeshes = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("type");
                SerializedProperty index = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("idx");

                // Don't display controlls for these type of meshes if the list is empty
                if (list.arraySize == 0)
                {
                    continue;
                }

                SkinnedMeshRenderer current = list.GetArrayElementAtIndex(index.intValue).objectReferenceValue as SkinnedMeshRenderer;
                bool currentEnabled = current != null && current.enabled;

                EditorGUILayout.BeginHorizontal();

                // Per-category visibility checkbox.
                bool toggled = EditorGUILayout.Toggle(currentEnabled, GUILayout.Width(18));
                if (toggled != currentEnabled && current != null)
                {
                    current.enabled = toggled;
                    MeshesUpdateSpecials(typeOfMeshes.stringValue);
                    MarkCustomized();
                }

                EditorGUILayout.LabelField(typeOfMeshes.stringValue.First().ToString().ToUpper() + typeOfMeshes.stringValue.Substring(1));

                if (GUILayout.Button("<"))
                {
                    string requireTag = ConstraintTagForCategory(typeOfMeshes.stringValue);
                    int newIdx = CycleIndex(list, index.intValue, -1, requireTag);

                    if (newIdx >= 0 && newIdx != index.intValue)
                    {
                        (list.GetArrayElementAtIndex(index.intValue).objectReferenceValue as SkinnedMeshRenderer).enabled = false;
                        index.intValue = newIdx;
                        (list.GetArrayElementAtIndex(newIdx).objectReferenceValue as SkinnedMeshRenderer).enabled = true;

                        MeshesUpdateSpecials(typeOfMeshes.stringValue);
                        MarkCustomized();
                    }

                    break;
                }
                if (GUILayout.Button(">"))
                {
                    string requireTag = ConstraintTagForCategory(typeOfMeshes.stringValue);
                    int newIdx = CycleIndex(list, index.intValue, +1, requireTag);

                    if (newIdx >= 0 && newIdx != index.intValue)
                    {
                        (list.GetArrayElementAtIndex(index.intValue).objectReferenceValue as SkinnedMeshRenderer).enabled = false;
                        index.intValue = newIdx;
                        (list.GetArrayElementAtIndex(newIdx).objectReferenceValue as SkinnedMeshRenderer).enabled = true;

                        MeshesUpdateSpecials(typeOfMeshes.stringValue);
                        MarkCustomized();
                    }

                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        private void PrefabDrawControls()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(loaded != null && loaded.boolValue ? "Reload All" : "Load"))
            {
                ReloadAll();
            }

            if (loaded != null && loaded.boolValue && GUILayout.Button("Reload Materials"))
            {
                ReloadMaterials();
                ReapplyMaterials();
            }

            EditorGUILayout.EndHorizontal();

            // Prefab name input. Auto-populated from the active set, gets a
            // "_custom" suffix as soon as the user diverges from the set layout,
            // and can be hand-edited any time.
            if (loaded != null && loaded.boolValue && prefabName != null)
            {
                EditorGUILayout.PropertyField(prefabName, new GUIContent("Prefab Name"));
            }

            EditorGUILayout.BeginHorizontal();

            if (loaded != null && loaded.boolValue && GUILayout.Button("Create Prefab"))
            {
                PrefabCreate();
            }

            if (loaded != null && loaded.boolValue && GUILayout.Button("Randomize"))
            {
                MeshesRandom();
            }

            GUIStyle style = new GUIStyle(GUI.skin.button);
            style.normal.textColor = Color.green;
            style.hover.textColor = Color.green;

            if (loaded != null && loaded.boolValue && !preview.boolValue &&GUILayout.Button("Preview"))
            {
                preview.boolValue = true;
            }

            if (loaded != null && loaded.boolValue && preview.boolValue && GUILayout.Button("Preview", style))
            {
                preview.boolValue = false;
            }

            if (loaded != null && loaded.boolValue && !duplicateMaterial.boolValue && GUILayout.Button("Duplicate Material"))
            {
                duplicateMaterial.boolValue = true;
            }

            if (loaded != null && loaded.boolValue && duplicateMaterial.boolValue && GUILayout.Button("Duplicate Material", style))
            {
                duplicateMaterial.boolValue = false;
            }

            EditorGUILayout.EndHorizontal();
        }

        private void PrefabCreate()
        {
            GameObject instance = Instantiate(script.gameObject);

            // Name the new instance from the Prefab Name field, falling back to
            // the source object's name (sans "(Clone)" suffix) when empty.
            string desired = prefabName != null ? prefabName.stringValue : null;
            instance.name = !string.IsNullOrEmpty(desired) ? desired : script.gameObject.name;

            SkinnedMeshRenderer[] smrs = instance.GetComponentsInChildren<SkinnedMeshRenderer>();
            Material mat;

            if (null == material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue)
            {
                EditorUtility.DisplayDialog("Internal Error",
                                "Link to the materials lost. Please Reload All or Reload Materials!",
                                "Ok");
                return;
            }

            if (duplicateMaterial.boolValue)
            {
                mat = new Material(smrs[0].sharedMaterial);

                string path = AssetDatabase.GetAssetPath(material.FindPropertyRelative("assets").GetArrayElementAtIndex(material.FindPropertyRelative("index").intValue).objectReferenceValue);
                string name = Path.GetFileNameWithoutExtension(path);
                path = Path.GetDirectoryName(path);
                path = path.Replace('\\', '/');
                int count = GetAssetsAtPath<Material>(path).Length;
                string final = path + "/" + name + "_" + count.ToString("D2") + ".mat";
                AssetDatabase.CreateAsset(mat, final);
            }
            else
            {
                SerializedProperty assets = material.FindPropertyRelative("assets");
                SerializedProperty index = material.FindPropertyRelative("index");
                mat = assets.GetArrayElementAtIndex(index.intValue).objectReferenceValue as Material;
            }

            
            DestroyImmediate(instance.GetComponent<PT_Create_Prefab>());
            foreach (SkinnedMeshRenderer smr in smrs)
            {
                if (!smr.enabled)
                {
                    DestroyImmediate(smr.gameObject);
                }
                else
                {
                    smr.sharedMaterial = mat;
                }
            }
        }

        private void ReloadMaterials()
        {
            SerializedProperty assets = material.FindPropertyRelative("assets");
            SerializedProperty index = material.FindPropertyRelative("index");
            assets.arraySize = 0;
            index.intValue = 0;

            // Get the material straight from the prefab's material.
            Material dm = PrefabUtility.GetCorrespondingObjectFromSource(script.gameObject).GetComponentInChildren<SkinnedMeshRenderer>().sharedMaterial;
            string path = AssetDatabase.GetAssetPath(dm);
            Debug.Log("Default material is: " + dm.name);
            string name = Path.GetFileNameWithoutExtension(path);
            path = Path.GetDirectoryName(path);
            path = path.Replace('\\', '/');
            Material[] materials = GetAssetsAtPath<Material>(path);

            foreach (Material m in materials)
            {
                assets.arraySize += 1;
                assets.GetArrayElementAtIndex(assets.arraySize - 1).objectReferenceValue = m;
                serializedObject.ApplyModifiedProperties();
            }

            // Select the material the prefab actually uses and reference it
            // DIRECTLY (no copy), so editing a slider edits the real asset and
            // shows on the parts using it -- and so the dropdown opens on it.
            int dmIndex = 0;
            for (int mi = 0; mi < assets.arraySize; mi++)
            {
                if (assets.GetArrayElementAtIndex(mi).objectReferenceValue == dm)
                {
                    dmIndex = mi;
                    break;
                }
            }
            index.intValue = dmIndex;
            material.FindPropertyRelative("instance").objectReferenceValue =
                (assets.arraySize > 0)
                    ? assets.GetArrayElementAtIndex(dmIndex).objectReferenceValue as Material
                    : dm;
            serializedObject.ApplyModifiedProperties();
        }

        // True when the rig references more than one distinct material across
        // its parts (e.g. a master prefab with per-part materials). Used to stop
        // ReapplyMaterials from flattening an intentional multi-material setup.
        private bool HasMultipleDistinctMaterials()
        {
            Material first = null;
            bool seenOne = false;
            for (int i = 0; i < meshes.arraySize; i++)
            {
                SerializedProperty list = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("list");
                for (int k = 0; k < list.arraySize; k++)
                {
                    SkinnedMeshRenderer smr = list.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer;
                    if (smr == null) { continue; }
                    Material m = smr.sharedMaterial;
                    if (m == null) { continue; }
                    if (!seenOne) { first = m; seenOne = true; }
                    else if (m != first) { return true; }
                }
            }
            return false;
        }

        private void ReapplyMaterials()
        {
            // Never flatten a rig that intentionally uses different materials per
            // part. Reassigning them all to the single instance would wipe the
            // per-part setup (this is what corrupted the master prefab).
            if (HasMultipleDistinctMaterials())
            {
                return;
            }

            for (int i = 0; i < meshes.arraySize; i++)
            {
                SerializedProperty listOfMeshes = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("list");

                // Update the material for every mesh
                for (int k = 0; k < listOfMeshes.arraySize; k++)
                {
                    (listOfMeshes.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer).sharedMaterial =
                        material.FindPropertyRelative("instance").objectReferenceValue as Material;
                }
            }
            serializedObject.ApplyModifiedProperties();
            DefaultMaterialProperties();
        }

        private void ReloadMeshes()
        {
            SkinnedMeshRenderer[] allmeshes = script.GetComponentsInChildren<SkinnedMeshRenderer>();

            for (int i = 0; i < meshes.arraySize; i++)
            {
                string typeOfMesh = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("type").stringValue;
                SerializedProperty listOfMeshes = meshes.GetArrayElementAtIndex(i).FindPropertyRelative("list");

                // Clear the list (skip null entries left behind by deleted scene meshes)
                for (int k = 0; k < listOfMeshes.arraySize; k++)
                {
                    SkinnedMeshRenderer m = listOfMeshes.GetArrayElementAtIndex(k).objectReferenceValue as SkinnedMeshRenderer;
                    if (m != null)
                    {
                        m.enabled = false;
                    }
                }

                listOfMeshes.arraySize = 0;
                serializedObject.ApplyModifiedProperties();
                // Reset the display index for this type of mesh
                meshes.GetArrayElementAtIndex(i).FindPropertyRelative("idx").intValue = 0;

                foreach (SkinnedMeshRenderer mesh in allmeshes)
                {
                    if (mesh.name.Contains(typeOfMesh))
                    {
                        listOfMeshes.arraySize += 1;
                        listOfMeshes.GetArrayElementAtIndex(listOfMeshes.arraySize - 1).objectReferenceValue = mesh;
                        mesh.enabled = false;
                        serializedObject.ApplyModifiedProperties();
                    }
                }

                Debug.Log("Type of mesh: " + typeOfMesh);
                if (listOfMeshes.arraySize > 0 &&
                    typeOfMesh != PT_Create_Prefab.TypeOfMesh.helmet) // Do not display at startup the helmet
                {
                    (listOfMeshes.GetArrayElementAtIndex(0).objectReferenceValue as SkinnedMeshRenderer).enabled = true;
                    Debug.Log("enable 0 for mesh: " + typeOfMesh);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        private void ReloadAll()
        {
            ReloadMaterials();
            ReapplyMaterials();
            ReloadMeshes();
            ApplySetByIndex();
            loaded.boolValue = true;
            serializedObject.ApplyModifiedProperties();
        }

        public override void OnInspectorGUI()
        {
            EditorGUI.BeginChangeCheck();

            if (!loaded.boolValue)
            {
                ReloadAll();
            }

            EditorGUILayout.LabelField("Meshes", EditorStyles.boldLabel);
            MeshesDrawControls();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Set", EditorStyles.boldLabel);
            SetDrawControls();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Prefab", EditorStyles.boldLabel);
            PrefabDrawControls();


            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Expansion", EditorStyles.boldLabel);

            ExpansionDragAndDrop();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shader", EditorStyles.boldLabel);
            
            // User may delete a material
            try
            {
                ShaderDropDownSelection();
            }
            catch
            {
                ReloadMaterials();
            }

            EditorGUILayout.Space();
            ShaderProperty_CS("skin", "Skin");
            ShaderProperty_CS("eyes", "Eyes");
            ShaderProperty_CS("hair", "Hair");
            ShaderProperty_CS("sclera", "Sclera");
            ShaderProperty_CS("lips", "Lips");
            ShaderProperty_CS("scars", "Scars");
            EditorGUILayout.Space();
            ShaderProperty_CSM("metal1", "Metal1");
            ShaderProperty_CSM("metal2", "Metal2");
            ShaderProperty_CSM("metal3", "Metal3");
            EditorGUILayout.Space();
            ShaderProperty_CS("leather1", "Leather1");
            ShaderProperty_CS("leather2", "Leather2");
            ShaderProperty_CS("leather3", "Leather3");
            EditorGUILayout.Space();
            ShaderProperty_C("cloth1", "Cloth1");
            ShaderProperty_C("cloth2", "Cloth2");
            ShaderProperty_C("cloth3", "Cloth3");
            EditorGUILayout.Space();
            ShaderProperty_CS("gems1", "Gems1");
            ShaderProperty_CS("gems2", "Gems2");
            ShaderProperty_CS("gems3", "Gems3");
            EditorGUILayout.Space();
            ShaderProperty_C("feathers1", "Feathers1");
            ShaderProperty_C("feathers2", "Feathers2");
            ShaderProperty_C("feathers3", "Feathers3");
            EditorGUILayout.Space();
            ShaderProperty_CI("coatofarms", "Coat of arms");
            EditorGUILayout.Space();
            ShaderProperty_CSP("light1", "Light1");
            ShaderProperty_CSP("light2", "Light3");
            ShaderProperty_CSP("light3", "Light3");
            ShaderProperty_UTL();


            if (EditorGUI.EndChangeCheck())
                serializedObject.ApplyModifiedProperties();

        }

        private T[] GetAssetsAtPath<T>(string path)
        {
            ArrayList list = new ArrayList();
            string[] files = Directory.GetFiles(Directory.GetParent(Application.dataPath) + "/" + path);

            foreach (string file in files)
            {
                UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path + "/" + Path.GetFileName(file), typeof(T));
                if (obj != null)
                {
                    list.Add(obj);
                }
            }

            T[] result = new T[list.Count];

            for (int i = 0; i < list.Count; i++)
            {
                result[i] = (T)list[i];
            }

            return result;
        }
    }
}
