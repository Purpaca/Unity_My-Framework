using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using IEnumerator = System.Collections.IEnumerator;

namespace Purpaca
{
    /// <summary>
    /// 音频管理器，提供分轨音量管理、多音频序列播放、音频播放完毕执行回调等功能
    /// </summary>
    public class AudioManager : MonoManagerBase<AudioManager>
    {
        #region 字段
        private List<Handle> m_oneShotHandles;
        private Dictionary<string, Handle> m_managedHandles;
        private List<AudioSource> m_pooledAudioSources;
        private List<Handle> m_pooledHandles;

        private float m_masterVolume = 1.0f;    // 全局音频播放音量
        private float m_musicVolume = 1.0f;     // 音乐音频播放音量 
        private float m_soundVolume = 1.0f;     // 音效音频播放音量
        private float m_otherVolume = 1.0f;     // 其它音频播放音量
        private int m_maxPooledAudioSourceCount = 50;
        private int m_maxPooledHandleCount = 50;

        private AudioMixer _mixer;
        private AudioMixerGroup _masterGroup, _musicGroup, _soundGroup, _otherGroup;
        #endregion

        #region 索引器
        private static BypassEffectsIndexer m_bypassEffectsIndexer = new();
        private static BypassListenerEffectsIndexer m_bypassListenerEffectsIndexer = new();
        private static BypassReverbZonesIndexer m_bypassReverbZonesIndexer = new();

        private static VolumeIndexer m_volumeIndexer = new();
        private static PitchIndexer m_pitchIndexer = new();
        private static PanSteroIndexer m_panSteroIndexer = new();
        private static SpatialBlendIndexer m_spatialBlendIndexer = new();
        private static ReverbZoneMixIndexer m_reverbZoneMixIndexer = new();

        private static DopplerLevelIndexer m_dopplerLevelIndexer = new();
        private static SpreadIndexer m_SpreadIndexer = new();
        private static RolloffModeIndexer m_rolloffModeIndexer = new();
        private static MinDistanceIndexer m_minDistanceIndexer = new();
        private static MaxDistanceIndexer m_maxDistanceIndexer = new();

        private static PositionIndexer m_positionIndexer = new();

        private static IsPlayingIndexer m_isPlayingIndexer = new();
        private static LengthIndexer m_lengthIndexer = new();
        //private static TimeIndexer m_timeIndexer = new();
        #region 对外公开
        public static BypassEffectsIndexer BypassEffects { get => m_bypassEffectsIndexer; }
        public static BypassListenerEffectsIndexer BypassListenerEffects { get => m_bypassListenerEffectsIndexer; }
        public static BypassReverbZonesIndexer BypassReverbZones { get => m_bypassReverbZonesIndexer; }

        public static VolumeIndexer Volume { get => m_volumeIndexer; }
        public static PitchIndexer Pitch { get => m_pitchIndexer; }
        public static PanSteroIndexer PanStero { get => m_panSteroIndexer; }
        public static SpatialBlendIndexer SpatialBlend { get => m_spatialBlendIndexer; }
        public static ReverbZoneMixIndexer ReverbZoneMix { get => m_reverbZoneMixIndexer; }

        public static DopplerLevelIndexer DopplerLevel { get => m_dopplerLevelIndexer; }
        public static SpreadIndexer Spread { get => m_SpreadIndexer; }
        public static RolloffModeIndexer RolloffMode { get => m_rolloffModeIndexer; }
        public static MinDistanceIndexer MinDistance { get => m_minDistanceIndexer; }
        public static MaxDistanceIndexer MaxDistance { get => m_maxDistanceIndexer; }

        public static PositionIndexer Position { get => m_positionIndexer; }

        public static IsPlayingIndexer IsPlaying { get => m_isPlayingIndexer; }
        public static LengthIndexer Length { get => m_lengthIndexer; }
        //public static TimeIndexer Time { get => m_timeIndexer; }
        #endregion

        #endregion

        #region 属性
        /// <summary>
        /// 全局音频播放音量
        /// </summary>
        public static float MasterVolume
        {
            get => instance.m_masterVolume;
            set
            {
                instance.m_masterVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                instance._mixer.SetFloat("MasterVolume", db);
            }
        }

