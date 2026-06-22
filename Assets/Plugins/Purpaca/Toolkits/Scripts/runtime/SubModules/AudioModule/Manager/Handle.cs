using UnityEngine;
using UnityEngine.Events;
using IEnumerator = System.Collections.IEnumerator;

namespace Purpaca.Audio
{
    public partial class AudioManager
    {
        /// <summary>
        /// 音频播放句柄
        /// </summary>
        public class Handle
        {
            private AudioSource m_audioSource;
            private AudioSequence m_sequence;
            private UnityAction m_onFinished;

            private bool _isInProcess = false;
            private bool _isPaused = false;
            private bool _disposed = false;    // 标识当前播放句柄是否被释放的Flag，被释放的播放句柄将无法被访问
            private Coroutine _coroutine;

            private AudioManager manager;

            #region 构造器
            public Handle(AudioManager manager, AudioSequence sequence, AudioSource audioSource, float volume = 1.0f)
            {
                this.manager = manager;

                m_sequence = sequence;
                m_audioSource = audioSource;
                m_audioSource.volume = volume;
            }
            #endregion

            #region 属性
            /// <summary>
            /// 音频句柄的输出频道
            /// </summary>
            public AudioOutputChannel OutputChannel
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return AudioOutputChannel.Other;
                    }

                    AudioOutputChannel output;
                    if (m_audioSource.outputAudioMixerGroup == manager._soundGroup)
                    {
                        output = AudioOutputChannel.Sound;
                    }
                    else if (m_audioSource.outputAudioMixerGroup == manager._musicGroup)
                    {
                        output = AudioOutputChannel.Music;
                    }
                    else
                    {
                        output = AudioOutputChannel.Other;
                    }

