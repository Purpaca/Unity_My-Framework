using System;
using System.Collections.Generic;
using UnityEngine.Events;
using Purpaca.Events;

namespace Purpaca
{
    /// <summary>
    /// 事件管理器，统一管理游戏中的事件的广播与监听
    /// </summary>
    public class EventManager : ManagerBase<EventManager>
    {
        #region 字段
        private Dictionary<string, EventInfo> m_events;
        #endregion

        #region Public 方法
        /// <summary>
        /// 广播指定名称的事件
        /// </summary>
        /// <param name="eventName">所要广播事件的名称</param>
        public void Broadcast(string eventName)
        {
            if (m_events.ContainsKey(eventName))
            {
                m_events[eventName].Broadcast();
            }
        }

        /// <summary>
        /// 广播指定名称的事件，并提供参数
        /// </summary>
        /// <typeparam name="T">所提供参数的类型</typeparam>
        /// <param name="eventName">所要广播事件的名称</param>
        /// <param name="parameter">提供的参数</param>
        public void Broadcast<T>(string eventName, T parameter)
        {
            if (m_events.ContainsKey(eventName))
            {
                m_events[eventName].Broadcast(parameter);
            }
        }

        /// <summary>
        /// 广播指定名称的事件，并提供参数
        /// </summary>
        /// <param name="eventName">所要广播事件的名称</param>
        /// <param name="parameter">提供的参数</param>
        /// <param name="type">所提供参数的类型</param>
        public void Broadcast(string eventName, object parameter, Type type)
        {
            if (m_events.ContainsKey(eventName))
            {
                m_events[eventName].Broadcast(parameter, type);
            }
        }

        /// <summary>
        /// 为指定名称的事件注册一个监听者
        /// </summary>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callback">回调方法</param>
        /// <returns>指定名称的事件下，此监听者的唯一标识ID</returns>
        public string AddListener(string eventName, UnityAction callback)
        {
            if (!m_events.ContainsKey(eventName))
            {
                m_events.Add(eventName, new EventInfo());
            }

            ListenerNonParameter listener = new ListenerNonParameter(callback);
            
            return m_events[eventName].AddListener(listener);
        }

        /// <summary>
        /// 为指定名称的事件注册一个需要参数的监听者
        /// </summary>
        /// <typeparam name="T">所需参数的类型</typeparam>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callback">回调方法</param>
        /// <returns>指定名称的事件下，此监听者的唯一标识ID</returns>
        public string AddListener<T>(string eventName, UnityAction<T> callback)
        {
            if (!m_events.ContainsKey(eventName))
            {
                m_events.Add(eventName, new EventInfo());
            }

            ListenerRequiresParameter<T> listener = new ListenerRequiresParameter<T>(callback);
            
            return m_events[eventName].AddListener(listener);
        }

        /// <summary>
        /// 为指定名称的事件注册一个需要参数的监听者
        /// </summary>
        /// <typeparam name="T">所需参数的类型</typeparam>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callback">回调方法</param>
        /// <param name="invokeWithoutParameter">当事件广播时未提供对应的参数，是否调用此监听者的回调方法？</param>
        /// <returns>指定名称的事件下，此监听者的唯一标识ID</returns>
        public string AddListener<T>(string eventName, UnityAction<T> callback, bool invokeWithoutParameter)
        {
            if (!m_events.ContainsKey(eventName))
            {
                m_events.Add(eventName, new EventInfo());
            }

            ListenerRequiresParameter<T> listener = new ListenerRequiresParameter<T>(callback, invokeWithoutParameter);
            
            return m_events[eventName].AddListener(listener);
        }

        /// <summary>
        /// 为指定名称的事件注册一个需要参数的监听者
        /// </summary>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callback">回调方法</param>
        /// <param name="type">所需参数的类型</param>
        /// <returns>指定名称的事件下，此监听者的唯一标识ID</returns>
        public string AddListener(string eventName, UnityAction<object> callback, Type type)
        {
            if (!m_events.ContainsKey(eventName))
            {
                m_events.Add(eventName, new EventInfo());
            }

            ListenerRequiresParameter listener = new ListenerRequiresParameter(callback, type);
            
            return m_events[eventName].AddListener(listener);
        }

