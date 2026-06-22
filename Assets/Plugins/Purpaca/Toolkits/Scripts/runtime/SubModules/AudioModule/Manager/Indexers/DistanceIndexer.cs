namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的最小播放距离的属性索引器代理
        /// </summary>
        public class MinDistanceIndexer : IndexerBase<float>
        {
            public MinDistanceIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].MinDistance : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].MinDistance = value;
                    }
                }
            }
        }

        /// <summary>
        /// 音频播放句柄的最大播放距离的属性索引器代理
        /// </summary>
        public class MaxDistanceIndexer : IndexerBase<float>
        {
            public MaxDistanceIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].MaxDistance : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].MaxDistance = value;
                    }
                }
            }
        }
    }
}