                    return output;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        switch (value)
                        {
                            case AudioOutputChannel.Sound:
                                m_audioSource.outputAudioMixerGroup = manager._soundGroup;
                                break;
                            case AudioOutputChannel.Music:
                                m_audioSource.outputAudioMixerGroup = manager._musicGroup;
                                break;
                            default:
                                m_audioSource.outputAudioMixerGroup = manager._masterGroup;
                                break;
                        }
                    }
                }
            }

            public bool BypassEffects
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return false;
                    }

                    return m_audioSource.bypassEffects;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.bypassEffects = value;
                    }
                }
            }

            public bool BypassReverbZones
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return false;
                    }

                    return m_audioSource.bypassReverbZones;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.bypassReverbZones = value;
                    }
                }
            }

            public float Volume
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.volume;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.volume = value;
                    }
                }
            }

            public float Pitch
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.pitch;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.pitch = value;
                    }
                }
            }

            public float PanStereo
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.panStereo;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.panStereo = value;
                    }
                }
            }

            public float SpatialBlend
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.spatialBlend;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.spatialBlend = value;
                    }
                }
            }

            public float ReverbZoneMix
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.reverbZoneMix;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.reverbZoneMix = value;
                    }
                }
            }

            public float DopplerLevel
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.dopplerLevel;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.dopplerLevel = value;
                    }
                }
            }

            public float Spread
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.spread;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.spread = value;
                    }
                }
            }

            public AudioRolloffMode RolloffMode
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return AudioRolloffMode.Custom;
                    }

                    return m_audioSource.rolloffMode;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        switch (value)
                        {
                            case AudioRolloffMode.Linear:
                            case AudioRolloffMode.Logarithmic:
                                m_audioSource.rolloffMode = value;
                                break;

                            default:
                                Debug.LogWarning("Parameter \"RolloffMode\" can only be set to \"Linear\" or \"Logarithmic\"!");
                                break;
                        }

                    }
                }
            }

            public float MinDistance
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.minDistance;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.minDistance = value;
                    }
                }
            }

            public float MaxDistance
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.maxDistance;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.maxDistance = value;
                    }
                }
            }

            public Vector3 Position
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return Vector3.zero;
                    }

                    return m_audioSource.transform.position;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        m_audioSource.transform.position = value;
                    }
                }
            }

            public bool IsPlaying
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return false;
                    }

                    return _isInProcess && !_isPaused;
                }
            }

            public float Length
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    float lenght = 0.0f;
                    foreach (var clip in m_sequence.Clips)
                    {
                        if (clip.Loops < 0)
                        {
                            return float.PositiveInfinity;
                        }

                        lenght += clip.AudioClip.length * (clip.Loops + 1);
                    }

                    return lenght;
                }
            }

            public float Time
            {
                get
                {
                    if (!CheckHandleValid())
                    {
                        return float.NaN;
                    }

                    //TODO
                    //TODO
                    return 0.0f;
                }
                set
                {
                    if (CheckHandleValid())
                    {
                        //TODO
                    }
                }
            }
            #endregion

            #region Public 方法
            /// <summary>
            /// 开始当前音频播放句柄的播放
            /// </summary>
            public void Play()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_sequence.LoadAudioData();

                if (_coroutine != null)
                {
                    manager.StopCoroutine(_coroutine);
                }
                _coroutine = manager.StartCoroutine(Process());
                _isPaused = false;
            }

            /// <summary>
            /// 停止当前音频播放句柄的播放
            /// </summary>
            public void Stop()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                if (_coroutine != null)
                {
                    manager.StopCoroutine(_coroutine);
                }
                m_audioSource.Stop();
                _isInProcess = false;
                _isPaused = false;
            }

            /// <summary>
            /// 暂停当前音频播放句柄的播放
            /// </summary>
            public void Pause()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_audioSource.Pause();
                _isPaused = true;
            }

            /// <summary>
            /// 恢复当前音频播放句柄的播放
            /// </summary>
            public void Resume()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_audioSource.UnPause();
                _isPaused = false;
            }

            /// <summary>
            /// 添加当此音频播放句柄播放完毕时的回调方法
            /// </summary>
            public void AddOnFinishedListener(UnityAction callback)
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_onFinished += callback;
            }

            /// <summary>
            /// 移除当此音频播放句柄播放完毕时的回调方法
            /// </summary>
            public void RemoveOnFinishedListener(UnityAction callback)
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_onFinished -= callback;
            }

            /// <summary>
            /// 清除当此音频播放句柄播放完毕时的回调方法
            /// </summary>
            public void ClearOnFinishedListener()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return;
                }

                m_onFinished = null;
            }

            /// <summary>
            /// 重新激活句柄
            /// </summary>
            public void Activate(AudioSequence sequence, AudioSource audioSource, float volume = 1.0f)
            {
                if (_disposed)
                {
                    m_sequence = sequence;
                    m_audioSource = audioSource;
                    m_audioSource.volume = volume;
                    _disposed = false;
                }
            }

            /// <summary>
            /// 释放此播放句柄
            /// </summary>
            public void Free()
            {
                if (_disposed)
                {
                    Debug.LogError("Cannot dispose a handle that has already been disposed");
                    return;
                }

                if (_coroutine != null)
                {
                    manager.StopCoroutine(_coroutine);
                }
                m_audioSource.Stop();

                ResetAudioSourceProperties(m_audioSource);
                if (manager.m_pooledAudioSources.Count < manager.AudioSourcePoolSize)
                {
                    manager.m_pooledAudioSources.Add(m_audioSource);
                }
                else
                {
                    Destroy(m_audioSource.gameObject);
                }

                _isInProcess = false;
                _isPaused = false;
                m_sequence = null;
                m_audioSource = null;
                m_onFinished = null;

                if (manager.m_pooledHandles.Count < manager.HandlePoolSize)
                {
                    manager.m_pooledHandles.Add(this);
                }

                _disposed = true;
            }
            #endregion

            #region Private 方法
            /// <summary>
            /// 检查当前句柄是否有效
            /// </summary>
            /// <returns>当前句柄是否有效？</returns>
            private bool CheckHandleValid()
            {
                if (_disposed)
                {
                    Debug.LogError("Attempt to access a disposed handle!");
                    return false;
                }
                else
                {
                    return true;
                }
            }
            #endregion

            #region 协程
            private IEnumerator Process()
            {
                _isInProcess = true;

                for (int i = 0; i < m_sequence.Clips.Length; i++)
                {
                    m_audioSource.clip = m_sequence.Clips[i].AudioClip;

                    int looped = m_sequence.Clips[i].Loops >= 0 ? 0 : m_sequence.Clips[i].Loops;
                    do
                    {
                        m_audioSource.Play();
                        while (m_audioSource.isPlaying || _isPaused)
                        {
                            yield return null;
                        }

                        if (m_sequence.Clips[i].Loops >= 0)
                        {
                            looped++;
                        }
                    }
                    while (looped <= m_sequence.Clips[i].Loops);
                }

                m_onFinished?.Invoke();
                _isInProcess = false;
            }
            #endregion
        }
    }
}