namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的多普勒效应级别值的属性索引器代理
        /// </summary>
        public class DopplerLevelIndexer : IndexerBase<float>
        {
            public DopplerLevelIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].DopplerLevel : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].DopplerLevel = value;
                    }
                }
            }
        }
    }
}