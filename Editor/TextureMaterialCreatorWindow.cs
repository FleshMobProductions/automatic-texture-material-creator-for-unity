using System.IO;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Text;
using System.Collections.Generic;

/*Copyright (c) 2019, Simon Eicher <fleshmobproductions@gmail.com>

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without restriction,
including without limitation the rights to use, copy, modify, merge, publish, distribute, sublicense,
and/or sell copies of the Software, and to permit persons to whom the Software is furnished to do so, 
subject to the following conditions: 

The above copyright notice and this permission notice shall be included in all copies or substantial 
portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT
LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace FMPUtils.Editor
{

    public class TextureMaterialCreatorWindow : EditorWindow
    {
        private const string normalMapDefaultPropertyName = "_BumpMap";
        private const string abledoMapDefaultPropertyName = "_MainTex";

        private readonly string[] normalMapSuffixes = new string[] { "_n", "_normal" };

        private struct MapSet
        {
            public Texture2D AlbedoMap { get; private set; }
            public string AlbedoMapAssetPath { get; private set; }
            public Texture2D NormalMap { get; private set; }

            public MapSet(Texture2D albedoMap, Texture2D normalMap, string albedoMapAssetPath)
            {
                this.AlbedoMap = albedoMap;
                this.NormalMap = normalMap;
                this.AlbedoMapAssetPath = albedoMapAssetPath;
            }
        }

        private struct AssetNameDetails
        {
            public string AssetPath { get; private set; }
            public string AssetName { get; private set; }
            public string AssetNameLowerCase { get; private set; }
            public string AssetNameNoFileTypeLowerCase { get; private set; }

            public AssetNameDetails(string assetPath, string assetName, string assetNameLowerCase, string assetNameNoFileTypeLowerCase)
            {
                this.AssetPath = assetPath;
                this.AssetName = assetName;
                this.AssetNameLowerCase = assetNameLowerCase;
                this.AssetNameNoFileTypeLowerCase = assetNameNoFileTypeLowerCase;
            }
        }

        private string textureSrcFolderPath;
        private string outputFolderPath;
        private Material baseMaterial;
        private Shader shader;
        private string materialPrefix = "";
        private string materialSuffix = "";
        private bool overrideAlphaIsTransparency;
        private bool alphaIsTransparencyNew;
        private bool overrideTexturePropertyName;
        private bool overrideNormalMapPropertyName;
        private string materialTexturePropertyName = "";
        private bool overrideExistingMaterials;
        private bool includeNormalMap;
        private string normalMapPropertyName = "";

        [MenuItem("FMPUtils/Texture Material Creator")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(TextureMaterialCreatorWindow));
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("General Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            materialPrefix = EditorGUILayout.TextField("Material Prefix", materialPrefix);
            materialSuffix = EditorGUILayout.TextField("Material Suffix", materialSuffix);
            baseMaterial = EditorGUILayout.ObjectField("Template Material", baseMaterial, typeof(Material), false) as Material;
            shader = EditorGUILayout.ObjectField("Shader Override", shader, typeof(Shader), false) as Shader;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Overwrite existing materials?");
            overrideExistingMaterials = EditorGUILayout.Toggle(overrideExistingMaterials);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Albedo texture \"Alpha is Transparency\" Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            overrideAlphaIsTransparency = EditorGUILayout.BeginToggleGroup("Override alphaIsTransparency", overrideAlphaIsTransparency);
            alphaIsTransparencyNew = EditorGUILayout.Toggle("alphaIsTransparency new value", alphaIsTransparencyNew);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Material Albedo Map Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            overrideTexturePropertyName = EditorGUILayout.BeginToggleGroup("Use custom material albedo property", overrideTexturePropertyName);
            materialTexturePropertyName = EditorGUILayout.TextField("Shader Texture Property Name", materialTexturePropertyName);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Material Normal Map Settings", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            includeNormalMap = EditorGUILayout.BeginToggleGroup("Include normal maps (need to end with \"_n\" or \"_normal\")", includeNormalMap);
            // Normal maps need to start with the regular texture name and end with "_n" or "_normal" to qualify as normal maps
            // The normal maps will also automatically have their import settings changed to normal maps if necessary
            // According to the documentation at https://docs.unity3d.com/ScriptReference/Material.SetTexture.html the default name 
            // property for normal maps in Unity's built in shaders is "_BumpMap" (and "_MainTex" for the main texture/albedo)
            overrideNormalMapPropertyName = EditorGUILayout.BeginToggleGroup("Use custom material normal map property", overrideNormalMapPropertyName);
            normalMapPropertyName = EditorGUILayout.TextField("Normal Map Property", normalMapPropertyName);
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.EndToggleGroup();
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Folder Selection", EditorStyles.boldLabel);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(string.Format("Input folder: {0}", (!string.IsNullOrEmpty(textureSrcFolderPath) ? textureSrcFolderPath : "[not assigned]")));
            EditorGUILayout.LabelField(string.Format("Output folder: {0}", (!string.IsNullOrEmpty(outputFolderPath) ? outputFolderPath : "[not assigned]")));

            if (GUILayout.Button("Use folder of selected asset as input"))
            {
                EditorHelpUtilities.AssignFolderOfSelectedObject(ref textureSrcFolderPath);
            }
            if (GUILayout.Button("Use folder of selected asset as output"))
            {
                EditorHelpUtilities.AssignFolderOfSelectedObject(ref outputFolderPath);
            }
            if (GUILayout.Button("Select Texture Input Folder"))
            {
                string folderPathTemp = null;
                if (EditorHelpUtilities.TryGetFolderSelection(out folderPathTemp, "Select folder with Textures"))
                {
                    textureSrcFolderPath = folderPathTemp;
                }
            }
            if (GUILayout.Button("Select Material Output Folder"))
            {
                string folderPathTemp = null;
                if (EditorHelpUtilities.TryGetFolderSelection(out folderPathTemp, "Select folder to store Materials to"))
                {
                    outputFolderPath = folderPathTemp;
                }
            }
            EditorGUILayout.Space();

            EditorGUILayout.LabelField("Asset Generation", EditorStyles.boldLabel);
            if (GUILayout.Button("Create Materials"))
            {
                CreateMaterials();
            }
        }

        private void CreateMaterials()
        {
            // Information for using Directory.Exists: 
            // The path parameter is permitted to specify relative or absolute path information. Relative path information is interpreted as relative to the current working directory.
            Debug.Log(string.Format("CreateMaterials: InputFolder Path passed {0}, output folder path {1}", textureSrcFolderPath, outputFolderPath));
            if (!Directory.Exists(textureSrcFolderPath))
            {
                EditorHelpUtilities.DisplaySimpleDialog("Invalid folder", string.Format("Input folder path {0} is not valid", textureSrcFolderPath));
                return;
            }
            if (!Directory.Exists(outputFolderPath))
            {
                EditorHelpUtilities.DisplaySimpleDialog("Invalid folder", string.Format("Output folder path {0} is not valid", outputFolderPath));
                return;
            }
            string inputFolderAssetsRelative = null;
            if (!EditorHelpUtilities.TryGetAssetsLocalPathForExistingFolder(textureSrcFolderPath, out inputFolderAssetsRelative))
            {
                EditorHelpUtilities.DisplaySimpleDialog("Invalid folder", string.Format("Input folder path {0} exists but was found to not belong into this project", textureSrcFolderPath));
                return;
            }
            string outputFolderAssetsRelative = null;
            if (!EditorHelpUtilities.TryGetAssetsLocalPathForExistingFolder(outputFolderPath, out outputFolderAssetsRelative))
            {
                EditorHelpUtilities.DisplaySimpleDialog("Invalid folder", string.Format("Input folder path {0} exists but was found to not belong into this project", outputFolderPath));
                return;
            }

            var baseMaterialPrev = baseMaterial;
            var shaderPrev = shader;

            if (baseMaterial == null)
            {
                Shader initShader = shader != null ? shader : Shader.Find("Standard");
                baseMaterial = new Material(initShader);
            }
            else if (shader != null)
            {
                baseMaterial.shader = shader;
            }

            Debug.Log(string.Format("Output folder path: {0}", outputFolderPath));
            string outputPathAssetsRelativeTrimmed = outputFolderAssetsRelative.TrimEnd('/').TrimEnd('\\');

            string[] assetGUIDs = AssetDatabase.FindAssets("t:texture2D", new string[] { inputFolderAssetsRelative });

            if (includeNormalMap)
            {
                BuildMaterialsWithNormalMaps(assetGUIDs, baseMaterial, outputPathAssetsRelativeTrimmed);
            }
            else
            {
                BuildMaterialsWithoutNormalMaps(assetGUIDs, baseMaterial, outputPathAssetsRelativeTrimmed);
            }
            shader = shaderPrev;
            // If we created a material only for this purpose, destroy it again
            if (baseMaterial != null && baseMaterialPrev == null)
            {
                GameObject.DestroyImmediate(baseMaterial);
            }
            baseMaterial = baseMaterialPrev;
            // Save new alphaIsTransparency setting for asset loaders if neccessary
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private string GetSanitizedMaterialFileName(string textureName)
        {
            return EditorHelpUtilities.GetSanetizedFileName(materialPrefix).Replace("/", "") + textureName + EditorHelpUtilities.GetSanetizedFileName(materialSuffix).Replace("/", "") + ".mat";
        }

        private void BuildMaterialsWithoutNormalMaps(string[] assetGUIDs, Material materialTemplate, string outputPathAssetsRelativeTrimmed)
        {
            foreach (var guid in assetGUIDs)
            {
                try
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                    Texture2D albedoTexture = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Texture2D)) as Texture2D;
                    if (albedoTexture != null)
                    {
                        if (overrideAlphaIsTransparency)
                        {
                            albedoTexture.alphaIsTransparency = alphaIsTransparencyNew;
                            TextureImporter textureImporter = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                            if (textureImporter != null && textureImporter.alphaIsTransparency != alphaIsTransparencyNew)
                            {
                                textureImporter.alphaIsTransparency = alphaIsTransparencyNew;
                            }
                        }
                        Material currentMaterial = new Material(baseMaterial); // copies all properties
                        SetAlbedoMapInMaterial(currentMaterial, albedoTexture);

                        string textureName = albedoTexture.name;
                        string fileName = GetSanitizedMaterialFileName(textureName);
                        string fullSavePath = outputPathAssetsRelativeTrimmed + "/" + fileName;

                        if (!overrideExistingMaterials && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fullSavePath)))
                        {
                            // if we don't allow material overwriting and there is an asset at our path (AssetDatabase.AssetPathToGUID not returning null) 
                            // just omit the asset creation
                            Destroy(currentMaterial);
                            continue;
                        }
                        // CreateAsset remark: If an asset already exists at path it will be deleted prior to creating a new asset. 
                        AssetDatabase.CreateAsset(currentMaterial, fullSavePath);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError(string.Format("Something has gone wrong while processing asset at path {0}: {1}", AssetDatabase.GUIDToAssetPath(guid), e));
                }
            }
        }

        private void BuildMaterialsWithNormalMaps(string[] assetGUIDs, Material materialTemplate, string outputPathAssetsRelativeTrimmed)
        {
            // Assume a list size of half the assetGUIDs length, but add a small buffer in case not all textures have proper normals for them
            List<MapSet> textureSets = new List<MapSet>((assetGUIDs.Length + 2) / 2);
            List<AssetNameDetails> assetNameDetails = new List<AssetNameDetails>(assetGUIDs.Length);
            bool wereTextureImportSettingsEdited = false;

            foreach (string guid in assetGUIDs)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                string assetName = EditorHelpUtilities.GetAssetNameFromPath(assetPath);
                string assetNameLowerCase = assetName.ToLower();
                int assetNameLastDotIndex = assetName.LastIndexOf('.');
                string assetNameNoFileTypeLowerCase = assetNameLowerCase.Substring(0, assetNameLastDotIndex);
                AssetNameDetails nameDetails = new AssetNameDetails(assetPath, assetName, assetNameLowerCase, assetNameNoFileTypeLowerCase);
                assetNameDetails.Add(nameDetails);
            }

            foreach (AssetNameDetails nameDetails in assetNameDetails)
            {
                bool isNormalMap = false;
                foreach (string normalSuffix in normalMapSuffixes)
                {
                    if (nameDetails.AssetNameNoFileTypeLowerCase.EndsWith(normalSuffix))
                    {
                        // Found normal map, continue search for albedo maps only
                        isNormalMap = true;
                        continue;
                    }
                }
                if (isNormalMap)
                {
                    continue;
                }
                Texture2D albedoTexture = AssetDatabase.LoadAssetAtPath(nameDetails.AssetPath, typeof(Texture2D)) as Texture2D;
                if (albedoTexture == null)
                {
                    continue;
                }
                if (overrideAlphaIsTransparency)
                {
                    albedoTexture.alphaIsTransparency = alphaIsTransparencyNew;
                    TextureImporter textureImporter = AssetImporter.GetAtPath(nameDetails.AssetPath) as TextureImporter;
                    if (textureImporter.alphaIsTransparency != alphaIsTransparencyNew)
                    {
                        wereTextureImportSettingsEdited = true;
                        textureImporter.alphaIsTransparency = alphaIsTransparencyNew;
                    }
                }
                Texture2D normalTexture = null;
                string normalMapAssetPath = FindNormalMapAssetPathForTextureName(assetNameDetails, nameDetails.AssetNameNoFileTypeLowerCase);
                if (normalMapAssetPath != null)
                {
                    TextureImporter normalTextureImporter = AssetImporter.GetAtPath(normalMapAssetPath) as TextureImporter;
                    if (normalTextureImporter.textureType != TextureImporterType.NormalMap)
                    {
                        normalTextureImporter.textureType = TextureImporterType.NormalMap;
                        wereTextureImportSettingsEdited = true;

                    }
                    normalTexture = AssetDatabase.LoadAssetAtPath(normalMapAssetPath, typeof(Texture2D)) as Texture2D;
                }
                MapSet textureCombination = new MapSet(albedoTexture, normalTexture, nameDetails.AssetPath);
                textureSets.Add(textureCombination);
            }
            // Apply the changes to the normal map texture settings to the project, if there were any: 
            if (wereTextureImportSettingsEdited)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            foreach (MapSet textureSet in textureSets)
            {
                try
                {
                    Material currentMaterial = new Material(baseMaterial); // copies all properties
                    SetAlbedoMapInMaterial(currentMaterial, textureSet.AlbedoMap);
                    SetNormalMapInMaterial(currentMaterial, textureSet.NormalMap);

                    string textureName = textureSet.AlbedoMap.name;
                    string fileName = GetSanitizedMaterialFileName(textureName);
                    string fullSavePath = outputPathAssetsRelativeTrimmed + "/" + fileName;

                    if (!overrideExistingMaterials && !string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(fullSavePath)))
                    {
                        // if we don't allow material overwriting and there is an asset at our path (AssetDatabase.AssetPathToGUID not returning null) 
                        // just omit the asset creation
                        Destroy(currentMaterial);
                        continue;
                    }
                    // CreateAsset remark: If an asset already exists at path it will be deleted prior to creating a new asset. 
                    AssetDatabase.CreateAsset(currentMaterial, fullSavePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError(string.Format("Something has gone wrong while processing texture set with albedo map at path {0}: {1}", textureSet.AlbedoMapAssetPath, e));
                }
            }
        }

        private void SetAlbedoMapInMaterial(Material material, Texture2D albedoTexture)
        {
            if (overrideTexturePropertyName)
            {
                if (material.HasProperty(materialTexturePropertyName))
                {
                    material.SetTexture(materialTexturePropertyName, albedoTexture);
                }
                else
                {
                    Debug.LogError(string.Format("Override Texture Property Name: material does not have a property called {0}. Default albedo property name is used (\"{1}\")", materialTexturePropertyName, abledoMapDefaultPropertyName));
                    material.mainTexture = albedoTexture;
                }
            }
            else
            {
                material.mainTexture = albedoTexture;
            }
        }

        private void SetNormalMapInMaterial(Material material, Texture2D normalTexture)
        {
            if (overrideNormalMapPropertyName)
            {
                if (material.HasProperty(normalMapPropertyName))
                {
                    material.SetTexture(normalMapPropertyName, normalTexture);
                    return;
                }
                else
                {
                    Debug.LogError(string.Format("Override Normal Map Property: material does not have a property called {0}. Default normal map property name is tried to be used (\"{1}\")", normalMapPropertyName, normalMapDefaultPropertyName));
                }
            }
            else
            {
                if (material.HasProperty(normalMapDefaultPropertyName))
                {
                    material.SetTexture(normalMapDefaultPropertyName, normalTexture);
                }
                else
                {
                    Debug.LogError(string.Format("Material does not have a standard property for normal maps (\"{0}\"). No normal texture will be assigned", normalMapDefaultPropertyName));
                }
            }
        }

        private string FindNormalMapAssetPathForTextureName(List<AssetNameDetails> assetNameDetails, string textureNameNoFileTypeLowerCase)
        {
            return FindMapAssetPathForTextureNameAndPossibleNameEndings(assetNameDetails, textureNameNoFileTypeLowerCase, normalMapSuffixes);
        }

        private string FindMapAssetPathForTextureNameAndPossibleNameEndings(List<AssetNameDetails> assetNameDetails, string textureNameNoFileTypeLowerCase, string[] nameEndings)
        {
            foreach (AssetNameDetails nameDetails in assetNameDetails)
            {
                if (nameDetails.AssetNameNoFileTypeLowerCase.StartsWith(textureNameNoFileTypeLowerCase))
                {
                    foreach (string suffix in nameEndings)
                    {
                        if (nameDetails.AssetNameNoFileTypeLowerCase.EndsWith(suffix))
                        {
                            return nameDetails.AssetPath;
                        }
                    }
                }
            }
            return null;
        }
    }
}
