namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的播放音量的属性索引器代理代理
        /// </summary>
        public class VolumeIndexer : IndexerBase<float>
        {
            public VolumeIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Volume : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].Volume = value;
                    }
                }
            }
        }
    }
}