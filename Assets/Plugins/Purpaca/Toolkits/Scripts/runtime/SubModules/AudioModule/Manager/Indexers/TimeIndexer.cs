namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的当前播放时间的属性索引器代理
        /// </summary>
        public class TimeIndexer : IndexerBase<float>
        {
            public TimeIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Time : float.NaN;
                set
                {
                    if(Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].Time = value;
                    }
                }
            }
        }
    }
}