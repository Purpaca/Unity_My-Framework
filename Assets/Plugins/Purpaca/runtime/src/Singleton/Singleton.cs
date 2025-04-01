using System;

namespace Purpaca.Singleton
{
    /// <summary>
    /// 单例模式基类
    /// </summary>
    public abstract class Singleton<T> where T : Singleton<T>
    {
        private static volatile T m_instance;
        private static bool initialized => m_instance != null;
        private static object locker = new object();

        #region 构造器
        protected Singleton()
        {
            if (initialized)
            {
                throw new InvalidOperationException($"\"{typeof(T).FullName}\" is a singleton type and the unique instance already exists. Unable to create another instance of this type");
            }

            m_instance = this as T;
        }
        #endregion

        #region 属性
        protected static T instance
        {
            get
            {
                if (m_instance == null)
                {
                    lock (locker)
                    {
                        if (m_instance == null)
                        {
                            m_instance = Activator.CreateInstance(typeof(T), true) as T;
                        }

                        return m_instance;
                    }
                }

                return m_instance;
            }
        }
    }
    #endregion
}