        /// <summary>
        /// 音乐音频播放音量
        /// </summary>
        public static float MusicVolume
        {
            get => instance.m_musicVolume;
            set
            {
                instance.m_musicVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                instance._mixer.SetFloat("MusicVolume", db);
            }
        }

        /// <summary>
        /// 音效音频播放音量
        /// </summary>
        public static float SoundVolume
        {
            get => instance.m_soundVolume;
            set
            {
                instance.m_soundVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                instance._mixer.SetFloat("SoundVolume", db);
            }
        }

        /// <summary>
        /// 其它音频播放音量
        /// </summary>
        public static float OtherVolume 
        {
            get => instance.m_otherVolume;
            set 
            {
                instance.m_otherVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                instance._mixer.SetFloat("OtherVolume", db);
            }
        }

        /// <summary>
        /// 存储空闲AudioSource的最大数量
        /// </summary>
        public static int AudioSourcePoolSize
        {
            get => instance.m_maxPooledAudioSourceCount;
            set => instance.m_maxPooledAudioSourceCount = Mathf.Max(0, value);
        }

        /// <summary>
        /// 存储空闲音频播放句柄的最大数量
        /// </summary>
        public static int HandlePoolSize
        {
            get => instance.m_maxPooledHandleCount;
            set => instance.m_maxPooledHandleCount = Mathf.Max(0, value);
        }
        #endregion

