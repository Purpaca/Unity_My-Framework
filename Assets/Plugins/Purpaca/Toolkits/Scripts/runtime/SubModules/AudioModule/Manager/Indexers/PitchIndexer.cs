namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的音调的属性索引器代理
        /// </summary>
        public class PitchIndexer : IndexerBase<float>
        {
            public PitchIndexer(AudioManager manager) : base(manager) { }
            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Pitch : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].Pitch = value;
                    }
                }
            }
        }
    }
}