using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

namespace Purpaca.Audio
{
    /// <summary>
    /// 音频管理器，提供分轨音量管理、多音频序列播放、音频播放完毕执行回调等功能
    /// </summary>
    public partial class AudioManager : MonoBehaviour
    {
        #region 单例模式实现
        private static AudioManager instance;

        /// <summary>
        /// 音频管理器的单例实例
        /// </summary>
        /// <remarks></remarks>
        public static AudioManager Instance 
        {
            get 
            {
                if(instance == null) 
                {
                    var go = new GameObject("AudioManager");
                    DontDestroyOnLoad(go);

                    instance = go.AddComponent<AudioManager>();
                }

                return instance;
            }
        }

        private void Awake()
        {
            if (instance != this && instance != null)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);

            OnInit();
        }
        #endregion

        #region 核心实现
        /// <summary>
        /// 音频管理器 初始化实现
        /// </summary>
        private void OnInit()
        {
            #region 加载默认的AudioMixer资产
            try
            {
                _mixer = Resources.Load<AudioMixer>("mixer");
                _masterGroup = _mixer.FindMatchingGroups("Master")[0];
                _musicGroup = _mixer.FindMatchingGroups("Master/Music")[0];
                _soundGroup = _mixer.FindMatchingGroups("Master/Sound")[0];

                if (_masterGroup == null || _musicGroup == null || _soundGroup == null)
                {
                    throw new Exception();
                }

                MasterVolume = 1.0f;
                MusicVolume = 1.0f;
                SoundVolume = 1.0f;
            }
            catch
            {
                throw new NullReferenceException("Purpaca.AudioManager:Unable to load the preset AudioMixer asset or the asset is modified!");
            }
            #endregion

            #region 初始化容器
            m_pooledAudioSources = new List<AudioSource>();
            m_pooledAudioSources.AddRange(SpawnNewAudioSources(10));
            m_pooledHandles = new List<Handle>();
            m_managedHandles = new Dictionary<string, Handle>();
            m_oneShotHandles = new List<Handle>();
            #endregion

            #region 初始化索引器
            m_bypassEffectsIndexer = new BypassEffectsIndexer(this);
            m_bypassReverbZonesIndexer = new BypassReverbZonesIndexer(this);

            m_volumeIndexer = new VolumeIndexer(this);
            m_pitchIndexer = new PitchIndexer(this);
            m_panSteroIndexer = new PanSteroIndexer(this);
            m_spatialBlendIndexer = new SpatialBlendIndexer(this);
            m_reverbZoneMixIndexer = new ReverbZoneMixIndexer(this);

            m_dopplerLevelIndexer = new DopplerLevelIndexer(this);
            m_SpreadIndexer = new SpreadIndexer(this);
            m_rolloffModeIndexer = new RolloffModeIndexer(this);
            m_minDistanceIndexer = new MinDistanceIndexer(this);
            m_maxDistanceIndexer = new MaxDistanceIndexer(this);

            m_positionIndexer = new PositionIndexer(this);

            m_isPlayingIndexer = new IsPlayingIndexer(this);
            m_lengthIndexer = new LengthIndexer(this);
            m_timeIndexer = new TimeIndexer(this);
            #endregion
        }

        #region 字段
        private AudioMixer _mixer;
        private AudioMixerGroup _masterGroup, _musicGroup, _soundGroup;

        private int m_maxPooledAudioSourceCount = 15;
        private int m_maxPooledHandleCount = 15;

        private float m_masterVolume = 1.0f;    // 全局音频播放音量
        private float m_musicVolume = 1.0f;     // 音乐音频播放音量
        private float m_soundVolume = 1.0f;     // 音效音频播放音量

        private List<Handle> m_oneShotHandles;
        private Dictionary<string, Handle> m_managedHandles;
        private List<AudioSource> m_pooledAudioSources;
        private List<Handle> m_pooledHandles;

        #region 索引器
        private BypassEffectsIndexer m_bypassEffectsIndexer;
        private BypassReverbZonesIndexer m_bypassReverbZonesIndexer;

