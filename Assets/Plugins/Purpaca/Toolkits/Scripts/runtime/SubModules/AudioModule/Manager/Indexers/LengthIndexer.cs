namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的音频序列时长的属性索引器代理
        /// </summary>
        public class LengthIndexer : ReadonlyIndexerBase<float>
        {
            public LengthIndexer(AudioManager manager) : base(manager) { }

            public override float this[string guid] => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Length : float.NaN;
        }
    }
}