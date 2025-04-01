using Purpaca.Singleton;

namespace Purpaca
{
    /// <summary>
    /// 管理器基类
    /// </summary>
    public abstract class ManagerBase<T> : Singleton<T> where T : ManagerBase<T>
    {
        /// <summary>
        /// 初始化管理器
        /// </summary>
        public static void Init()
        {
            _ = instance;
        }

        public static T Instance => instance;

        /// <summary>
        /// 当管理器被初始化时调用
        /// </summary>
        protected virtual void OnInit() { }

        #region Unity 消息
        protected ManagerBase() : base()
        {
            OnInit();
        }
        #endregion
    }
}