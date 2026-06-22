namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的声道平衡值的属性索引器代理
        /// </summary>
        public class PanSteroIndexer : IndexerBase<float>
        {
            public PanSteroIndexer(AudioManager manager) : base(manager) { }
            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].PanStereo : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].PanStereo = value;
                    }
                }
            }
        }
    }
}
