using System;
using UnityEngine;

namespace Purpaca
{
    /// <summary>
    /// 音频序列
    /// </summary>
    public class AudioSequence : ScriptableObject
    {
        [SerializeField]
        private Clip[] m_clips;

        #region 属性
        public Clip[] Clips
        {
            get => m_clips;
        }
        #endregion

        #region Public 方法
        /// <summary>
        /// 在运行时创建一个AudioSequence实例
        /// </summary>
        public static AudioSequence CreateAudioSequence(params Clip[] clips)
        {
            var seq = CreateInstance<AudioSequence>();
            seq.m_clips = clips;
            return seq;
        }

        /// <summary>
        /// 加载此音频序列中所有AudioClip的AudioData
        /// （导入设置中开启了"Preload Audio Data"选项的AudioClip会自动加载其AudioData）
        /// </summary>
        /// <returns>是否加载成功？</returns>
        public bool LoadAudioData()
        {
            bool succeeded = true;

            foreach (Clip clip in m_clips)
            {
                succeeded = clip.AudioClip.LoadAudioData();
            }

            return succeeded;
        }

        /// <summary>
        /// 卸载此音频序列中所有AudioClip的AudioData
        /// （此方法只对基于实际音频文件资源的AudioClip有效）
        /// </summary>
        /// <returns>是否卸载成功？</returns>
        public bool UnloadAudioData()
        {
            bool succeeded = true;

            foreach (Clip clip in m_clips)
            {
                succeeded = clip.AudioClip.UnloadAudioData();
            }

            return succeeded;
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 音频片段
        /// </summary>
        [Serializable]
        public sealed class Clip
        {
            [SerializeField]
            private AudioClip m_audioClip;

            [SerializeField, Tooltip("循环次数（片段会在播放一次的基础上根据该值额外重复播放多次，如果此值为负，则永久循环播放）")]
            private int m_loops;

            #region 构造器
            public Clip(AudioClip audioClip, int loops)
            {
                m_audioClip = audioClip;
                m_loops = loops;
            }
            #endregion

            #region 属性
            public AudioClip AudioClip { get => m_audioClip; }

            /// <summary>
            /// 循环次数（片段会在播放一次的基础上根据该值额外重复播放多次，如果此值为负，则永久循环播放）
            /// </summary>
            public int Loops { get => m_loops; }
            #endregion
        }
        #endregion
    }
}