        /// <summary>
        /// 为指定名称的事件注册一个需要参数的监听者
        /// </summary>
        /// <param name="eventName">要监听的事件名称</param>
        /// <param name="callback">回调方法</param>
        /// <param name="type">所需参数的类型</param>
        /// <param name="invokeWithoutParameter">当事件广播时未提供对应的参数，是否调用此监听者的回调方法？</param>
        /// <returns>指定名称的事件下，此监听者的唯一标识ID</returns>
        public string AddListener(string eventName, UnityAction<object> callback, Type type, bool invokeWithoutParameter)
        {
            if (!m_events.ContainsKey(eventName))
            {
                m_events.Add(eventName, new EventInfo());
            }

            ListenerRequiresParameter listener = new ListenerRequiresParameter(callback, type, invokeWithoutParameter);
            
            return m_events[eventName].AddListener(listener);
        }

        /// <summary>
        /// 移除指定名称的事件下与唯一标识ID对应的监听者
        /// </summary>
        /// <param name="eventName">要移除监听者的事件名称</param>
        /// <param name="guid">与要移除的监听者对应的唯一标识ID</param>
        public void RemoveListener(string eventName, string guid)
        {
            if (m_events.ContainsKey(eventName))
            {
                m_events[eventName].RemoveListener(guid);
                if (m_events[eventName].ListenerCount <= 0) 
                {
                    m_events.Remove(eventName);
                }
            }
        }

        /// <summary>
        /// 清除指定名称的事件下所有的监听者
        /// </summary>
        /// <param name="eventName">要清除监听者的事件名称</param>
        public void ClearListener(string eventName)
        {
            if (m_events.ContainsKey(eventName))
            {
                m_events.Remove(eventName);
            }
        }

        /// <summary>
        /// 清除所有的事件
        /// </summary>
        public void ClearAllEvents()
        {
            m_events.Clear();
        }
        #endregion

        #region Protected 方法
        protected override void OnInit()
        {
            m_events = new Dictionary<string, EventInfo>();
        }
        #endregion

        #region 内部类型
        private class EventInfo 
        {
            #region 字段
            private Event m_event;
            private Dictionary<string,IEventListenerBase> m_listeners;
            #endregion

            #region 构造器
            public EventInfo()
            {
                m_event = new Event();
                m_listeners = new Dictionary<string, IEventListenerBase>();
            }
            #endregion

            #region 属性
            /// <summary>
            /// 订阅此事件的监听者数量
            /// </summary>
            public int ListenerCount { get => m_listeners.Count; }
            #endregion

            #region Public 方法
            /// <summary>
            /// 广播此事件
            /// </summary>
            public void Broadcast()
            {
                m_event.Broadcast();
            }

            /// <summary>
            /// 广播此事件，并提供参数
            /// </summary>
            /// <typeparam name="T">所提供参数的类型</typeparam>
            /// <param name="parameter">提供的参数</param>
            public void Broadcast<T>(T parameter)
            {
                m_event.Broadcast(parameter);
            }

            /// <summary>
            /// 广播此事件，并提供参数
            /// </summary>
            /// <param name="parameter">提供的参数</param>
            /// <param name="type">所提供参数的类型</param>
            public void Broadcast(object parameter, Type type)
            {
                m_event.Broadcast(parameter, type);
            }

            /// <summary>
            /// 添加事件监听者
            /// </summary>
            /// <returns>事件监听者的唯一标识ID</returns>
            public string AddListener(IEventListenerBase listener)
            {
                var guid = Guid.NewGuid().ToString();
                m_listeners.Add(guid, listener);
                m_event.AddListener(listener);
                return guid;
            }

            /// <summary>
            /// 移除指定的事件监听者
            /// </summary>
            /// <param name="guid">要移除的事件监听者的唯一标识ID</param>
            public void RemoveListener(string guid) 
            {
                if (m_listeners.ContainsKey(guid)) 
                {
                    m_event.RemoveListener(m_listeners[guid]);
                    m_listeners.Remove(guid);
                }
            }
            #endregion
        }
        #endregion
    }
}