using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.IO;

namespace FMPUtils.Editor
{

    public class EditorHelpUtilities
    {
        public static string GetSanetizedFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return string.Empty;
            }
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder("");
            foreach (char c in fileName)
            {
                if (System.Array.IndexOf(invalidChars, c) < 0)
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets assetLocalPath to folderPathFull is already a valid local project folder path. 
        /// Otherwise checks if the passed string folederPathFull starts with Application.dataPath. If that is not the case, 
        /// assumes that the root Assets folder of the project "Assets/" is the last Assets folder string in the supplied folderPathFull
        /// Already existing asset relative paths start with "Assets/", so we account for that
        /// stores the path result into assetLocalPath
        /// </summary>
        /// <returns>true if a valid asset folder local path was found, otherwise false</returns>
        public static bool TryGetAssetsLocalPathForExistingFolder(string folderPathFull, out string assetLocalPath)
        {
            if (string.IsNullOrEmpty(folderPathFull))
            {
                assetLocalPath = string.Empty;
                return false;
            }

            folderPathFull = folderPathFull.Trim().Trim('/'); // remove white space chars
                                                              // We also need to trim the last / of the path to be recognized as valid
            if (AssetDatabase.IsValidFolder(folderPathFull))
            {
                assetLocalPath = folderPathFull;
                return true;
            }
            int startIndex = 0;
            // Application.dataPath: Platform dependend - but for Unity Editor: <path to project folder>/Assets
            // If we have an absolute path, the path must start with Application.dataPath
            string dataPath = Application.dataPath;
            if (folderPathFull.StartsWith(dataPath))
            {
                int charactersToStrip = "Assets".Length;
                startIndex = dataPath.Length - charactersToStrip;
            }
            else
            {
                int assetsIndex = folderPathFull.LastIndexOf("Assets/");
                if (assetsIndex == -1)
                {
                    assetsIndex = folderPathFull.LastIndexOf(@"Assets\");
                }
                if (assetsIndex == -1)
                {
                    assetLocalPath = string.Empty;
                    return false;
                }
                startIndex = assetsIndex;
            }
            int charLength = folderPathFull.Length - startIndex;
            assetLocalPath = folderPathFull.Substring(startIndex, charLength);
            Debug.Log(string.Format("TryGetAssetsLocalPathForExistingFolder: asset local path {0}, is valid: {1}", assetLocalPath, AssetDatabase.IsValidFolder(assetLocalPath)));
            return (AssetDatabase.IsValidFolder(assetLocalPath));
        }

        public static bool TryGetAbsolutePathForAssetsLocalPath(string assetLocalPath, out string folderPathFull)
        {
            folderPathFull = null;
            if (string.IsNullOrEmpty(assetLocalPath))
            {
                Debug.LogError("TryGetAbsolutePathForAssetsLocalPath: passed assetLocalPath is empty");
                return false;
            }

            if (Directory.Exists(assetLocalPath))
            {
                folderPathFull = assetLocalPath;
                return true;
            }

            string assetLocalPathTrimmed = assetLocalPath.Trim().Trim('/');
            string assetLocalFolderPath = assetLocalPathTrimmed;
            // Make sure that the folder of the asset is already a valid path in the assets folder
            if (!AssetDatabase.IsValidFolder(assetLocalFolderPath))
            {
                assetLocalFolderPath = assetLocalPathTrimmed.Substring(0, assetLocalPathTrimmed.LastIndexOf('/'));
                if (!AssetDatabase.IsValidFolder(assetLocalFolderPath))
                {
                    Debug.LogError("TryGetAbsolutePathForAssetsLocalPath: assetLocalPath was not recognized as valid path in the project");
                    return false;
                } 
            }
            // returns path/Assets in the unity editor
            // Valid AssetDatabase paths start with Assets - so we need to filter the "Asset" folder level out on one path
            string projectPath = Application.dataPath;
            int charLenToRemove = "Assets/".Length;
            string assetLocalPathTrimmedNoAssetPrefix = assetLocalPath.Substring(charLenToRemove, assetLocalPath.Length - charLenToRemove);
            folderPathFull = projectPath + "/" + assetLocalPathTrimmedNoAssetPrefix;
            Debug.Log(string.Format("TryGetAbsolutePathForAssetsLocalPath result path: {0}", folderPathFull));
            return true;
        } 

        public static bool TryGetFolderSelection(out string resultPath, string dialogueTitle)
        {
            resultPath = EditorUtility.OpenFolderPanel(dialogueTitle, "", "");
            if (resultPath.Length != 0 && Directory.Exists(resultPath))
            {
                return true;
            }
            return false;
        }

        public static void DisplaySimpleDialog(string title, string text)
        {
            EditorUtility.DisplayDialog(title, text, "Ok");
        }

        public static bool DisplayConfirmDialog(string title, string text, string okText = "Proceed", string cancelText = "Cancel")
        {
            return EditorUtility.DisplayDialog(title, text, okText, cancelText);
        }

        public static void AssignFolderOfSelectedObject(ref string output)
        {
            var activeObj = Selection.activeObject;
            if (activeObj != null)
            {
                // AssetDatabase.GetAssetPath: All paths are relative to the project folder, for example: "Assets/MyTextures/hello.png".
                string folderPathTemp = AssetDatabase.GetAssetPath(activeObj);
                // Directory.Exists would work for a relative or absolute path, 
                // but we assume that the selected object it a specific file since 
                // I don't know if Selection.activeObject would return a folder asset
                int lastFolderSeperationCharIndex = folderPathTemp.LastIndexOf('/');
                output = folderPathTemp.Substring(0, lastFolderSeperationCharIndex + 1);
            }
        }

        public static string GetAssetNameFromPath(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return null;
            }
            int lastForwardSlashIndex = assetPath.LastIndexOf('/');
            int assetNameStartIndex = lastForwardSlashIndex + 1;
            return assetPath.Substring(assetNameStartIndex, assetPath.Length - assetNameStartIndex);
        }
    }

}
