using System;

namespace Purpaca.Events
{
    /// <summary>
    /// 事件监听者基类
    /// </summary>
    public abstract class ListenerBase { }

    /// <summary>
    /// 需要参数的监听者基类
    /// </summary>
    public abstract class ListenerRequiresParameterBase : ListenerBase
    {
        /// <summary>
        /// 所需参数的类型
        /// </summary>
        public abstract Type ParameterType { get; }

        /// <summary>
        /// 当监听的事件以未提供参数的方式广播时，是否调用回调方法？
        /// </summary>
        public abstract bool InvokeWithoutParameter { get; }
    }
}