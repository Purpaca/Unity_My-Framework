namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的是否忽略混响效果的属性索引器代理
        /// </summary>
        public class BypassEffectsIndexer : IndexerBase<bool>
        {
            public BypassEffectsIndexer(AudioManager manager) : base(manager) { }

            public override bool this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].BypassEffects : false;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].BypassEffects = value;
                    }
                }
            }
        }
    }
}