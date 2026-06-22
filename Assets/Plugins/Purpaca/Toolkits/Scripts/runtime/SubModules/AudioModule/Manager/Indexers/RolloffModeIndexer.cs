using UnityEngine;

namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄的音频衰减模式的属性索引器代理
        /// </summary>
        public class RolloffModeIndexer : IndexerBase<AudioRolloffMode>
        {
            public RolloffModeIndexer(AudioManager manager) : base(manager) { }

            public override AudioRolloffMode this[string guid]
            {
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].RolloffMode : default;
                set
                {
                    if (Manager.CheckGUIDValid(guid))
                    {
                        Manager.m_managedHandles[guid].RolloffMode = value;
                    }
                }
            }
        }
    }
}