using UnityEngine.Events;

namespace Purpaca.Events.ManagerExtends
{
    /// <summary>
    /// 无需参数的监听者
    /// </summary>
    internal class ListenerNonParameter : IEventListener, IEventListener<object>
    {
        #region 字段
        private UnityAction m_callback;
        #endregion

        #region 构造器
        /// <param name="callback">回调方法</param>
        public ListenerNonParameter(UnityAction callback)
        {
            m_callback = callback;
        }
        #endregion

        #region Public 方法
        public void Invoke()
        {
            m_callback?.Invoke();
        }

        public void Invoke(object parameter)
        {
            m_callback?.Invoke();
        }
        #endregion
    }
}