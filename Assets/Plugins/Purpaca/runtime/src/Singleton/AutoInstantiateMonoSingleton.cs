using UnityEngine;

namespace Purpaca.Singleton
{
    /// <summary>
    /// 在被访问时会自动实例化的单例MonoBehaviour基类
    /// </summary>
    public abstract class AutoInstantiateMonoSingleton<T> : MonoBehaviour where T : AutoInstantiateMonoSingleton<T>
    {
        private static T m_instance;

        protected static T instance
        {
            get
            {
                if (m_instance == null)
                {
                    GameObject gameObject = new GameObject(typeof(T).Name);
                    m_instance = gameObject.AddComponent<T>();
                }

                return m_instance;
            }
        }

        #region Unity 消息
        protected virtual void Awake()
        {
            if (m_instance != null && !ReferenceEquals(m_instance, this))
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