        #region Public 方法
        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="loops">在播放一次的基础上额外循环播放的次数。如果此值为负，则永久循环播放</param>
        /// <param name="volume">此音频播放句柄的播放音量</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public static string Play(AudioClip audioClip, int loops = 0, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            AudioSequence sequence = AudioSequence.CreateAudioSequence(new AudioSequence.Clip(audioClip, loops));
            return Play(sequence, volume, channel, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="volume">此音频播放句柄的播放音量</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public static string Play(AudioSequence sequence, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            string identity = Guid.NewGuid().ToString();

            AudioSource source;
            if (!instance.TryGetAudioSource(out source))
            {
                source = instance.SpawnNewAudioSource();
            }

            Handle handle = instance.GetFreeHandle(sequence, source, volume);
            handle.AddOnFinishedListener(callback);

            SetOutputChanel(ref source, channel);

            instance.m_managedHandles.Add(identity, handle);
            handle.Play();

            return identity;
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="volume">播放音量</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioClip audioClip, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            AudioSequence sequence = AudioSequence.CreateAudioSequence(new AudioSequence.Clip(audioClip, 0));
            PlayOneShot(sequence, volume, channel, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="volume">播放音量</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioSequence sequence, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            foreach(var clip in sequence.Clips) 
            {
                if (clip.Loops < 0) 
                {
                    //需要英语化
                    Debug.LogError($"不能一次性播放此序列\"{sequence.name}\"，因为它是无限循环的！");
                    return;
                }
            }

            AudioSource source;
            if (!instance.TryGetAudioSource(out source))
            {
                source = instance.SpawnNewAudioSource();
            }

            Handle handle = instance.GetFreeHandle(sequence, source, volume);
            handle.AddOnFinishedListener(() =>
            {
                callback?.Invoke();
                handle.Free();
                instance.m_oneShotHandles.Remove(handle);
            });

            SetOutputChanel(ref source, channel);

            instance.m_oneShotHandles.Add(handle);
            handle.Play();
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄重新播放
        /// </summary>
        public static void Replay(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].Play();
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄停止播放
        /// </summary>
        public static void Stop(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].Stop();
        }

        /// <summary>
        /// 暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public static void Pause(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].Pause();
        }

        /// <summary>
        /// 取消暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public static void UnPause(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].Resume();
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄添加在播放完毕后的回调方法
        /// </summary>
        public static void AddOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].AddOnFinishedListener(callback);
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄移除在播放完毕后的回调方法
        /// </summary>
        public static void RemoveOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].RemoveOnFinishedListener(callback);
        }

        /// <summary>
        /// 清除唯一标识码对应音频播放句柄所有播放完毕后的回调方法
        /// </summary>
        public static void ClearOnPlayFinishedCallback(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            instance.m_managedHandles[guid].ClearOnFinishedListener();
        }

        /// <summary>
        /// 设置唯一标识码对应音频播放句柄的音频输出频道
        /// </summary>
        public static void SetOutputChanel(string guid, AudioOutputChannel chanel)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            AudioMixerGroup mixerGroup;
            switch (chanel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = instance._musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = instance._soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = instance._masterGroup;
                    break;
            }
            instance.m_managedHandles[guid].OutputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 设置给定的AudioSource的音频输出频道
        /// </summary>
        public static void SetOutputChanel(ref AudioSource audioSource, AudioOutputChannel chanel)
        {
            AudioMixerGroup mixerGroup;

            switch (chanel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = instance._musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = instance._soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = instance._masterGroup;
                    break;
            }

            audioSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 设置给定的AudioMixer的音频输出频道
        /// </summary>
        public static void SetOutputChanel(ref AudioMixer audioMixer, AudioOutputChannel chanel)
        {
            AudioMixerGroup mixerGroup;

            switch (chanel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = instance._musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = instance._soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = instance._masterGroup;
                    break;
            }

            audioMixer.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 释放唯一标识码对应的音频播放句柄
        /// </summary>
        public static void Free(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return;
            }

            Handle handle = instance.m_managedHandles[guid];
            handle.Free();
            instance.m_managedHandles.Remove(guid);
        }

        /// <summary>
        /// 释放全部的音频播放句柄
        /// </summary>
        /// <param name="freeOneShot">是否立即停止播放并释放一次性播放的音频播放句柄？</param>
        public static void FreeAll(bool freeOneShot = true)
        {
            foreach (var handle in instance.m_managedHandles.Values)
            {
                handle.Free();
            }
            instance.m_managedHandles.Clear();

            if (freeOneShot)
            {
                foreach (var handle in instance.m_oneShotHandles)
                {
                    handle.Free();
                }
                instance.m_oneShotHandles.Clear();
            }
        }
        #endregion

        #region Privete 方法
        /// <summary>
        /// 将音量百分比（0.0f~1.0f）转换为AudioMixer中的音量（DB）
        /// </summary>
        private static float Convert01ToDB(float value)
        {
            value = value <= 0.0f ? 0.0001f : value;
            return Mathf.Log10(value) * 20.0f;
        }

        /// <summary>
        /// 将给定的AudioSource组件的参数重置为默认值
        /// </summary>
        private static void ResetAudioSourceProperties(AudioSource audioSource)
        {
            audioSource.clip = null;
            audioSource.outputAudioMixerGroup = null;

            audioSource.bypassEffects = false;
            audioSource.bypassListenerEffects = false;
            audioSource.bypassReverbZones = false;

            audioSource.volume = 1.0f;
            audioSource.pitch = 1.0f;
            audioSource.panStereo = 0.0f;
            audioSource.spatialBlend = 0.0f;
            audioSource.reverbZoneMix = 1.0f;

            audioSource.dopplerLevel = 1.0f;
            audioSource.spread = 0.0f;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1.0f;
            audioSource.maxDistance = 500.0f;

            audioSource.transform.position = Vector3.zero;
        }

        /// <summary>
        /// 获取并初始化一个音频播放句柄
        /// </summary>
        private Handle GetFreeHandle(AudioSequence sequence, AudioSource audioSource, float volume = 1.0f)
        {
            Handle handle;
            if (m_pooledHandles.Count > 0)
            {
                handle = m_pooledHandles[0];
                handle.Activate(sequence, audioSource, volume);
                m_pooledHandles.RemoveAt(0);
            }
            else
            {
                handle = new Handle(sequence, audioSource, volume);
            }

            return handle;
        }

        /// <summary>
        /// 创建挂有AudioSource组件的GameObject
        /// </summary>
        private AudioSource SpawnNewAudioSource()
        {
            GameObject gameObject = new GameObject("Managed AudioSource");
            gameObject.transform.SetParent(instance.gameObject.transform);

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            return audioSource;
        }

        /// <summary>
        /// 创建指定数量的AudioSource实例
        /// </summary>
        private AudioSource[] SpawnNewAudioSources(int count)
        {
            AudioSource[] audioSources = new AudioSource[count];
            for (int i = 0; i < count; i++)
            {
                audioSources[i] = SpawnNewAudioSource();
            }

            return audioSources;
        }

        /// <summary>
        /// 尝试从AudioSource池中取出一个空闲的AudioSource实例
        /// </summary>
        /// <param name="audioSource">取出的AudioSource实例</param>
        /// <returns>是否成功取出空闲的AudioSource实例？</returns>
        private bool TryGetAudioSource(out AudioSource audioSource)
        {
            for (int i = 0; i < m_pooledAudioSources.Count; i++)
            {
                if (!m_pooledAudioSources[i].isPlaying)
                {
                    audioSource = m_pooledAudioSources[i];
                    m_pooledAudioSources.Remove(m_pooledAudioSources[i]);
                    return true;
                }
            }

            audioSource = null;
            return false;
        }
        #endregion

        #region Protected 方法
        protected override void OnInit()
        {
            try
            {
                _mixer = Resources.Load<AudioMixer>("Purpaca/AudioMixer");
                _masterGroup = _mixer.FindMatchingGroups("Master")[0];
                _musicGroup = _mixer.FindMatchingGroups("Master/Music")[0];
                _soundGroup = _mixer.FindMatchingGroups("Master/Sound")[0];
                _otherGroup = _mixer.FindMatchingGroups("Master/Sound/Other")[0];

                if (_masterGroup == null || _musicGroup == null || _soundGroup == null || _otherGroup == null)
                {
                    throw new Exception();
                }
            }
            catch
            {
                throw new NullReferenceException("Unable to load the preset AudioMixer asset or the asset is modified!");
            }

            m_pooledAudioSources = new List<AudioSource>();
            m_pooledAudioSources.AddRange(SpawnNewAudioSources(10));
            m_pooledHandles = new List<Handle>();
            m_managedHandles = new Dictionary<string, Handle>();
            m_oneShotHandles = new List<Handle>();
        }
        #endregion

        #region 内部类型
        /// <summary>
        /// 音频播放句柄
        /// </summary>
        private class Handle
        {
            private AudioSource m_audioSource;
            private AudioSequence m_sequence;
            private UnityAction m_onFinished;

            private bool _isInProcess = false;
            private bool _isPaused = false;
            private bool _disposed = false;    // 标识当前播放句柄是否被释放的Flag，被释放的播放句柄将无法被访问
            private Coroutine _coroutine;

            #region 构造器
            public Handle(AudioSequence sequence, AudioSource audioSource, float volume = 1.0f)
            {
                m_sequence = sequence;
                m_audioSource = audioSource;
                m_audioSource.volume = volume;
            }
            #endregion

            #region 属性
            public AudioMixerGroup OutputAudioMixerGroup
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return null;
                    }
                    return m_audioSource.outputAudioMixerGroup;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.outputAudioMixerGroup = value;
                }
            }

            public bool ByPassEffects
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return false;
                    }
                    return m_audioSource.bypassEffects;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.bypassEffects = value;
                }
            }

            public bool BypassListenerEffects
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return false;
                    }
                    return m_audioSource.bypassListenerEffects;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.bypassListenerEffects = value;
                }
            }

            public bool BypassReverbZones
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return false;
                    }
                    return m_audioSource.bypassReverbZones;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.bypassReverbZones = value;
                }
            }

            public float Volume
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.volume;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.volume = value;
                }
            }

            public float Pitch
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.pitch;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.pitch = value;
                }
            }

            public float PanStero
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.panStereo;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.panStereo = value;
                }
            }

            public float SpatialBlend
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.spatialBlend;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.spatialBlend = value;
                }
            }

            public float ReverbZoneMix
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.reverbZoneMix;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.reverbZoneMix = value;
                }
            }

            public float DopplerLevel
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.dopplerLevel;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.dopplerLevel = value;
                }
            }

            public float Spread
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.spread;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.spread = value;
                }
            }

            public AudioRolloffMode RolloffMode
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return AudioRolloffMode.Custom;
                    }
                    return m_audioSource.rolloffMode;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }

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

            public float MinDistance
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.minDistance;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.minDistance = value;
                }
            }

            public float MaxDistance
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }
                    return m_audioSource.maxDistance;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.maxDistance = value;
                }
            }

            public Vector3 Position
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return Vector3.zero;
                    }
                    return m_audioSource.transform.position;
                }
                set
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }
                    m_audioSource.transform.position = value;
                }
            }

            public bool IsPlaying
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return false;
                    }
                    return _isInProcess && !_isPaused;
                }
            }

            public float Length
            {
                get
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
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

            /*
            public float Time 
            {
                get 
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return float.NaN;
                    }


                }
                set 
                {
                    if (_disposed)
                    {
                        Debug.LogError("Attempt to access a disposed handle!");
                        return;
                    }


                }
            }
            */
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
                    instance.StopCoroutine(_coroutine);
                }
                _coroutine = instance.StartCoroutine(Process());
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
                    instance.StopCoroutine(_coroutine);
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
                    instance.StopCoroutine(_coroutine);
                }
                m_audioSource.Stop();

                ResetAudioSourceProperties(m_audioSource);
                if (instance.m_pooledAudioSources.Count < AudioSourcePoolSize)
                {
                    instance.m_pooledAudioSources.Add(m_audioSource);
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

                if (instance.m_pooledHandles.Count < HandlePoolSize)
                {
                    instance.m_pooledHandles.Add(this);
                }

                _disposed = true;
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
                        while (m_audioSource.isPlaying || _isPaused )
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

        #region 音频播放句柄属性索引器
        public class BypassEffectsIndexer
        {
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return instance.m_managedHandles[guid].ByPassEffects;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].ByPassEffects = value;
                }
            }
        }

        public class BypassListenerEffectsIndexer
        {
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return instance.m_managedHandles[guid].BypassListenerEffects;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].BypassListenerEffects = value;
                }
            }
        }

        public class BypassReverbZonesIndexer
        {
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return instance.m_managedHandles[guid].BypassReverbZones;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].BypassReverbZones = value;
                }
            }
        }

        public class VolumeIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].Volume;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].Volume = value;
                }
            }
        }

        public class PitchIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].Pitch;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].Pitch = value;
                }
            }
        }

        public class PanSteroIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].PanStero;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].PanStero = value;
                }
            }
        }

        public class SpatialBlendIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].SpatialBlend;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].SpatialBlend = value;
                }
            }
        }

        public class ReverbZoneMixIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].ReverbZoneMix;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].ReverbZoneMix = value;
                }
            }
        }

        public class DopplerLevelIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].DopplerLevel;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].DopplerLevel = value;
                }
            }
        }

        public class SpreadIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].Spread;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].Spread = value;
                }
            }
        }

        public class RolloffModeIndexer
        {
            public AudioRolloffMode this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return AudioRolloffMode.Custom;
                    }
                    return instance.m_managedHandles[guid].RolloffMode;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].RolloffMode = value;
                }
            }
        }

        public class MinDistanceIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].MinDistance;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].MinDistance = value;
                }
            }
        }

        public class MaxDistanceIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].MaxDistance;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].MaxDistance = value;
                }
            }
        }

        public class PositionIndexer
        {
            public Vector3 this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return Vector3.zero;
                    }
                    return instance.m_managedHandles[guid].Position;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].Position = value;
                }
            }
        }

        public class IsPlayingIndexer
        {
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return instance.m_managedHandles[guid].IsPlaying;
                }
            }
        }

        public class LengthIndexer
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].Length;
                }
            }
        }

        /*TODO 实现控制播放的时间进度，难点在于无限循环的序列的播放时间的解析和计算*/
        /*
        public class TimeIndexer 
        {
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return instance.m_managedHandles[guid].Time;
                }
                set 
                {
                    if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    instance.m_managedHandles[guid].Time = value;
                }
            }
        }*/
        #endregion

        #endregion
    }
}