/*
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;
using IEnumerator = System.Collections.IEnumerator;

namespace AssetManagement
{
    /// <summary>
    /// AssetBundle管理器
    /// </summary>
    public class AssetBundleManager : Purpaca.MonoManagerBase<AssetBundleManager>
    {
        private AssetBundle mainBundle;
        private AssetBundleManifest mainManifest;
        private Dictionary<string, AssetBundle> loadedBundles;
        private Dictionary<string, int> loadedBundleRefCount;

        #region 属性
        public static AssetBundleManager Instance { get => instance; }

        /// <summary>
        /// 当前设备平台上的AssetBundle本地存储路径
        /// </summary>
        public static string AssetBundlesStoragePath
        {
            get
            {
                switch (Application.platform)
                {
                    case RuntimePlatform.Android:
                    case RuntimePlatform.IPhonePlayer:
                    case RuntimePlatform.WebGLPlayer:
                        return Path.Combine(Application.persistentDataPath, "AssetBundles");
                    default:
                        return Path.Combine(Application.streamingAssetsPath, "AssetBundles");
                }
            }
        }
        #endregion

        #region Public 方法
        /// <summary>
        /// 从给定名称的AssetBundle中加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        public Object LoadAsset(string bundleName, string assetName)
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAsset(assetName);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中加载给定名称的资源
        /// </summary>
        /// <typeparam name="T">要加载的资源的类型</typeparam>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        public T LoadAsset<T>(string bundleName, string assetName) where T : Object
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAsset<T>(assetName);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="type">要加载的资源的类型</param>
        /// <returns></returns>
        public Object LoadAsset(string bundleName, string assetName, Type type)
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAsset(assetName, type);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        public AssetBundleRequest LoadAssetAsync(string bundleName, string assetName)
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAssetAsync(assetName);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="callback">加载请求结束后的回调</param>
        public void LoadAssetAsync(string bundleName, string assetName, UnityAction<Object> callback)
        {
            StartCoroutine(LoadAssetAsyncCoroutine(bundleName, assetName, callback));
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <typeparam name="T">要加载的资源的类型</typeparam>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        public AssetBundleRequest LoadAssetAsync<T>(string bundleName, string assetName) where T : Object
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAssetAsync<T>(assetName);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <typeparam name="T">要加载的资源的类型</typeparam>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="callback">加载请求结束后的回调</param>
        public void LoadAssetAsync<T>(string bundleName, string assetName, UnityAction<T> callback) where T : Object
        {
            StartCoroutine(LoadAssetAsyncCoroutine<T>(bundleName, assetName, callback));
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="type">要加载的资源的类型</param>
        public AssetBundleRequest LoadAssetAsync(string bundleName, string assetName, Type type)
        {
            LoadAssetBundle(bundleName);
            return loadedBundles[bundleName].LoadAssetAsync(assetName, type);
        }

        /// <summary>
        /// 从给定名称的AssetBundle中异步加载给定名称的资源
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="type">要加载的资源的类型</param>
        /// <param name="callback">加载请求结束后的回调</param>
        public void LoadAssetAsync(string bundleName, string assetName, Type type, UnityAction<Object> callback)
        {
            StartCoroutine(LoadAssetAsyncCoroutine(bundleName, assetName, type, callback));
        }

        /// <summary>
        /// 卸载指定名称的AssetBundle
        /// </summary>
        /// <param name="bundleName">要卸载的AssetBundle的名称</param>
        public void UnloadAssetBundle(string bundleName, bool unloadAllLoadedObjects = false)
        {
            if (loadedBundles.ContainsKey(bundleName))
            {
                loadedBundles[bundleName].Unload(unloadAllLoadedObjects);
                loadedBundles.Remove(bundleName);
                loadedBundleRefCount.Remove(bundleName);

                UnloadDependencies(bundleName);
            }
        }

        /// <summary>
        /// 卸载所有的AssetBundle
        /// </summary>
        /// <param name="unloadAllLoadedObjects"></param>
        public void UnloadAllAssetBundles(bool unloadAllLoadedObjects = false)
        {
            AssetBundle.UnloadAllAssetBundles(unloadAllLoadedObjects);
            mainBundle = null;
            mainManifest = null;
            loadedBundles.Clear();
        }
        #endregion

        #region Private 方法
        /// <summary>
        /// 加载指定名称的AssetBundle
        /// </summary>
        /// <param name="bundleName">指定的AssetBundle的名称</param>
        private void LoadAssetBundle(string bundleName)
        {
            if (!loadedBundles.ContainsKey(bundleName))
            {
                LoadDependencies(bundleName);

                AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(AssetBundlesStoragePath, bundleName));
                loadedBundles.Add(bundleName, bundle);
                loadedBundleRefCount.Add(bundleName, 1);
            }
        }

        /// <summary>
        /// 加载指定名称的AssetBundle的依赖
        /// </summary>
        /// <param name="bundleName">指定的AssetBundle的名称</param>
        private void LoadDependencies(string bundleName)
        {
            string[] dependencies = mainManifest.GetAllDependencies(bundleName);

            foreach (string dependency in dependencies)
            {
                if (!loadedBundles.ContainsKey(dependency))
                {
                    AssetBundle bundle = AssetBundle.LoadFromFile(Path.Combine(AssetBundlesStoragePath, dependency));
                    loadedBundles.Add(dependency, bundle);
                    loadedBundleRefCount.Add(dependency, 1);
                }
                else
                {
                    loadedBundleRefCount[dependency] += 1;
                }
            }
        }

        /// <summary>
        /// 卸载指定名称的AssetBundle的依赖
        /// </summary>
        /// <param name="bundleName">指定的AssetBundle的名称</param>
        private void UnloadDependencies(string bundleName)
        {
            string[] dependencies = mainManifest.GetAllDependencies(bundleName);
            foreach (string dependency in dependencies)
            {
                if (loadedBundleRefCount.ContainsKey(dependency))
                {
                    loadedBundleRefCount[dependency] -= 1;

                    if (loadedBundleRefCount[dependency] <= 0)
                    {
                        UnloadAssetBundle(dependency);
                    }
                }
            }
        }
        #endregion

        #region 协程
        /// <summary>
        /// 异步加载资源的协程
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="callback">加载请求结束后的回调</param>
        private IEnumerator LoadAssetAsyncCoroutine(string bundleName, string assetName, UnityAction<Object> callback)
        {
            LoadAssetBundle(bundleName);
            var request = loadedBundles[bundleName].LoadAssetAsync(assetName);
            yield return request;
            callback?.Invoke(request.asset);
        }

        /// <summary>
        /// 异步加载资源的协程
        /// </summary>
        /// <typeparam name="T">要加载的资源的类型</typeparam>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="callback">加载请求结束后的回调</param>
        private IEnumerator LoadAssetAsyncCoroutine<T>(string bundleName, string assetName, UnityAction<T> callback) where T : Object
        {
            LoadAssetBundle(bundleName);
            var request = loadedBundles[bundleName].LoadAssetAsync(assetName);
            yield return request;
            callback?.Invoke(request.asset as T);
        }

        /// <summary>
        /// 异步加载资源的协程
        /// </summary>
        /// <param name="bundleName">给定的AssetBundle名称</param>
        /// <param name="assetName">给定的资源名称</param>
        /// <param name="type">要加载的资源的类型</param>
        /// <param name="callback">加载请求结束后的回调</param>
        private IEnumerator LoadAssetAsyncCoroutine(string bundleName, string assetName, Type type, UnityAction<Object> callback)
        {
            LoadAssetBundle(bundleName);
            var request = loadedBundles[bundleName].LoadAssetAsync(assetName, type);
            yield return request;
            callback?.Invoke(request.asset);
        }
        #endregion

        #region Unity 消息
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(gameObject);

            loadedBundles = new Dictionary<string, AssetBundle>();
            loadedBundleRefCount = new Dictionary<string, int>();


            mainBundle = AssetBundle.LoadFromFile(Path.Combine(AssetBundlesStoragePath, "AssetBundle"));
            mainManifest = mainBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            if (mainManifest == null)
            {
                throw new NullReferenceException($"Can not load the AssetBundleManifest asset from the main bundle at \"{Path.Combine(AssetBundlesStoragePath, "AssetBundle")}\".\nThe bundle at this location is not reachable or it is not the Main AssetBundle.");
            }

        }

        private void OnDestroy()
        {
            AssetBundle.UnloadAllAssetBundles(false);
        }
        #endregion
    }
}
*/