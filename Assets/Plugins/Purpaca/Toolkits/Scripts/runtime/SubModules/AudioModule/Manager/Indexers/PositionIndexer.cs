using UnityEngine;

namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄在世界空间下位置坐标的属性索引器代理
        /// </summary>
        public class PositionIndexer : IndexerBase<Vector3>
        {
            public PositionIndexer(AudioManager manager) : base(manager) { }

            public override Vector3 this[string guid] 
            { 
                get => Manager.CheckGUIDValid(guid) ? Manager.m_managedHandles[guid].Position : Vector3.zero; 
                set
                {
                    if (Manager.CheckGUIDValid(guid)) 
                    {
                        Manager.m_managedHandles[guid].Position = value;
                    }
                }
            }
        }
    }
}