        private VolumeIndexer m_volumeIndexer;
        private PitchIndexer m_pitchIndexer;
        private PanSteroIndexer m_panSteroIndexer;
        private SpatialBlendIndexer m_spatialBlendIndexer;
        private ReverbZoneMixIndexer m_reverbZoneMixIndexer;

        private DopplerLevelIndexer m_dopplerLevelIndexer;
        private SpreadIndexer m_SpreadIndexer;
        private RolloffModeIndexer m_rolloffModeIndexer;
        private MinDistanceIndexer m_minDistanceIndexer;
        private MaxDistanceIndexer m_maxDistanceIndexer;

        private PositionIndexer m_positionIndexer;

        private IsPlayingIndexer m_isPlayingIndexer;
        private LengthIndexer m_lengthIndexer;
        private TimeIndexer m_timeIndexer;
        #endregion

        #endregion

        #region 属性
        /// <summary>
        /// 全局音频播放音量
        /// </summary>
        public float MasterVolume
        {
            get => m_masterVolume;
            set
            {
                m_masterVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                _mixer.SetFloat("VolumeMaster", db);
            }
        }

        /// <summary>
        /// 音乐音频播放音量
        /// </summary>
        public float MusicVolume
        {
            get => m_musicVolume;
            set
            {
                m_musicVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                _mixer.SetFloat("VolumeMusic", db);
            }
        }

        /// <summary>
        /// 音效音频播放音量
        /// </summary>
        public float SoundVolume
        {
            get => m_soundVolume;
            set
            {
                m_soundVolume = Mathf.Clamp01(value);
                float db = Convert01ToDB(value);
                _mixer.SetFloat("VolumeSound", db);
            }
        }

        /// <summary>
        /// 存储空闲AudioSource的最大数量
        /// </summary>
        public int AudioSourcePoolSize
        {
            get => m_maxPooledAudioSourceCount;
            set => m_maxPooledAudioSourceCount = Mathf.Max(0, value);
        }

        /// <summary>
        /// 存储空闲音频播放句柄的最大数量
        /// </summary>
        public int HandlePoolSize
        {
            get => m_maxPooledHandleCount;
            set => m_maxPooledHandleCount = Mathf.Max(0, value);
        }

        #region 索引器
        /// <summary>
        /// 是否忽略混响效果？ (效果器组件或全局的 <see cref="AudioListener"/> 上设置的效果器).
        /// </summary>
        public BypassEffectsIndexer BypassEffects => m_bypassEffectsIndexer;

        /// <summary>
        /// 是否受 ReverbZones 的影响？
        /// </summary>
        public BypassReverbZonesIndexer BypassReverbZones => m_bypassReverbZonesIndexer;

        /// <summary>
        /// 播放音量（0.0f~1.0f）
        /// </summary>
        public VolumeIndexer Volume => m_volumeIndexer;

        /// <summary>
        /// 音频播放的声调
        /// </summary>
        public PitchIndexer Pitch => m_pitchIndexer;

        /// <summary>
        /// 向左声道（0~0.5）或向右声道（0.5~1.0）移动来播放音频
        /// </summary>
        public PanSteroIndexer PanStero => m_panSteroIndexer;

        /// <summary>
        /// 受3D空间化计算（衰减、多普勒等）的影响程度。值为0则为完全2D音频播放，值为1.0则为完全3D音频播放。
        /// </summary>
        public SpatialBlendIndexer SpatialBlend => m_spatialBlendIndexer;

        /// <summary>
        /// ReverbZone的混合程度。
        /// </summary>
        public ReverbZoneMixIndexer ReverbZoneMix => m_reverbZoneMixIndexer;

        /// <summary>
        /// 音频播放受多普勒效果影响的程度。
        /// </summary>
        public DopplerLevelIndexer DopplerLevel => m_dopplerLevelIndexer;

        /// <summary>
        /// 设置扬声器空间中3d立体声或多声道声音的扩散角度（以度为单位）
        /// </summary>
        public SpreadIndexer Spread => m_SpreadIndexer;

        /// <summary>
        /// 音频随距离衰减的模式（<see cref="AudioRolloffMode"/> 类型的枚举值）
        /// </summary>
        public RolloffModeIndexer RolloffMode => m_rolloffModeIndexer;

        /// <summary>
        /// 音频的音量停止增大的距离
        /// </summary>
        public MinDistanceIndexer MinDistance => m_minDistanceIndexer;

