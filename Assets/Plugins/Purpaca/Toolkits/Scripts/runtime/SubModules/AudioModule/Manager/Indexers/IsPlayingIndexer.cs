namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄当前播放状态的属性索引器代理
        /// </summary>
        public class IsPlayingIndexer : ReadonlyIndexerBase<bool>
        {
            public IsPlayingIndexer(AudioManager manager) : base(manager) { }

            public override bool this[string guid] => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].IsPlaying : false;
        }
    }
}