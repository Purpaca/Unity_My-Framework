using Purpaca.Singleton;

namespace Purpaca
{
    /// <summary>
    /// 管理器基类
    /// </summary>
    public abstract class ManagerBase<T> : Singleton<T> where T : ManagerBase<T>
    {
        #region 构造器
        protected ManagerBase() : base()
        {
            OnInit();
        }
        #endregion

        #region 属性
        /// <summary>
        /// 管理器<see cref="T"/>的单例实例
        /// </summary>
        public static T Instance => instance;
        #endregion

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
        /// </summary>
        protected virtual void OnInit() { }
        #endregion
    }
}