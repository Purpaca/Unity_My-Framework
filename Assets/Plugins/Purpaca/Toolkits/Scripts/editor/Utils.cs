using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Purpaca.Editor
{
    public static class Utils
    {
        #region Public 方法

        #region ScriptableObject Asset相关
        /// <summary>
        /// 在当前 Projects视图 中选择的路径下创建指定类型的ScriptableObject资产（如果当前在 Projects视图 中没有选择有效的路径，将直接在"Assets"下创建）
        /// </summary>
        /// <typeparam name="T">要创建的ScriptableObject资源的类型</typeparam>
        /// <param name="assetName">要创建资源的名称</param>
        public static void CreateScriptableAsset<T>(string assetName) where T : ScriptableObject
        {
            var asset = (ScriptableObject)ScriptableObject.CreateInstance<T>();
            CreateScriptableAsset(ref asset, assetName);
        }

        /// <summary>
        /// 在当前 Projects视图 中选择的路径下创建指定类型的ScriptableObject资产（如果当前在 Projects视图 中没有选择有效的路径，将直接在"Assets"下创建）
        /// </summary>
        /// <param name="type">要创建的ScriptableObject资源的类型</param>
        /// <param name="assetName">要创建资源的名称</param>
        public static void CreateScriptableAsset(Type type, string assetName)
        {
            if (!type.IsSubclassOf(typeof(ScriptableObject)) || type.IsAbstract)
            {
                Debug.LogError($"无法创建类型为\"{type.FullName}\"的资产，此类型是抽象类型或其未继承自ScriptableObject!");
                return;
            }

            var asset = ScriptableObject.CreateInstance(type);
            CreateScriptableAsset(ref asset, assetName);
        }

        /// <summary>
        /// 将指定的ScriptableObject实例保存为资产
        /// </summary>
        /// <param name="assetName">要创建资源的名称</param>
        public static void CreateScriptableAsset(ref ScriptableObject scriptable, string assetName)
        {
            string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (string.IsNullOrEmpty(selectedPath))
            {
                selectedPath = "Assets";
            }

            string path = File.Exists(selectedPath) ? Path.GetDirectoryName(selectedPath) : selectedPath;
            path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, $"{assetName}.asset"));

            AssetDatabase.CreateAsset(scriptable, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();

            Selection.activeObject = scriptable;
        }

        /*
        public static void CreateScriptableAsset(ref ScriptableObject scriptable, string assetName, string path)
        {
            path = AssetDatabase.GenerateUniqueAssetPath(Path.Combine(path, $"{assetName}.asset"));
            AssetDatabase.CreateAsset(scriptable, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = scriptable;
        }
        */
        #endregion

        #endregion
    }
}
