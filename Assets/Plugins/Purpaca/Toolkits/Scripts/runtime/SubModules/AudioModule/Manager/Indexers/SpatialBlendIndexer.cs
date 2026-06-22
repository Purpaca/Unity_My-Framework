namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的空间混合值的属性索引器代理
        /// </summary>
        public class SpatialBlendIndexer : IndexerBase<float>
        {
            public SpatialBlendIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].SpatialBlend : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].SpatialBlend = value;
                    }
                }
            }
        }
    }
}