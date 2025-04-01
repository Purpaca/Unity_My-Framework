using UnityEngine;

namespace Purpaca.Singleton
{
    /// <summary>
    /// 只能存在一个实例对象的单例MonoBehaviour基类
    /// </summary>
    public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T> 
    {
        private static T m_instance;

        #region 属性
        protected static T instance { get => m_instance; }
        #endregion

        #region Public 方法
        /// <summary>
        /// 获取此类型的单例实例
        /// </summary>
        /// <param name="includeInactive">是否允许获取处于未激活状态的实例</param>
        public static T GetInstance(bool includeInactive = false) 
        {
            if (m_instance == null)
            {
                m_instance = FindObjectOfType<T>(includeInactive);
            }

            return m_instance;
        }
        #endregion

        #region Unity 消息
        protected virtual void Awake()
        {
            if (m_instance != null)
            {
                Debug.LogError($"Component \"{typeof(T).FullName}\" on gameobject \"{gameObject.name}\" is designed as a singleton script component, and the unique instance already exists. This instance would be destroyed.");
                Destroy(this);
                return;
            }

            m_instance = (T)this;
        }
        #endregion
    }
}