        /// <summary>
        /// 音频变得听不见或停止衰减的距离
        /// </summary>
        public MaxDistanceIndexer MaxDistance => m_maxDistanceIndexer;

        /// <summary>
        /// 音频在世界坐标中播放的位置
        /// </summary>
        public PositionIndexer Position => m_positionIndexer;

        /// <summary>
        /// 当前音频是否正在播放？
        /// </summary>
        public IsPlayingIndexer IsPlaying => m_isPlayingIndexer;

        /// <summary>
        /// 音频的总时长
        /// </summary>
        public LengthIndexer Length => m_lengthIndexer;

        /// <summary>
        /// 音频的当前播放时间
        /// </summary>
        [Obsolete("尚不支持的功能！", true)]
        public TimeIndexer Time => m_timeIndexer;
        #endregion

        #endregion

        #region Public 方法
        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioClip audioClip, UnityAction callback = null)
        {
            return Play(audioClip, 0, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="loops">在播放一次的基础上额外循环播放的次数。如果此值为负，则永久循环播放</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioClip audioClip, int loops, UnityAction callback = null)
        {
            return Play(audioClip, loops, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioClip audioClip, AudioOutputChannel channel, UnityAction callback = null)
        {
            return Play(audioClip, 0, 1.0f, channel, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="loops">在播放一次的基础上额外循环播放的次数。如果此值为负，则永久循环播放</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioClip audioClip, int loops, AudioOutputChannel channel, UnityAction callback = null)
        {
            return Play(audioClip, loops, 1.0f, channel, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="loops">在播放一次的基础上额外循环播放的次数。如果此值为负，则永久循环播放</param>
        /// <param name="volume">此音频播放句柄的播放音量</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioClip audioClip, int loops, float volume, AudioOutputChannel channel, UnityAction callback = null)
        {
            AudioSequence sequence = AudioSequence.CreateAudioSequence(new AudioSequence.Clip(audioClip, loops));
            return Play(sequence, volume, channel, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioSequence sequence, UnityAction callback = null)
        {
            return Play(sequence, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioSequence sequence, AudioOutputChannel channel, UnityAction callback = null)
        {
            return Play(sequence, 1.0f, channel, callback);
        }

        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="volume">此音频播放句柄的播放音量</param>
        /// <param name="channel">此音频播放句柄的音频输出频道</param>
        /// <param name="callback">当此音频播放句柄播放完毕后的回调方法</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public string Play(AudioSequence sequence, float volume, AudioOutputChannel channel, UnityAction callback = null)
        {
            string identity = Guid.NewGuid().ToString();

            AudioSource source;
            if (!TryGetAudioSource(out source))
            {
                source = SpawnNewAudioSource();
            }

            Handle handle = GetFreeHandle(sequence, source, volume);
            handle.AddOnFinishedListener(callback);

            SetOutputChannel(ref source, channel);

            m_managedHandles.Add(identity, handle);
            handle.Play();

            return identity;
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioClip audioClip, UnityAction callback = null)
        {
            PlayOneShot(audioClip, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioClip audioClip, AudioOutputChannel channel, UnityAction callback = null)
        {
            PlayOneShot(audioClip, 1.0f, channel, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="volume">播放音量</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioClip audioClip, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            AudioSequence sequence = AudioSequence.CreateAudioSequence(new AudioSequence.Clip(audioClip, 0));
            PlayOneShot(sequence, volume, channel, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioSequence sequence, UnityAction callback = null)
        {
            PlayOneShot(sequence, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioSequence sequence, AudioOutputChannel channel, UnityAction callback = null)
        {
            PlayOneShot(sequence, 1.0f, channel, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="volume">播放音量</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public void PlayOneShot(AudioSequence sequence, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            foreach (var clip in sequence.Clips)
            {
                if (clip.Loops < 0)
                {
                    // TODO：需要英语化
                    Debug.LogError($"不能一次性播放此序列\"{sequence.name}\"，因为它是无限循环的！");
                    return;
                }
            }

            AudioSource source;
            if (!TryGetAudioSource(out source))
            {
                source = SpawnNewAudioSource();
            }

            Handle handle = GetFreeHandle(sequence, source, volume);
            handle.AddOnFinishedListener(() =>
            {
                callback?.Invoke();
                handle.Free();
                m_oneShotHandles.Remove(handle);
            });

            SetOutputChannel(ref source, channel);

            m_oneShotHandles.Add(handle);
            handle.Play();
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄重新播放
        /// </summary>
        public void Replay(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].Play();
            }
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄停止播放
        /// </summary>
        public void Stop(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].Stop();
            }
        }

        /// <summary>
        /// 暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public void Pause(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].Pause();
            }
        }

        /// <summary>
        /// 取消暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public void UnPause(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].Resume();
            }
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄添加在播放完毕后的回调方法
        /// </summary>
        public void AddOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].AddOnFinishedListener(callback);
            }
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄移除在播放完毕后的回调方法
        /// </summary>
        public void RemoveOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].RemoveOnFinishedListener(callback);
            }
        }

        /// <summary>
        /// 清除唯一标识码对应音频播放句柄所有播放完毕后的回调方法
        /// </summary>
        public void ClearOnPlayFinishedCallback(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].ClearOnFinishedListener();
            }
        }

        /// <summary>
        /// 设置唯一标识码对应音频播放句柄的音频输出频道
        /// </summary>
        public void SetOutputChannel(string guid, AudioOutputChannel channel)
        {
            if (CheckGUIDValid(guid))
            {
                m_managedHandles[guid].OutputChannel = channel;
            }
        }

        /// <summary>
        /// 设置给定的AudioSource的音频输出频道
        /// </summary>
        public void SetOutputChannel(ref AudioSource audioSource, AudioOutputChannel channel)
        {
            AudioMixerGroup mixerGroup;

            switch (channel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = _musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = _soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = _masterGroup;
                    break;
            }

            audioSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 设置给定的AudioMixer的音频输出频道
        /// </summary>
        public void SetOutputChannel(ref AudioMixer audioMixer, AudioOutputChannel channel)
        {
            AudioMixerGroup mixerGroup;

            switch (channel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = _musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = _soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = _masterGroup;
                    break;
            }

            audioMixer.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 释放唯一标识码对应的音频播放句柄
        /// </summary>
        public void Free(string guid)
        {
            if (CheckGUIDValid(guid))
            {
                Handle handle = m_managedHandles[guid];
                handle.Free();
                m_managedHandles.Remove(guid);
            }
        }

        /// <summary>
        /// 释放全部的音频播放句柄
        /// </summary>
        /// <param name="freeOneShot">是否立即停止播放并释放一次性播放的音频播放句柄？</param>
        public void FreeAll(bool freeOneShot = true)
        {
            foreach (var handle in m_managedHandles.Values)
            {
                handle.Free();
            }
            m_managedHandles.Clear();
            if (freeOneShot)
            {
                FreeAllOneShot();
            }
        }

        /// <summary>
        /// 释放全部的一次性的音频播放句柄
        /// </summary>
        public void FreeAllOneShot()
        {
            foreach (var handle in m_oneShotHandles)
            {
                handle.Free();
            }
            m_oneShotHandles.Clear();
        }

        /// <summary>
        /// 检查给定的唯一标识码是否对应有效的播放句柄
        /// </summary>
        /// <returns>给定的唯一标识码是否对应有效的播放句柄？</returns>
        public bool IsGuidValid(string guid)
        {
            return (!string.IsNullOrEmpty(guid) && m_managedHandles.ContainsKey(guid));
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
        /// 检查给定的GUID是否有效，并在无效时输出警告日志
        /// </summary>
        private bool CheckGUIDValid(string guid)
        {
            var result = IsGuidValid(guid);

            if (!result)
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
            }

            return result;
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
                handle = new Handle(this, sequence, audioSource, volume);
            }

            return handle;
        }

        /// <summary>
        /// 创建挂有AudioSource组件的GameObject
        /// </summary>
        private AudioSource SpawnNewAudioSource()
        {
            var go = new GameObject("Managed AudioSource");
            go.transform.SetParent(gameObject.transform);

            AudioSource audioSource = go.AddComponent<AudioSource>();
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

        #endregion
    }
}