using UnityEngine;

namespace Purpaca.Base
{
    public abstract class MonoModuleBase : MonoBehaviour
    {
        private static GameObject root;
        protected static GameObject RootGameObject 
        {
            get 
            {
                if(root == null) 
                {
                    root = new GameObject("Modules");
                    DontDestroyOnLoad(root);
                }

                return root;
            }
        }

        public static void ReleaseAllModules()
        {
            if (root != null)
            {
                Destroy(root);
                root = null;
            }
        }
    }

    /// <summary>
    /// 由MonoBehaviour驱动的模块基类型，提供单例访问和生命周期管理功能。通过泛型参数T指定模块类型，确保在场景中只有一个实例存在。
    /// </summary>
    /// <typeparam name="T">当前模块的类型</typeparam>
    public abstract class MonoModuleBase<T> : MonoModuleBase where T : MonoModuleBase<T>
    {
        private static T instance;

        /// <summary>
        /// 获取模块的单例实例
        /// </summary>
        /// <remarks>如果单例实例不存在，将会自动创建一个新的实例并设置为单例实例</remarks>
        public static T Instance
        {
            get
            {
                if (instance == null)
                {
                    var go = new GameObject(typeof(T).Name);
                    go.transform.SetParent(RootGameObject.transform);

                    instance = go.AddComponent<T>();
                }

                return instance;
            }
        }

        /// <summary>
        /// 释放模块当前的单例实例
        /// </summary>
        public static void Release()
        {
            if (instance != null)
            {
                Destroy(instance.gameObject);
                instance = null;
            }
        }

        /// <summary>
        /// 模块初始化时调用，调用时机类似于 “MonoBehaviour.Awake”。
        /// </summary>
        protected virtual void OnInit() { }

        /// <summary>
        /// 模块被销毁时调用，调用时机类似于 “MonoBehaviour.OnDestroy”。
        /// </summary>
        protected virtual void OnRelease() { }

        private void Awake()
        {
            if (instance != this) 
            {
                if(instance != null) 
                {
                    Destroy(this);
                    return;
                }
                else 
                {
                    instance = (T)this;
                }
            }

            if(transform.parent != RootGameObject.transform) 
            {
                transform.SetParent(RootGameObject.transform);
            }

            OnInit();
        }

        private void OnDestroy()
        {
            OnRelease();

            if (instance == this)
            {
                instance = null;
            }
        }
    }
}