using System;
using UnityEngine;
using UnityEngine.Events;
using Object = UnityEngine.Object;

namespace Purpaca
{
    public class ManagedAudio : MonoBehaviour
    {
        [SerializeField]
        private AudioAssetInfo m_audio;
        [Space, SerializeField]
        private bool m_playOnAwake = true;
        [Space, SerializeField]
        private OnPlayEnd m_onAudioFinishedPlaying;

        private string m_guid;

        #region Public 方法
        public void Play()
        {
            if (!string.IsNullOrEmpty(m_guid))
            {
                AudioManager.Replay(m_guid);
            }
            else
            {
                switch (m_audio.m_type)
                {
                    case AudioAssetType.None:
                        return;

                    case AudioAssetType.AudioClip:
                        m_guid = PlayAudioClip(m_audio.m_asset as AudioClip);
                        break;

                    case AudioAssetType.AudioSequence:
                        m_guid = PlayAudioSequence(m_audio.m_asset as AudioSequence);
                        break;

                }
            }
        }

        public void Stop()
        {
            if (!string.IsNullOrEmpty(m_guid))
            {
                AudioManager.Stop(m_guid);
            }
        }
        #endregion

        #region Private 方法
        private string PlayAudioClip(AudioClip clip)
        {
            string guid = AudioManager.Play(clip);
            AudioManager.AddOnPlayFinishedCallback(guid, () =>
            {
                m_onAudioFinishedPlaying?.Invoke();
            });
            return guid;
        }

        private string PlayAudioSequence(AudioSequence sequence) 
        {
            string guid = AudioManager.Play(sequence);
            AudioManager.AddOnPlayFinishedCallback(guid, () =>
            {
                m_onAudioFinishedPlaying?.Invoke();
            });
            return guid;
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 可用于播放的音频资源类型
        /// </summary>
        public enum AudioAssetType 
        {
            None,
            /// <summary>
            /// <see cref="UnityEngine.AudioClip"/>
            /// </summary>
            AudioClip,
            /// <summary>
            /// <see cref="Purpaca.AudioSequence"/>
            /// </summary>
            AudioSequence
        }

        /// <summary>
        /// 可用于播放的音频资源信息
        /// </summary>
        [Serializable]
        public class AudioAssetInfo 
        {
            public Object m_asset;
            public AudioAssetType m_type;
        }

        [Serializable]
        public class OnPlayEnd : UnityEvent { }
        #endregion
    }
}
