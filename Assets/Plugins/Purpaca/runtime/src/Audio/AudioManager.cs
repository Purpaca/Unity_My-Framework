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
        private AudioMixer _mixer;
        private AudioMixerGroup _masterGroup, _musicGroup, _soundGroup, _otherGroup;

        private int m_maxPooledAudioSourceCount = 50;
        private int m_maxPooledHandleCount = 50;

        private float m_masterVolume = 1.0f;    // 全局音频播放音量
        private float m_musicVolume = 1.0f;     // 音乐音频播放音量
        private float m_soundVolume = 1.0f;     // 音效音频播放音量
        private float m_otherVolume = 1.0f;     // 其它音频播放音量

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

        #region 索引器
        /// <summary>
        /// 是否忽略混响效果？ (效果器组件或全局的 <see cref="AudioListener"/> 上设置的效果器).
        /// </summary>
        public static BypassEffectsIndexer BypassEffects { get => instance.m_bypassEffectsIndexer; }
        /// <summary>
        /// 是否受 ReverbZones 的影响？
        /// </summary>
        public static BypassReverbZonesIndexer BypassReverbZones { get => instance.m_bypassReverbZonesIndexer; }

        /// <summary>
        /// 播放音量（0.0f~1.0f）
        /// </summary>
        public static VolumeIndexer Volume { get => instance.m_volumeIndexer; }
        /// <summary>
        /// 音频播放的声调
        /// </summary>
        public static PitchIndexer Pitch { get => instance.m_pitchIndexer; }
        /// <summary>
        /// 向左声道（0~0.5）或向右声道（0.5~1.0）移动来播放音频
        /// </summary>
        public static PanSteroIndexer PanStero { get => instance.m_panSteroIndexer; }
        /// <summary>
        /// 受3D空间化计算（衰减、多普勒等）的影响程度。值为0则为完全2D音频播放，值为1.0则为完全3D音频播放。
        /// </summary>
        public static SpatialBlendIndexer SpatialBlend { get => instance.m_spatialBlendIndexer; }
        /// <summary>
        /// ReverbZone的混合程度。
        /// </summary>
        public static ReverbZoneMixIndexer ReverbZoneMix { get => instance.m_reverbZoneMixIndexer; }

        /// <summary>
        /// 音频播放受多普勒效果影响的程度。
        /// </summary>
        public static DopplerLevelIndexer DopplerLevel { get => instance.m_dopplerLevelIndexer; }
        /// <summary>
        /// 设置扬声器空间中3d立体声或多声道声音的扩散角度（以度为单位）
        /// </summary>
        public static SpreadIndexer Spread { get => instance.m_SpreadIndexer; }
        /// <summary>
        /// 音频随距离衰减的模式（<see cref="AudioRolloffMode"/> 类型的枚举值）
        /// </summary>
        public static RolloffModeIndexer RolloffMode { get => instance.m_rolloffModeIndexer; }
        /// <summary>
        /// 音频的音量停止增大的距离
        /// </summary>
        public static MinDistanceIndexer MinDistance { get => instance.m_minDistanceIndexer; }
        /// <summary>
        /// 音频变得听不见或停止衰减的距离，具体效果取决于 <see cref="RolloffMode"/>
        /// </summary>
        public static MaxDistanceIndexer MaxDistance { get => instance.m_maxDistanceIndexer; }

        /// <summary>
        /// 音频在世界坐标中播放的位置
        /// </summary>
        public static PositionIndexer Position { get => instance.m_positionIndexer; }

        /// <summary>
        /// 当前音频是否正在播放？
        /// </summary>
        public static IsPlayingIndexer IsPlaying { get => instance.m_isPlayingIndexer; }
        /// <summary>
        /// 音频的总时长
        /// </summary>
        public static LengthIndexer Length { get => instance.m_lengthIndexer; }
        /// <summary>
        /// 音频的当前播放时间
        /// </summary>
        [Obsolete("尚不支持的功能！", true)]
        public static TimeIndexer Time { get => instance.m_timeIndexer; }
        #endregion

        #endregion

        #region Public 方法
        /// <summary>
        /// 创建一个音频播放句柄，并立即开始播放
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <returns>音频播放句柄的唯一标识码，可用于之后对音频播放句柄进行访问和操作</returns>
        public static string Play(AudioClip audioClip, UnityAction callback = null) 
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
        public static string Play(AudioClip audioClip, int loops, UnityAction callback = null)
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
        public static string Play(AudioClip audioClip, AudioOutputChannel channel, UnityAction callback = null) 
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
        public static string Play(AudioClip audioClip, int loops, AudioOutputChannel channel, UnityAction callback = null)
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
        public static string Play(AudioClip audioClip, int loops, float volume, AudioOutputChannel channel, UnityAction callback = null)
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
        public static string Play(AudioSequence sequence, UnityAction callback = null)
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
        public static string Play(AudioSequence sequence, AudioOutputChannel channel, UnityAction callback = null)
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
        public static string Play(AudioSequence sequence, float volume, AudioOutputChannel channel, UnityAction callback = null)
        {
            string identity = Guid.NewGuid().ToString();

            AudioSource source;
            if (!instance.TryGetAudioSource(out source))
            {
                source = instance.SpawnNewAudioSource();
            }

            Handle handle = instance.GetFreeHandle(sequence, source, volume);
            handle.AddOnFinishedListener(callback);

            SetOutputChannel(ref source, channel);

            instance.m_managedHandles.Add(identity, handle);
            handle.Play();

            return identity;
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioClip audioClip, UnityAction callback = null)
        {
            PlayOneShot(audioClip, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="audioClip">要用于播放的AudioClip</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioClip audioClip, AudioOutputChannel channel, UnityAction callback = null)
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
        public static void PlayOneShot(AudioClip audioClip, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
        {
            AudioSequence sequence = AudioSequence.CreateAudioSequence(new AudioSequence.Clip(audioClip, 0));
            PlayOneShot(sequence, volume, channel, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioSequence sequence, UnityAction callback = null)
        {
            PlayOneShot(sequence, 1.0f, AudioOutputChannel.Other, callback);
        }

        /// <summary>
        /// 一次性播放音频
        /// </summary>
        /// <param name="sequence">要用于播放的音频序列</param>
        /// <param name="channel">音频输出频道</param>
        /// <param name="callback">当播放完毕后的回调方法</param>
        public static void PlayOneShot(AudioSequence sequence, AudioOutputChannel channel, UnityAction callback = null)
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
        public static void PlayOneShot(AudioSequence sequence, float volume = 1.0f, AudioOutputChannel channel = AudioOutputChannel.Other, UnityAction callback = null)
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

            SetOutputChannel(ref source, channel);

            instance.m_oneShotHandles.Add(handle);
            handle.Play();
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄重新播放
        /// </summary>
        public static void Replay(string guid)
        {
            if(CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].Play();
            }
        }

        /// <summary>
        /// 使唯一标识码对应的音频播放句柄停止播放
        /// </summary>
        public static void Stop(string guid)
        {
            if (CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].Stop();
            }
        }

        /// <summary>
        /// 暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public static void Pause(string guid)
        {
            if (CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].Pause();
            }
        }

        /// <summary>
        /// 取消暂停唯一标识码对应的音频播放句柄的播放
        /// </summary>
        public static void UnPause(string guid)
        {
            if(CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].Resume();
            }
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄添加在播放完毕后的回调方法
        /// </summary>
        public static void AddOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if (!CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].AddOnFinishedListener(callback);
            }
        }

        /// <summary>
        /// 为唯一标识码对应音频播放句柄移除在播放完毕后的回调方法
        /// </summary>
        public static void RemoveOnPlayFinishedCallback(string guid, UnityAction callback)
        {
            if(CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].RemoveOnFinishedListener(callback);
            }
        }

        /// <summary>
        /// 清除唯一标识码对应音频播放句柄所有播放完毕后的回调方法
        /// </summary>
        public static void ClearOnPlayFinishedCallback(string guid)
        {
            if (CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].ClearOnFinishedListener();
            }
        }

        /// <summary>
        /// 设置唯一标识码对应音频播放句柄的音频输出频道
        /// </summary>
        public static void SetOutputChannel(string guid, AudioOutputChannel channel)
        {
            if(CheckGuidValid(guid))
            {
                instance.m_managedHandles[guid].OutputChannel = channel;
            }
        }

        /// <summary>
        /// 设置给定的AudioSource的音频输出频道
        /// </summary>
        public static void SetOutputChannel(ref AudioSource audioSource, AudioOutputChannel channel)
        {
            AudioMixerGroup mixerGroup;

            switch (channel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = instance._musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = instance._soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = instance._otherGroup;
                    break;
            }

            audioSource.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 设置给定的AudioMixer的音频输出频道
        /// </summary>
        public static void SetOutputChannel(ref AudioMixer audioMixer, AudioOutputChannel channel)
        {
            AudioMixerGroup mixerGroup;

            switch (channel)
            {
                case AudioOutputChannel.Music:
                    mixerGroup = instance._musicGroup;
                    break;
                case AudioOutputChannel.Sound:
                    mixerGroup = instance._soundGroup;
                    break;
                case AudioOutputChannel.Other:
                default:
                    mixerGroup = instance._otherGroup;
                    break;
            }

            audioMixer.outputAudioMixerGroup = mixerGroup;
        }

        /// <summary>
        /// 释放唯一标识码对应的音频播放句柄
        /// </summary>
        public static void Free(string guid)
        {
            if(CheckGuidValid(guid))
            {
                Handle handle = instance.m_managedHandles[guid];
                handle.Free();
                instance.m_managedHandles.Remove(guid);
            }
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
                FreeAllOneShot();
            }
        }

        /// <summary>
        /// 释放全部的一次性的音频播放句柄
        /// </summary>
        public static void FreeAllOneShot()
        {
            foreach (var handle in instance.m_oneShotHandles)
            {
                handle.Free();
            }
            instance.m_oneShotHandles.Clear();
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
        /// 检查给定的唯一标识码是否对应有效的播放句柄
        /// </summary>
        /// <returns>给定的唯一标识码是否对应有效的播放句柄？</returns>
        private static bool CheckGuidValid(string guid)
        {
            if (string.IsNullOrEmpty(guid) || !instance.m_managedHandles.ContainsKey(guid))
            {
                Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                return false;
            }
            return true;
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
            #region 加载默认的AudioMixer资产
            try
            {
                _mixer = Resources.Load<AudioMixer>("Purpaca/AudioMixer");
                _masterGroup = _mixer.FindMatchingGroups("Master")[0];
                _musicGroup = _mixer.FindMatchingGroups("Master/Music")[0];
                _soundGroup = _mixer.FindMatchingGroups("Master/Sound")[0];
                _otherGroup = _mixer.FindMatchingGroups("Master/Other")[0];

                if (_masterGroup == null || _musicGroup == null || _soundGroup == null || _otherGroup == null)
                {
                    throw new Exception();
                }

                MasterVolume = 1.0f;
                MusicVolume = 1.0f;
                SoundVolume = 1.0f;
                OtherVolume = 1.0f;
            }
            catch
            {
                throw new NullReferenceException("Unable to load the preset AudioMixer asset or the asset is modified!");
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
            /// <summary>
            /// 音频句柄的输出频道
            /// </summary>
            public AudioOutputChannel OutputChannel
            {
                get
                {
                    if (!CheckIsvalidHandle()) 
                    {
                        return AudioOutputChannel.Other;
                    }

                    AudioOutputChannel output;
                    if(m_audioSource.outputAudioMixerGroup == instance._soundGroup) 
                    {
                        output = AudioOutputChannel.Sound;
                    }
                    else if(m_audioSource.outputAudioMixerGroup == instance._musicGroup) 
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
                    if (CheckIsvalidHandle()) 
                    {
                        switch (value)
                        {
                            case AudioOutputChannel.Sound:
                                m_audioSource.outputAudioMixerGroup = instance._soundGroup;
                                break;
                            case AudioOutputChannel.Music:
                                m_audioSource.outputAudioMixerGroup = instance._musicGroup;
                                break;
                            default:
                                m_audioSource.outputAudioMixerGroup = instance._otherGroup;
                                break;
                        }
                    }
                }
            }

            public bool ByPassEffects
            {
                get
                {
                    if (!CheckIsvalidHandle()) 
                    {
                        return false;
                    }

                    return m_audioSource.bypassEffects;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.bypassEffects = value;
                    }
                }
            }

            public bool BypassReverbZones
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return false;
                    }

                    return m_audioSource.bypassReverbZones;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.bypassReverbZones = value;
                    }
                }
            }

            public float Volume
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.volume;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.volume = value;
                    }
                }
            }

            public float Pitch
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.pitch;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.pitch = value;
                    }
                }
            }

            public float PanStero
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.panStereo;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.panStereo = value;
                    }
                }
            }

            public float SpatialBlend
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.spatialBlend;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.spatialBlend = value;
                    }
                }
            }

            public float ReverbZoneMix
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.reverbZoneMix;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.reverbZoneMix = value;
                    }
                }
            }

            public float DopplerLevel
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.dopplerLevel;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.dopplerLevel = value;
                    }
                }
            }

            public float Spread
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.spread;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.spread = value;
                    }
                }
            }

            public AudioRolloffMode RolloffMode
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return AudioRolloffMode.Custom;
                    }

                    return m_audioSource.rolloffMode;
                }
                set
                {
                    if (CheckIsvalidHandle())
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
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.minDistance;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.minDistance = value;
                    }
                }
            }

            public float MaxDistance
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    return m_audioSource.maxDistance;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.maxDistance = value;
                    }
                }
            }

            public Vector3 Position
            {
                get
                {
                    if (!CheckIsvalidHandle())
                    {
                        return Vector3.zero;
                    }

                    return m_audioSource.transform.position;
                }
                set
                {
                    if (CheckIsvalidHandle())
                    {
                        m_audioSource.transform.position = value;
                    }
                }
            }

            public bool IsPlaying
            {
                get
                {
                    if (!CheckIsvalidHandle())
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
                    if (!CheckIsvalidHandle())
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
                    if (!CheckIsvalidHandle())
                    {
                        return float.NaN;
                    }

                    //TODO
                    //TODO
                    return 0.0f;
                }
                set
                {
                    if (CheckIsvalidHandle())
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

            #region Private 方法
            /// <summary>
            /// 检查当前句柄是否有效
            /// </summary>
            /// <returns>当前句柄是否有效？</returns>
            private bool CheckIsvalidHandle() 
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

        #region 音频播放句柄属性索引器代理
        /// <summary>
        /// 音频播放句柄的是否忽略混响效果的属性索引器代理
        /// </summary>
        public class BypassEffectsIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public BypassEffectsIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return manager.m_managedHandles[guid].ByPassEffects;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].ByPassEffects = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的是否忽略混响区域的属性索引器代理
        /// </summary>
        public class BypassReverbZonesIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public BypassReverbZonesIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return manager.m_managedHandles[guid].BypassReverbZones;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].BypassReverbZones = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的播放音量的属性索引器代理代理
        /// </summary>
        public class VolumeIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public VolumeIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].Volume;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].Volume = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的音调的属性索引器代理
        /// </summary>
        public class PitchIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public PitchIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].Pitch;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].Pitch = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的声道平衡值的属性索引器代理
        /// </summary>
        public class PanSteroIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public PanSteroIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].PanStero;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].PanStero = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的空间混合值的属性索引器代理
        /// </summary>
        public class SpatialBlendIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public SpatialBlendIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].SpatialBlend;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].SpatialBlend = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的混响区域混合值的属性索引器代理
        /// </summary>
        public class ReverbZoneMixIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public ReverbZoneMixIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].ReverbZoneMix;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].ReverbZoneMix = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的多普勒效应级别值的属性索引器代理
        /// </summary>
        public class DopplerLevelIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public DopplerLevelIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].DopplerLevel;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].DopplerLevel = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的多声道传播角度的属性索引器代理
        /// </summary>
        public class SpreadIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public SpreadIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].Spread;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].Spread = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的音频衰减模式的属性索引器代理
        /// </summary>
        public class RolloffModeIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public RolloffModeIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public AudioRolloffMode this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return AudioRolloffMode.Custom;
                    }
                    return manager.m_managedHandles[guid].RolloffMode;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].RolloffMode = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的最小播放距离的属性索引器代理
        /// </summary>
        public class MinDistanceIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public MinDistanceIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].MinDistance;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].MinDistance = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的最大播放距离的属性索引器代理
        /// </summary>
        public class MaxDistanceIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public MaxDistanceIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].MaxDistance;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].MaxDistance = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄在世界空间下位置坐标的属性索引器代理
        /// </summary>
        public class PositionIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public PositionIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public Vector3 this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return Vector3.zero;
                    }
                    return manager.m_managedHandles[guid].Position;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].Position = value;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄当前播放状态的属性索引器代理
        /// </summary>
        public class IsPlayingIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public IsPlayingIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public bool this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return false;
                    }
                    return manager.m_managedHandles[guid].IsPlaying;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的音频序列时长的属性索引器代理
        /// </summary>
        public class LengthIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public LengthIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].Length;
                }
            }
            #endregion
        }

        /// <summary>
        /// 音频播放句柄的当前播放时间的属性索引器代理
        /// </summary>
        public class TimeIndexer
        {
            #region 字段
            private AudioManager manager;
            #endregion

            #region 构造器
            public TimeIndexer(AudioManager manager)
            {
                this.manager = manager;
            }
            #endregion

            #region 索引器
            public float this[string guid]
            {
                get
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return float.NaN;
                    }
                    return manager.m_managedHandles[guid].Time;
                }
                set
                {
                    if (string.IsNullOrEmpty(guid) || !manager.m_managedHandles.ContainsKey(guid))
                    {
                        Debug.LogWarning($"The given guid \"{guid}\" is invalid!");
                        return;
                    }
                    manager.m_managedHandles[guid].Time = value;
                }
            }
            #endregion
        }
        #endregion

        #endregion
    }
}