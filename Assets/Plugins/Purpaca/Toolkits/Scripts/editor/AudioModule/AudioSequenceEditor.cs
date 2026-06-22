using UnityEditor;
using Purpaca.Audio;

namespace Purpaca.Editor.Audio
{
    [CustomEditor(typeof(AudioSequence))]
    public class AudioSequenceEditor : UnityEditor.Editor
    {
        #region 字段
        private SerializedProperty m_clipsProperty;
        #endregion

        #region Public 方法
        /// <summary>
        /// 创建一个新的 <see cref="AudioSequence"/> 类型的Asset
        /// </summary>
        [MenuItem("Purpaca/Audio/Create AudioSequence Asset")]
        [MenuItem("Assets/Create/Purpaca/Audio/AudioSequence Asset")]
        public static void CreateAudioSequenceAsset()
        {
            Utils.CreateScriptableAsset<AudioSequence>("New AudioSequence");
        }

        #region 重载方法
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            bool hasUnreachableClip = false;
            int clipsCount = m_clipsProperty.arraySize;
            for (int i = 0; i < clipsCount; i++) 
            {
                var clip = m_clipsProperty.GetArrayElementAtIndex(i);
                if (clip.FindPropertyRelative("m_loops").intValue < 0 && i < clipsCount - 1)
                {
                    hasUnreachableClip = true;
                    break;
                }
            }

            if (hasUnreachableClip) 
            {
                EditorGUILayout.HelpBox("将音频序列中非末尾位置的音频片段设置为无限循环播放会导致后续的所有片段被忽略而无法播放！", MessageType.Warning);
            }
        }
        #endregion

        #endregion

        #region Unity 消息
        private void OnEnable()
        {
            m_clipsProperty = serializedObject.FindProperty("m_clips");
        }
        #endregion
    }
}
