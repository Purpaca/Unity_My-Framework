using System;
using System.Collections.Generic;
using UnityEngine;

namespace Purpaca.Events
{
    /// <summary>
    /// 事件实例，用于存储事件的监听者和提供实际的事件广播与注册监听者方法
    /// </summary>
    public class Event
    {
        #region 字段
        private List<IEventListenerBase> m_listeners;
        #endregion

        #region 构造器
        public Event() 
        {
            m_listeners = new List<IEventListenerBase>();
        }
        #endregion

        #region Public 方法
        /// <summary>
        /// 广播事件
        /// </summary>
        public void Broadcast()
        {
            foreach (var listener in m_listeners)
            {
                if(listener is IEventListener) 
                {
                    (listener as IEventListener).Invoke();
                }
            }
        }

        /// <summary>
        /// 广播事件并提供参数
        /// </summary>
        /// <param name="parameter">提供的参数</param>
        public void Broadcast<T>(T parameter)
        {
            foreach (var listener in m_listeners)
            {
                if (listener is IEventListener<T>)
                {
                    (listener as IEventListener<T>).Invoke(parameter);
                }
            }
        }

        /// <summary>
        /// 广播事件并提供参数
        /// </summary>
        /// <remarks>此方法依赖于反射，存在一定程度上的性能损耗，谨慎使用！</remarks>
        /// <param name="parameter">提供的参数</param>
        /// <param name="type">所提供参数的类型</param>
        public void Broadcast(object parameter, Type type)
        {
            foreach (var listener in m_listeners)
            {
                var interfaces = listener.GetType().GetInterfaces();
                foreach (var interfaceType in interfaces)
                {
                    // 获取IEventListener<T>接口,并调用Invoke方法
                    if (interfaceType.IsGenericType && interfaceType.GetGenericTypeDefinition() == typeof(IEventListener<>))
                    {
                        var genericType = interfaceType.GetGenericArguments()[0];
                        var actualInterfaceType = typeof(IEventListener<>).MakeGenericType(genericType);
                        var paramType = actualInterfaceType.GetProperty("ParameterType").GetValue(listener) as Type;
                        if (paramType == type)
                        {
                            var method = actualInterfaceType.GetMethod("Invoke");
                            method.Invoke(listener, new object[] { parameter });
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 添加指定的事件监听者
        /// </summary>
        /// <param name="listener">要添加的事件监听者</param>
        public void AddListener(IEventListenerBase listener)
        {
            if (m_listeners.Contains(listener)) 
            {
                Debug.LogWarning($"this listener is already exist!");
                return;
            }

            m_listeners.Add(listener);
        }

        /// <summary>
        /// 移除指定的事件监听者
        /// </summary>
        /// <param name="listenerToRemove">要移除的事件监听者</param>
        public void RemoveListener(IEventListenerBase listenerToRemove)
        {
            if (m_listeners.Contains(listenerToRemove)) 
            {
                m_listeners.Remove(listenerToRemove);
            }
        }

        /// <summary>
        /// 清除所有监听者
        /// </summary>
        public void ClearListener()
        {
            m_listeners.Clear();
        }
        #endregion
    }
}