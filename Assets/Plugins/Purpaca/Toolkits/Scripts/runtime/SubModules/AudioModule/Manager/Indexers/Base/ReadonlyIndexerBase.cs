namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// AudioManager向外部提供的便捷访问音频播放句柄属性的只读索引器的基类
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public abstract class ReadonlyIndexerBase<T>
        {
            private readonly AudioManager manager;

            protected ReadonlyIndexerBase(AudioManager manager)
            {
                this.manager = manager;
            }

            /// <summary>
            /// 持有当前便捷索引器的AudioManager实例
            /// </summary>
            protected AudioManager Manager => manager;

            public abstract T this[string name] { get; }
        }
    }
}