using UnityEngine;
using UnityEngine.Events;
using IEnumerator = System.Collections.IEnumerator;

namespace Purpaca
{
    /// <summary>
    /// 共享的MonoBehaviour，为非继承自MonoBehaviour的类型提供使用MonoBehaviour的生命周期方法以及协程的功能
    /// </summary>
    public sealed class SharedMonoBehaviour : MonoManagerBase<SharedMonoBehaviour>
    {
        #region 字段
        private event UnityAction m_onUpdate;
        private event UnityAction m_onFixedUpdate;
        private event UnityAction m_onLateUpdate;
        #endregion

        #region 事件

        #region MonoBehaviour 生命周期方法相关事件
        public static event UnityAction OnUpdate
        {
            add => instance.m_onUpdate += value;
            remove => instance.m_onUpdate -= value;
        }

        public static event UnityAction OnFixedUpdate
        {
            add => instance.m_onFixedUpdate += value;
            remove => instance.m_onFixedUpdate -= value;
        }

        public static event UnityAction OnLateUpdate
        {
            add => instance.m_onLateUpdate += value;
            remove => instance.m_onLateUpdate -= value;
        }
        #endregion

        #endregion

        #region Public 方法
        /// <summary>
        /// 启动协程
        /// </summary>
        public static Coroutine LaunchCoroutine(IEnumerator routine)
        {
            return instance.StartCoroutine(routine);
        }

        /// <summary>
        /// 终止当前共享MonoBehaviour上的指定协程
        /// </summary>
        public static void EndCoroutine(Coroutine routine)
        {
            instance.StopCoroutine(routine);
        }

        /// <summary>
        /// 终止当前共享MonoBehaviour上正在运行的全部协程
        /// </summary>
        public static void EndAllCoroutine()
        {
            instance.StopAllCoroutines();
        }
        #endregion

        #region Unity 消息
        private void Update()
        {
            m_onUpdate?.Invoke();
        }

        private void FixedUpdate()
        {
            m_onFixedUpdate?.Invoke();
        }

        private void LateUpdate()
        {
            m_onLateUpdate?.Invoke();
        }
        #endregion
    }
}