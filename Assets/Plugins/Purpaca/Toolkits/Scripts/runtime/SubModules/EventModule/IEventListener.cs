using System;

namespace Purpaca.Events
{
    public interface IEventListenerBase { }

    /// <summary>
    /// 事件监听者
    /// </summary>
    public interface IEventListener : IEventListenerBase
    {
        /// <summary>
        /// 事件监听者提供的回调方法
        /// </summary>
        public void Invoke();
    }

    /// <summary>
    /// 接受一个参数的事件监听者
    /// </summary>
    /// <typeparam name="T">要接收的参数的类型</typeparam>
    public interface IEventListener<T> : IEventListenerBase
    {
        /// <summary>
        /// 所需要提供参数的类型
        /// </summary>
        public Type ParameterType { get => typeof(T); }

        /// <summary>
        /// 事件监听者提供的需要一个参数回调方法
        /// </summary>
        /// <param name="parameter">所提供的参数</param>
        public void Invoke(T parameter);
    }
}