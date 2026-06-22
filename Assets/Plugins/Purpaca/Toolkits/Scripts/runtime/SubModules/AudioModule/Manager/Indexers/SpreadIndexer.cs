namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的多声道传播角度的属性索引器代理
        /// </summary>
        public class SpreadIndexer : IndexerBase<float>
        {
            public SpreadIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Spread : float.NaN;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].Spread = value;
                    }
                }
            }
        }
    }
}