namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的是否忽略混响区域的属性索引器代理
        /// </summary>
        public class BypassReverbZonesIndexer : IndexerBase<bool>
        {
            public BypassReverbZonesIndexer(AudioManager manager) : base(manager) { }

            public override bool this[string guid] 
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].BypassReverbZones : false;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].BypassReverbZones = value;
                    }
                }
            }
        }
    }
}
