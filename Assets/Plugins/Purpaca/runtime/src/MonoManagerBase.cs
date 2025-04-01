using Purpaca.Singleton;

namespace Purpaca
{
    /// <summary>
    /// 基于MonoBehaviour的管理器基类
    /// </summary>
    public abstract class MonoManagerBase<T> : AutoInstantiateMonoSingleton<T> where T : MonoManagerBase<T>
    {
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public static void Init()
        {
            _ = instance;
        }

        /// <summary>
        /// 当管理器被初始化时调用，相当于MonoBehaviour.Awake
        /// </summary>
        protected virtual void OnInit() { }

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