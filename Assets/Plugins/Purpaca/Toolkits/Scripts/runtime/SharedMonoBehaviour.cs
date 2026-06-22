using UnityEngine;
using UnityEngine.Events;
using Purpaca.Singleton;
using IEnumerator = System.Collections.IEnumerator;

namespace Purpaca
{

    /// <summary>
    /// 共享的MonoBehaviour，为非继承自MonoBehaviour的类型提供使用MonoBehaviour生命周期以及协程功能的能力
    /// </summary>
    public sealed class SharedMonoBehaviour : MonoSingleton<SharedMonoBehaviour>
    {
        private event UnityAction onUpdateEvent;
        private event UnityAction onFixedUpdateEvent;
        private event UnityAction onLateUpdateEvent;


        public static event UnityAction OnUpdate
        {
            add => instance.onUpdateEvent += value;
            remove => instance.onUpdateEvent -= value;
        }

        public static event UnityAction OnFixedUpdate
        {
            add => instance.onFixedUpdateEvent += value;
            remove => instance.onFixedUpdateEvent -= value;
        }

        public static event UnityAction OnLateUpdate
        {
            add => instance.onLateUpdateEvent += value;
            remove => instance.onLateUpdateEvent -= value;
        }

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

        private void Update()
        {
            onUpdateEvent?.Invoke();
        }

        private void FixedUpdate()
        {
            onFixedUpdateEvent?.Invoke();
        }

        private void LateUpdate()
        {
            onLateUpdateEvent?.Invoke();
        }
    }
}