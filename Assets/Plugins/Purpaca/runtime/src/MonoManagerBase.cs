using Purpaca.Singleton;

namespace Purpaca
{
    /// <summary>
    /// 基于MonoBehaviour的管理器基类
    /// </summary>
    public abstract class MonoManagerBase<T> : AutoInstantiateMonoSingleton<T> where T : MonoManagerBase<T>
    {
        #region Public 方法
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public static void Init()
        {
            _ = instance;
        }
        #endregion

        #region Protected 方法
        /// <summary>
        /// 当管理器被初始化时调用
        /// 相当于<see cref="UnityEngine.MonoBehaviour"/>的 Awake() 方法"/>
        /// </summary>
        protected virtual void OnInit() { }
        #endregion

        #region Unity 消息
        protected override void Awake()
        {
            base.Awake();
            DontDestroyOnLoad(instance.gameObject);
            instance.OnInit();
        }
        #endregion
    }
}