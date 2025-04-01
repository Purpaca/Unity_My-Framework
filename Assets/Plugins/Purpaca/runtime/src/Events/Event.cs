using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace Purpaca.Events
{
    /// <summary>
    /// 事件实例，用于存储事件的监听者和提供实际的事件广播与注册监听者方法
    /// </summary>
    public class Event
    {
        #region 字段
        private Dictionary<string, ListenerBase> m_listeners = new Dictionary<string, ListenerBase>();
        #endregion

        #region 构造器
        public Event() 
        {
            m_listeners = new Dictionary<string, ListenerBase>();
        }
        #endregion

        #region Public 方法
        /// <summary>
        /// 广播事件
        /// </summary>
        public void Broadcast()
        {
            foreach (var listener in m_listeners.Values)
            {
                if (listener is ListenerNonParameter)
                {
                    (listener as ListenerNonParameter).Invoke();
                }
                else if(listener is ListenerRequiresParameterBase)
                {
                    var l = listener as ListenerRequiresParameterBase;
                    if(l.InvokeWithoutParameter)
                    {
                        BroadcastWithDefaultParameterValue(listener);
                    }
                }
            }
        }

        /// <summary>
        /// 广播事件并提供参数
        /// </summary>
        /// <param name="parameter">提供的参数</param>
        public void Broadcast<T>(T parameter)
        {
            foreach (var listener in m_listeners.Values)
            {
                if (listener is ListenerNonParameter)
                {
                    (listener as ListenerNonParameter).Invoke();
                }
                else if (listener is ListenerRequiresParameter<T>)
                {
                    (listener as ListenerRequiresParameter<T>).Invoke(parameter);
                }
                else if (listener is ListenerRequiresParameter)
                {
                    var l = listener as ListenerRequiresParameter;
                    if (l.ParameterType == typeof(T))
                    {
                        l.Invoke(parameter);
                    }
                    else if (l.InvokeWithoutParameter)
                    {
                        BroadcastWithDefaultParameterValue(listener);
                    }
                }
            }
        }

        /// <summary>
        /// 广播事件并提供参数
        /// </summary>
        /// <param name="parameter">提供的参数</param>
        /// <param name="type">所提供参数的类型</param>
        public void Broadcast(object parameter, Type type)
        {
            foreach (var listener in m_listeners.Values)
            {
                if (listener is ListenerNonParameter)
                {
                    (listener as ListenerNonParameter).Invoke();
                }
                else
                {
                    if (listener is ListenerRequiresParameter)
                    {
                        var l = listener as ListenerRequiresParameter;
                        if (l.ParameterType == type)
                        {
                            l.Invoke(parameter);
                        }
                        else if (l.InvokeWithoutParameter)
                        {
                            BroadcastWithDefaultParameterValue(listener);
                        }
                    }
                    else
                    {
                        Type genericType = typeof(ListenerRequiresParameter<>).MakeGenericType(type);
                        if (listener.GetType() == genericType)
                        {
                            listener.GetType().GetMethod("Invoke").Invoke(listener, new object[] { parameter });
                        }
                        else
                        {
                            if ((listener as ListenerRequiresParameterBase).InvokeWithoutParameter)
                            {
                                BroadcastWithDefaultParameterValue(listener);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 为此事件注册一个监听者
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <returns>此监听者的唯一标识ID</returns>
        public string AddListener(UnityAction callback)
        {
            string guid = Guid.NewGuid().ToString();
            var listener = new ListenerNonParameter(callback);
            m_listeners.Add(guid, listener);
            return guid;
        }

        /// <summary>
        /// 为此事件注册一个监听者
        /// </summary>
        /// <typeparam name="T">监听者的回调方法所需参数的类型</typeparam>
        /// <param name="callback">回调方法</param>
        /// <returns>此监听者的唯一标识ID</returns>
        public string AddListener<T>(UnityAction<T> callback)
        {
            string guid = Guid.NewGuid().ToString();
            var listener = new ListenerRequiresParameter<T>(callback);
            m_listeners.Add(guid, listener);
            return guid;
        }

        /// <summary>
        /// 为此事件注册一个监听者
        /// </summary>
        /// <typeparam name="T">监听者的回调方法所需参数的类型</typeparam>
        /// <param name="callback">回调方法</param>
        /// <param name="invokeWithoutParameter">当事件广播时未提供对应的参数，是否调用此监听者的回调方法？</param>
        /// <returns>此监听者的唯一标识ID</returns>
        public string AddListener<T>(UnityAction<T> callback, bool invokeWithoutParameter)
        {
            string guid = Guid.NewGuid().ToString();
            var listener = new ListenerRequiresParameter<T>(callback, invokeWithoutParameter);
            m_listeners.Add(guid, listener);
            return guid;
        }

        /// <summary>
        /// 为此事件注册一个监听者
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <param name="type">监听者的回调方法所需参数的类型</param>
        /// <returns>此监听者的唯一标识ID</returns>
        public string AddListener(UnityAction<object> callback, Type type)
        {
            string guid = Guid.NewGuid().ToString();
            var listener = new ListenerRequiresParameter(callback, type);
            m_listeners.Add(guid, listener);
            return guid;
        }

        /// <summary>
        /// 为此事件注册一个监听者
        /// </summary>
        /// <param name="callback">回调方法</param>
        /// <param name="type">监听者的回调方法所需参数的类型</param>
        /// <param name="invokeWithoutParameter">当事件广播时未提供对应的参数，是否调用此监听者的回调方法？</param>
        /// <returns>此监听者的唯一标识ID</returns>
        public string AddListener(UnityAction<object> callback, Type type, bool invokeWithoutParameter)
        {
            string guid = Guid.NewGuid().ToString();
            var listener = new ListenerRequiresParameter(callback, type, invokeWithoutParameter);
            m_listeners.Add(guid, listener);
            return guid;
        }

        /// <summary>
        /// 移除唯一标识ID对应的监听者
        /// </summary>
        /// <param name="guid">所要移除监听者的唯一标识ID</param>
        public void RemoveListener(string guid)
        {
            if (m_listeners.ContainsKey(guid))
                m_listeners.Remove(guid);
        }

        /// <summary>
        /// 清除所有监听者
        /// </summary>
        public void ClearListener()
        {
            m_listeners.Clear();
        }
        #endregion

        #region Private 方法
        /// <summary>
        /// 以提供为null或默认值的参数的方式广播事件
        /// </summary>
        private void BroadcastWithDefaultParameterValue(ListenerBase listener)
        {
            var method = listener.GetType().GetMethod("Invoke");

            var parameters = method.GetParameters();
            object[] args = new object[parameters.Length];
            for (int i = 0; i < parameters.Length; i++)
            {
                args[i] = parameters[i].ParameterType.IsValueType ? Activator.CreateInstance(parameters[i].ParameterType) : null;
            }

            method.Invoke(listener, args);
        }
        #endregion
    }
}
